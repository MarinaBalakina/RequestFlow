namespace RateLimiter.Reader.Configuration;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "user-requests";
    public string GroupId { get; set; } = "rate-limiter-reader";
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}

public sealed class MongoOptions
{
    public string ConnectionString { get; set; } = "";
    public string DatabaseName { get; set; } = "";
    public string CollectionName { get; set; } = "";
    public TimeSpan SynchronizationPollInterval { get; set; } = TimeSpan.FromSeconds(1);
}

public sealed class RateLimiterOptions
{
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
