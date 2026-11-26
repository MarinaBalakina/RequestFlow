using RateLimiter.Reader.Domain.Entity;
using RateLimiter.Reader.Domain.Entity.Changes;

namespace RateLimiter.Reader.Application.Abstractions;

public interface IRateLimitRepository
{
    IAsyncEnumerable<RateLimit> GetAllLimitsAsync();

    IAsyncEnumerable<RateLimitChange> GetLimitChangesAsync();
}