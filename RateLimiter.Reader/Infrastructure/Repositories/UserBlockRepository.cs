using Microsoft.Extensions.Options;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Configuration;
using StackExchange.Redis;

namespace RateLimiter.Reader.Infrastructure.Repositories;

public sealed class UserBlockRepository : IUserBlockRepository
{
    private readonly IDatabase _database;
    private readonly string _keyPrefix;

    public UserBlockRepository(
        IConnectionMultiplexer connection,
        IOptions<RedisOptions> options)
    {
        _database = connection.GetDatabase();
        _keyPrefix = options.Value.KeyPrefix;
    }

    public Task SetBlockAsync(
        string userId,
        string route,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database
            .StringSetAsync(GetKey(userId, route), "1", duration, When.NotExists)
            .WaitAsync(cancellationToken);
    }

    public Task<TimeSpan?> GetTtlAsync(
        string userId,
        string route,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return _database
            .KeyTimeToLiveAsync(GetKey(userId, route))
            .WaitAsync(cancellationToken);
    }

    private string GetKey(string userId, string route) =>
        $"{_keyPrefix}:blocked:{userId}:{route}";
}