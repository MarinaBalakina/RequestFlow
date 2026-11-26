using RateLimiter.Reader.Domain.Entity;

namespace RateLimiter.Reader.Application.Abstractions;

public interface IReaderService
{
    Task InitializeLimitsAsync();

    Task StartSynchronization();

    RateLimit[] GetAllLimits();
    
    RateLimit? GetLimitByRoute(string route);
}
