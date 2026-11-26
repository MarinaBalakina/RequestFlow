using Microsoft.Extensions.Options;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Configuration;

namespace RateLimiter.Reader.Application.Services;

public sealed class UserBlockService(
    IUserBlockRepository repository,
    IOptions<RedisOptions> options) : IUserBlockService
{
    public Task BlockAsync(
        string userId,
        string route,
        CancellationToken cancellationToken)
    {
        return repository.SetBlockAsync(
            userId, route, options.Value.BlockDuration, cancellationToken);
    }

    public async Task<BlockStatus> GetBlockStatusAsync(
        string userId,
        string route,
        CancellationToken cancellationToken)
    {
        TimeSpan? ttl = await repository.GetTtlAsync(
            userId, route, cancellationToken);

        if (ttl is null || ttl.Value <= TimeSpan.Zero)
            return new BlockStatus(false, 0);

        return new BlockStatus(true, (int)Math.Ceiling(ttl.Value.TotalSeconds));
    }
}