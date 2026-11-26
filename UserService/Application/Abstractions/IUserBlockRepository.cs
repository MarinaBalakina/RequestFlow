namespace UserService.Application.Abstractions;

public interface IUserBlockRepository
{
    Task<bool> IsBlockedAsync(string userId, string route, CancellationToken cancellationToken);
}