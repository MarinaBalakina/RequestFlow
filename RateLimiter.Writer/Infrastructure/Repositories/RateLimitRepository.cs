using MongoDB.Driver;
using RateLimiter.Writer.Application.Abstractions;
using RateLimiter.Writer.Application.Exceptions;
using RateLimiter.Writer.Domain.Entity;
using RateLimiter.Writer.Infrastructure.Mappers;
using RateLimiter.Writer.Infrastructure.Models;

namespace RateLimiter.Writer.Infrastructure.Repositories;

public class RateLimitRepository : IRateLimitRepository
{
    private readonly IMongoCollection<RateLimitDbModel> _collection;

    private readonly RateLimitDbMapper _dbMapper;
    
    public RateLimitRepository(IMongoCollection<RateLimitDbModel> collection, RateLimitDbMapper dbMapper)
    {
        _collection = collection;
        _dbMapper = dbMapper;
    }
    
    public async Task<bool> ExistsByRouteAsync(string route, CancellationToken cancellationToken)
    {
        var filter = Builders<RateLimitDbModel>.Filter.Eq("route", route);
        
        RateLimitDbModel? model = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        
        return model is not null;
    }

    public async Task<RateLimit> CreateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken)
    {
        RateLimitDbModel model = _dbMapper.RateLimitToDbModel(rateLimit);
        
        try
        {
            await _collection.InsertOneAsync(model, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RateLimitAlreadyExistsException(rateLimit.Route);
        }
        
        return _dbMapper.DbModelToRateLimit(model);
    }

    public async Task<RateLimit?> GetLimitByRouteAsync(string route, CancellationToken cancellationToken)
    {
        var filter = Builders<RateLimitDbModel>.Filter.Eq("route", route);
        
        RateLimitDbModel? model = await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        
        return model == null ? null : _dbMapper.DbModelToRateLimit(model);
    }

    public async Task<RateLimit?> UpdateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken)
    {
        var filter = Builders<RateLimitDbModel>.Filter.Eq("route", rateLimit.Route);
        var update = Builders<RateLimitDbModel>.Update.Set("requests_per_minute",  rateLimit.RequestsPerMinute);
        
        var options = new FindOneAndUpdateOptions<RateLimitDbModel>{ ReturnDocument = ReturnDocument.After };
        
        RateLimitDbModel? model = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        
        return model == null ? null : _dbMapper.DbModelToRateLimit(model);
    }

    public async Task<bool> DeleteLimitAsync(string route, CancellationToken cancellationToken)
    {
        var filter = Builders<RateLimitDbModel>.Filter.Eq("route", route);
        
        var result = await _collection.DeleteOneAsync(filter, cancellationToken);
        
        return result.DeletedCount > 0;
    }
}