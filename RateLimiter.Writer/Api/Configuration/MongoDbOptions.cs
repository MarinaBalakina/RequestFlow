namespace RateLimiter.Writer.Api.Configuration;

public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; init; } = string.Empty;
    
    public string DatabaseName { get; init; } = string.Empty;

    public string CollectionName { get; init; } = string.Empty;
}