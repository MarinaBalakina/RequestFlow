namespace RateLimiter.Reader.Configuration;

public class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public TimeSpan BlockDuration { get; init; }
}