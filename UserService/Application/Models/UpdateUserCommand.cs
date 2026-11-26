namespace UserService.Application.Models;

public sealed record UpdateUserCommand(
    string? PasswordHash,
    string? PasswordSalt,
    string? Name,
    string? Surname,
    int? Age);