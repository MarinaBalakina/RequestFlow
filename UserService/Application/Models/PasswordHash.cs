namespace UserService.Application.Models;

public sealed record PasswordHash(string Hash, string Salt);