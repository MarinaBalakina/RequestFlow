using RateLimiter.Writer.Domain.Entity;

namespace RateLimiter.Writer.Application.Abstractions;

public interface IRateLimitService
{
    Task<RateLimit> CreateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken);

    Task<RateLimit> GetLimitByRouteAsync(string route, CancellationToken cancellationToken);

    Task<RateLimit> UpdateLimitAsync(RateLimit rateLimit, CancellationToken cancellationToken);

    Task DeleteLimitAsync(string route, CancellationToken cancellationToken);
}