namespace RateLimiter.Reader.Application.Abstractions;

public interface IUserBlockService
{
    Task BlockAsync(string userId, string route, CancellationToken cancellationToken);
    Task<BlockStatus> GetBlockStatusAsync(string userId, string route, CancellationToken cancellationToken);
}
