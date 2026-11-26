using UserService.Application.Models;

namespace UserService.Application.Abstractions;

public interface IPasswordHasher
{
    PasswordHash Hash(string password);

    bool Verify(string password, string hash, string salt);
}