using MongoDB.Bson.Serialization.Attributes;

namespace RateLimiter.Writer.Infrastructure.Models;

[BsonIgnoreExtraElements]
public sealed class RateLimitDbModel
{
    [BsonElement("route")]
    public string Route { get; set; } = string.Empty;
    
    [BsonElement("requests_per_minute")]
    public int RequestsPerMinute { get; set; }
}