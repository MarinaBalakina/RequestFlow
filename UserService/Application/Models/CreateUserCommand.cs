namespace UserService.Application.Models;

public sealed record CreateUserCommand(
    string Login,
    string PasswordHash,
    string PasswordSalt,
    string Name,
    string Surname,
    int Age);