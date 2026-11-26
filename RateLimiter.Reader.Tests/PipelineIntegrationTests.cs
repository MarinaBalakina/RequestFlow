using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RateLimiter.Reader.Application;
using RateLimiter.Reader.Application.Services;
using RateLimiter.Reader.Configuration;
using RateLimiter.Reader.Infrastructure;
using RateLimiter.Reader.Infrastructure.Mappers;
using RateLimiter.Reader.Infrastructure.Models;
using RateLimiter.Reader.Infrastructure.Repositories;

namespace RateLimiter.Reader.Tests;

public sealed class PipelineIntegrationTests
{
    [IntegrationFact]
    public async Task ProcessesKafkaEventsAndSkipsMalformedJson()
    {
        string suffix = Guid.NewGuid().ToString("N");
        string topic = $"reader-test-{suffix}";
        string database = $"reader_test_{suffix}";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = "localhost:9092" }).Build();
        var mongo = new MongoClient("mongodb://localhost:27018/?directConnection=true");
        await admin.CreateTopicsAsync([new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }]);
        var collection = mongo.GetDatabase(database).GetCollection<BsonDocument>("rate_limits");
        await collection.InsertOneAsync(new BsonDocument { { "route", "/a" }, { "requests_per_minute", 2 } },
            cancellationToken: timeout.Token);
        var repository = new RateLimitRepository(
            mongo.GetDatabase(database).GetCollection<RateLimitDbModel>("rate_limits"),
            new RateLimitMapper(), Options.Create(new MongoOptions()));
        var limits = new ReaderService(repository);
        await limits.InitializeLimitsAsync();
        var limiterOptions = Options.Create(new RateLimiterOptions());
        var state = new TestBlocks(new TestClock());
        var service = new RequestLimitService(limits, state, limiterOptions, TimeProvider.System);
        using var consumer = new KafkaRequestConsumer(service,
            Options.Create(new KafkaOptions { Topic = topic, GroupId = topic }), NullLogger<KafkaRequestConsumer>.Instance);

        try
        {
            await consumer.StartAsync(timeout.Token);
            using var producer = new ProducerBuilder<string, string>(new ProducerConfig
            {
                BootstrapServers = "localhost:9092", EnableIdempotence = true
            }).Build();

            await producer.ProduceAsync(topic, new Message<string, string> { Key = "123", Value = "{invalid" }, timeout.Token);
            foreach (RequestEvent request in new RequestEvent[]
            {
                new("123", "/a"), new("321", "/a"), new("123", "/unlimited"),
                new("123", "/a"), new("123", "/a")
            })
                await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = request.UserId,
                    Value = JsonSerializer.Serialize(new { user_id = int.Parse(request.UserId), endpoint = request.Route })
                }, timeout.Token);
            while (!(await state.GetBlockStatusAsync("123", "/a", timeout.Token)).IsBlocked)
                await Task.Delay(100, timeout.Token);

            BlockStatus blockedUser = await state.GetBlockStatusAsync("123", "/a", timeout.Token);
            Assert.True(blockedUser.IsBlocked);
            Assert.False((await state.GetBlockStatusAsync("321", "/a", timeout.Token)).IsBlocked);
            Assert.False((await state.GetBlockStatusAsync("123", "/unlimited", timeout.Token)).IsBlocked);
        }
        finally
        {
            await consumer.StopAsync(CancellationToken.None);
            await admin.DeleteTopicsAsync([topic]);
            await mongo.DropDatabaseAsync(database);
        }
    }
}

public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_READER_INTEGRATION_TESTS") != "1")
            Skip = "Requires local MongoDB/Kafka; set RUN_READER_INTEGRATION_TESTS=1";
    }
}
