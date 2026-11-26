using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UserService.Api.Configuration;
using UserService.Application.Abstractions;

namespace UserService.Infrastructure.Repositories;

public class UserBlockRepository :  IUserBlockRepository
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

    public async Task<bool> IsBlockedAsync(
        string userId,
        string route,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string key = $"{_keyPrefix}:blocked:{userId}:{route}";

        return await _database
            .KeyExistsAsync(key)
            .WaitAsync(cancellationToken);
    }
}