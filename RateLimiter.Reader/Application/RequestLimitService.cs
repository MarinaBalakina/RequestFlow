using Microsoft.Extensions.Options;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Configuration;

namespace RateLimiter.Reader.Application;

public sealed record BlockStatus(bool IsBlocked, int RetryAfterSeconds);

public sealed class RequestLimitService(
    IReaderService limits, IUserBlockService blocks, IOptions<RateLimiterOptions> options, TimeProvider clock)
{
    private readonly Dictionary<(string UserId, string Route), Dictionary<string, long>> _requests = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<BlockStatus> ProcessAsync(RequestEvent request, string messageId,
        CancellationToken cancellationToken)
    {
        request.Validate();
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            long now = clock.GetTimestamp();
            RemoveExpiredRequests(now);

            BlockStatus status = await blocks.GetBlockStatusAsync(request.UserId, request.Route, cancellationToken);
            if (status.IsBlocked)
                return status;

            var limit = limits.GetLimitByRoute(request.Route);
            if (limit is null)
                return new BlockStatus(false, 0);
            if (limit.RequestsPerMinute <= 0)
                throw new InvalidOperationException($"Invalid RPM for route {request.Route}");

            var key = (request.UserId, request.Route);
            if (!_requests.TryGetValue(key, out var window))
            {
                window = new Dictionary<string, long>();
                _requests[key] = window;
            }
            
            window.TryAdd(messageId, now);
            if (window.Count <= limit.RequestsPerMinute)
                return new BlockStatus(false, 0);

            await blocks.BlockAsync(request.UserId, request.Route, cancellationToken);
            _requests.Remove(key);
            return await blocks.GetBlockStatusAsync(request.UserId, request.Route, cancellationToken);
        }
        finally { _lock.Release(); }
    }

    private void RemoveExpiredRequests(long now)
    {
        foreach (var pair in _requests.ToArray())
        {
            var expired = pair.Value.Where(entry => clock.GetElapsedTime(entry.Value, now) >= options.Value.Window);
            foreach (string id in expired.Select(entry => entry.Key).ToArray())
                pair.Value.Remove(id);
            if (pair.Value.Count == 0)
                _requests.Remove(pair.Key);
        }
    }
}
