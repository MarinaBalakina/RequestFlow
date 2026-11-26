using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using UserRequestsKafkaGenerator.Configuration;
using UserRequestsKafkaGenerator.Models;

namespace UserRequestsKafkaGenerator.Services;

public sealed class KafkaEventPublisher
{
    private readonly IProducer<Null, string>  _producer;
    private readonly string _topic;

    public KafkaEventPublisher(IProducer<Null, string> producer, IOptions<KafkaOptions> options)
    {
        _producer = producer;
        _topic = options.Value.Topic;
    }

    public Task PublishAsync(UserRequestEvent requestEvent, CancellationToken token)
    {
        var json = JsonSerializer.Serialize(requestEvent);

        var message = new Message<Null, string> { Value = json};
        
        return _producer.ProduceAsync(_topic, message, token);
    }
}