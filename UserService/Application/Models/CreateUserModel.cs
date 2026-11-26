namespace UserService.Application.Models;

public sealed record CreateUserModel(
    string Login,
    string Password,
    string Name,
    string Surname,
    int Age);