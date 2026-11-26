namespace UserService.Api.Configuration;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    
    public string ConnectionString { get; init; } = String.Empty;
    
    public string KeyPrefix { get; init; } = String.Empty;
}