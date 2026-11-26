namespace UserService.Application.Models;

public sealed record UpdateUserModel(string? Password, string? Name, string? Surname, int? Age);