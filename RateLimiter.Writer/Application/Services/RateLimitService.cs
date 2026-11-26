using RateLimiter.Writer.Application.Abstractions;
using RateLimiter.Writer.Application.Exceptions;
using RateLimiter.Writer.Domain.Entity;

namespace RateLimiter.Writer.Application.Services;

public sealed class RateLimitService : IRateLimitService
{
    private readonly IRateLimitRepository _repository;

    public RateLimitService(IRateLimitRepository repository)
    {
        _repository = repository;
    }

    public async Task<RateLimit> CreateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken)
    {
        bool alreadyExists = await _repository.ExistsByRouteAsync(rateLimit.Route, cancellationToken);
        if (alreadyExists)
        {
            throw new RateLimitAlreadyExistsException(rateLimit.Route);
        }

        return await _repository.CreateLimitAsync(rateLimit, cancellationToken);
    }

    public async Task<RateLimit> GetLimitByRouteAsync(string route, CancellationToken cancellationToken)
    {
        RateLimit? rateLimit = await _repository.GetLimitByRouteAsync(route, cancellationToken);
        return rateLimit ?? throw new RateLimitNotFoundException(route);
    }

    public async Task<RateLimit> UpdateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken)
    {
        RateLimit? updated = await _repository.UpdateLimitAsync(rateLimit, cancellationToken);
        return updated ?? throw new RateLimitNotFoundException(rateLimit.Route);
    }

    public async Task DeleteLimitAsync(string route, CancellationToken cancellationToken)
    {
        bool deleted = await _repository.DeleteLimitAsync(route, cancellationToken);
        if (!deleted)
        {
            throw new RateLimitNotFoundException(route);
        }
    }
}