using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RateLimiter.Reader.Infrastructure.Models;

[BsonIgnoreExtraElements]
public sealed class RateLimitDbModel
{
    [BsonElement("route")]
    public string Route { get; set; } = string.Empty;
    
    [BsonElement("requests_per_minute")]
    public int RequestsPerMinute { get; set; }
    
    [BsonId]
    public ObjectId Id { get; set; }
}