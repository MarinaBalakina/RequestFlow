namespace UserService.Application.Abstractions;

public interface IUserBlockService
{
    Task<bool> IsBlockedAsync(string userId, string route, CancellationToken cancellationToken);
}