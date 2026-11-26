using System.Collections.Concurrent;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Domain.Entity;
using RateLimiter.Reader.Domain.Entity.Changes;

namespace RateLimiter.Reader.Application.Services;

public sealed class ReaderService : IReaderService
{
    private readonly IRateLimitRepository _repository;
    private readonly ConcurrentDictionary<string, RateLimit> _limits = new();
    
    public ReaderService(IRateLimitRepository repository)
    {
        _repository = repository;
    }
    
    public async Task InitializeLimitsAsync()
    {
        await foreach (RateLimit rateLimit in _repository.GetAllLimitsAsync())
        {
            _limits[rateLimit.Route] = rateLimit;
        }
    }
    
    public Task StartSynchronization()
    {
        return Task.Factory.StartNew(
            SynchronizeLimitsAsync,
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    private async Task SynchronizeLimitsAsync()
    {
        await foreach (RateLimitChange change in _repository.GetLimitChangesAsync())
        {
            switch (change.Type)
            {
                case RateLimitChangeType.Upsert:
                    if (change.RateLimit is not null)
                    {
                        _limits[change.Route] = change.RateLimit;
                    }
                    break;

                case RateLimitChangeType.Delete:
                    _limits.TryRemove(change.Route, out _);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(change.Type));
            }
        }
    }
    
    public RateLimit[] GetAllLimits()
    {
        return _limits.Values.ToArray();
    }

    public RateLimit? GetLimitByRoute(string route)
    {
        return _limits.TryGetValue(route, out var limit) ? limit : null;
    }
}
