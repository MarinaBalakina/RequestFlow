using MongoDB.Driver;
using RateLimiter.Writer.Infrastructure.Models;
using MongoDB.Bson;

namespace RateLimiter.Writer.Infrastructure.Database;

public class RateLimitDb : IHostedService
{
    private readonly IMongoCollection<RateLimitDbModel> _collection;

    public RateLimitDb(IMongoCollection<RateLimitDbModel> collection)
    {
        _collection = collection;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var index = Builders<RateLimitDbModel>.IndexKeys
            .Ascending(model => model.Route);
        
        var indexOptions = new CreateIndexOptions
        {
            Unique = true,
            Name = "index_rate_limit_route",
            Background = true
        };
        
        var indexModel = new CreateIndexModel<RateLimitDbModel>(index, indexOptions);
        try
        {
            await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code is 85 or 86)
        {
            using var cursor = await _collection.Indexes.ListAsync(cancellationToken);
            var existingIndexes = await cursor.ToListAsync(cancellationToken);
            bool equivalentIndexExists = existingIndexes.Any(existing =>
                existing["key"].AsBsonDocument.Equals(new BsonDocument("route", 1)) &&
                existing.GetValue("unique", false) == BsonBoolean.True &&
                existing.GetValue("sparse", false) != BsonBoolean.True &&
                !existing.Contains("partialFilterExpression") && !existing.Contains("collation"));
            if (!equivalentIndexExists)
                throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
