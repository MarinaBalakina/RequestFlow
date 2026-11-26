namespace RateLimiter.Reader.Application.Abstractions;

public interface IUserBlockRepository
{
    Task SetBlockAsync(string userId, string route, TimeSpan duration, CancellationToken cancellationToken);

    Task<TimeSpan?> GetTtlAsync(string userId, string route, CancellationToken cancellationToken);
}