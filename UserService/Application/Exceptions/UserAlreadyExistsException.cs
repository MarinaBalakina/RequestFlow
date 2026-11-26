namespace UserService.Application.Exceptions;

public sealed class UserAlreadyExistsException : Exception
{
    public string Login { get; }

    public UserAlreadyExistsException(string login) : base($"User with login {login} is already registered")
    {
        Login = login;
    }
}