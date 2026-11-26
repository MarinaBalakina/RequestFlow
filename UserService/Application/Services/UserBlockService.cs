using UserService.Application.Abstractions;

namespace UserService.Application.Services;

public sealed class UserBlockService : IUserBlockService
{
    private readonly IUserBlockRepository _repository;

    public UserBlockService(IUserBlockRepository repository)
    {
        _repository = repository;
    }

    public Task<bool> IsBlockedAsync(
        string userId,
        string route,
        CancellationToken cancellationToken)
    {
        return _repository.IsBlockedAsync(
            userId,
            route,
            cancellationToken);
    }
}