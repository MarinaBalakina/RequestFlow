using System.Globalization;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using RateLimiter.Reader.Application;
using RateLimiter.Reader.Configuration;

namespace RateLimiter.Reader.Infrastructure;

public sealed class KafkaRequestConsumer(
    RequestLimitService service, IOptions<KafkaOptions> options, ILogger<KafkaRequestConsumer> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        KafkaOptions settings = options.Value;
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            AllowAutoCreateTopics = false
        }).Build();
        consumer.Subscribe(settings.Topic);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> message;
                try
                {
                    message = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException ex) when (!ex.Error.IsFatal)
                {
                    logger.LogError(ex, "Kafka consume error; retrying");
                    await Task.Delay(settings.RetryDelay, stoppingToken);
                    continue;
                }

                while (true)
                {
                    try
                    {
                        var requestEvent = JsonSerializer.Deserialize<KafkaRequest>(message.Message.Value ?? "null")
                            ?? throw new JsonException("Request event must not be null");
                        
                        if (requestEvent.user_id <= 0)
                            throw new InvalidRequestEventException("user_id must be a positive integer");
                        
                        var request = new RequestEvent(requestEvent.user_id.ToString(CultureInfo.InvariantCulture),
                            requestEvent.endpoint);
                        BlockStatus result = await service.ProcessAsync(request,
                            message.TopicPartitionOffset.ToString(), stoppingToken);
                        logger.LogInformation("Request {Position}: user={UserId}, route={Route}, blocked={Blocked}, retryAfter={Seconds}s",
                            message.TopicPartitionOffset, request.UserId, request.Route, result.IsBlocked, result.RetryAfterSeconds);
                        break;
                    }
                    catch (Exception ex) when (ex is JsonException or InvalidRequestEventException)
                    {
                        logger.LogWarning(ex, "Skipping invalid request event at {Position}", message.TopicPartitionOffset);
                        break;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Processing failed at {Position}; retrying without committing",
                            message.TopicPartitionOffset);
                        await Task.Delay(settings.RetryDelay, stoppingToken);
                    }
                }

                try { consumer.Commit(message); }
                catch (KafkaException ex) when (!ex.Error.IsFatal)
                {
                    logger.LogWarning(ex, "Offset commit failed at {Position}; replay is possible", message.TopicPartitionOffset);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally { consumer.Close(); }
    }

    private sealed record KafkaRequest(int user_id, string endpoint);
}
