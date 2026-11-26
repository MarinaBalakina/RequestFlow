using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Domain.Entity;
using RateLimiter.Reader.Domain.Entity.Changes;
using RateLimiter.Reader.Infrastructure.Mappers;
using RateLimiter.Reader.Infrastructure.Models;
using RateLimiter.Reader.Configuration;

namespace RateLimiter.Reader.Infrastructure.Repositories;

public class RateLimitRepository :  IRateLimitRepository
{
    private readonly IMongoCollection<RateLimitDbModel> _rateLimitCollection;

    private readonly RateLimitMapper _mapper;

    private readonly ConcurrentDictionary<ObjectId, string> _routesById = new();
    private readonly ConcurrentDictionary<string, RateLimit> _lastLimits = new();
    private readonly TimeSpan _pollInterval;

    public RateLimitRepository(IMongoCollection<RateLimitDbModel> rateLimitCollection, RateLimitMapper mapper,
        IOptions<MongoOptions> options)
    {
        _rateLimitCollection = rateLimitCollection;
        _mapper = mapper;
        _pollInterval = options.Value.SynchronizationPollInterval;
    }

    public async IAsyncEnumerable<RateLimit> GetAllLimitsAsync()
    {
        var filters = Builders<RateLimitDbModel>.Filter.Empty;

        var options = new FindOptions<RateLimitDbModel>
        {
            BatchSize = 1000
        };
        
        using var cursor = await _rateLimitCollection.FindAsync(filters, options);

        while (await cursor.MoveNextAsync())
        {
            foreach(var current in cursor.Current)
            {
                _routesById[current.Id] = current.Route;
                
                var limit = _mapper.DbModelToRateLimit(current);
                _lastLimits[limit.Route] = limit;
                yield return limit;
            }
        }
    }

    public async IAsyncEnumerable<RateLimitChange> GetLimitChangesAsync()
    {
        var hello = await _rateLimitCollection.Database.RunCommandAsync<BsonDocument>(new BsonDocument("hello", 1));
        var changes = hello.Contains("setName") ? WatchChangesAsync() : PollChangesAsync();
        await foreach (var change in changes)
            yield return change;
    }

    private async IAsyncEnumerable<RateLimitChange> PollChangesAsync()
    {
        while (true)
        {
            await Task.Delay(_pollInterval);
            var snapshot = new Dictionary<string, RateLimit>();
            using var cursor = await _rateLimitCollection.FindAsync(
                Builders<RateLimitDbModel>.Filter.Empty, new FindOptions<RateLimitDbModel> { BatchSize = 1000 });
            while (await cursor.MoveNextAsync())
            foreach (var document in cursor.Current)
            {
                var limit = _mapper.DbModelToRateLimit(document);
                snapshot[limit.Route] = limit;
            }

            foreach (var pair in snapshot)
            {
                bool changed = !_lastLimits.TryGetValue(pair.Key, out var previous) || previous != pair.Value;
                _lastLimits[pair.Key] = pair.Value;
                if (changed)
                    yield return new RateLimitChange(RateLimitChangeType.Upsert, pair.Key, pair.Value);
            }
            foreach (string route in _lastLimits.Keys)
                if (!snapshot.ContainsKey(route) && _lastLimits.TryRemove(route, out _))
                    yield return new RateLimitChange(RateLimitChangeType.Delete, route, null);
        }
    }

    private async IAsyncEnumerable<RateLimitChange> WatchChangesAsync()
    {
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<RateLimitDbModel>>()
            .Match(change =>
                change.OperationType == ChangeStreamOperationType.Insert ||
                change.OperationType == ChangeStreamOperationType.Update ||
                change.OperationType == ChangeStreamOperationType.Delete ||
                change.OperationType == ChangeStreamOperationType.Replace);

        var option = new ChangeStreamOptions
        {
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
        };
        
        using var cursor = await _rateLimitCollection.WatchAsync(pipeline, option);

        while (await cursor.MoveNextAsync())
        {
            foreach (var current in cursor.Current)
            {
                if (current.OperationType == ChangeStreamOperationType.Delete)
                {
                    var id = current.DocumentKey["_id"].AsObjectId;

                    if (_routesById.TryRemove(id, out var route))
                    {
                        yield return new RateLimitChange(
                            RateLimitChangeType.Delete,
                            route,
                            null);
                    }
                    
                    continue;
                }

                var document = current.FullDocument;
                
                if (document is null) continue;
                
                var rateLimit = _mapper.DbModelToRateLimit(document);

                _routesById[document.Id] = rateLimit.Route;

                yield return new RateLimitChange(
                    RateLimitChangeType.Upsert,
                    rateLimit.Route,
                    rateLimit);
            }
        }
    }
}
