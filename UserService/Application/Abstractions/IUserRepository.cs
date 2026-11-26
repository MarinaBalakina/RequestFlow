using UserService.Application.Models;
using UserService.Domain.Entity;

namespace UserService.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByLoginAsync(string login, CancellationToken token);

    Task<User> CreateUserAsync(CreateUserCommand createUserCommand, CancellationToken token);

    Task<User?> GetUserByIdAsync(int userId, CancellationToken token);

    Task<User[]> GetUserByNameAsync(string name, string surname, CancellationToken token);

    Task<User?> UpdateUserAsync(int userId, UpdateUserCommand updateUserCommand, CancellationToken token);

    Task<bool> DeleteUserAsync(int id, CancellationToken token);
}
