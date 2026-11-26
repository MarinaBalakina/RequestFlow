namespace UserService.Application.Exceptions;

public sealed class UserNotFoundException : Exception
{
    public UserNotFoundException(int userId) : base($"User with id {userId} not found") { }

    public UserNotFoundException(string name, string surname) : base($"User with Name {name} and surname {surname} not found") { }
}