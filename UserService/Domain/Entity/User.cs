namespace UserService.Domain.Entity;

public sealed record User(
    int Id,
    string Login,
    string PasswordHash,
    string PasswordSalt,
    string Name,
    string Surname,
    int Age);