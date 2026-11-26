using UserService.Domain.Entity;
using UserService.Application.Models;

namespace UserService.Application.Abstractions;

public interface IUserService
{
    Task<User> CreateUserAsync(CreateUserModel createUserModel, CancellationToken token);

    Task<User> GetUserByIdAsync(int id, CancellationToken token);

    Task<User[]> GetUserByNameAsync(string name, string surname, CancellationToken token);

    Task<User> UpdateUserAsync(int userId, UpdateUserModel updateUserModel, CancellationToken token);

    Task DeleteUserAsync(int id, CancellationToken token);
}
