using UserService.Application.Abstractions;
using UserService.Application.Exceptions;
using UserService.Application.Models;
using UserService.Domain.Entity;

namespace UserService.Application.Services;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<User> CreateUserAsync(CreateUserModel createUserModel, CancellationToken token)
    {
        bool userExists = await _userRepository.ExistsByLoginAsync(createUserModel.Login, token);

        if (userExists) throw new UserAlreadyExistsException(createUserModel.Login);

        PasswordHash hash = _passwordHasher.Hash(createUserModel.Password);

        CreateUserCommand createUserData = new CreateUserCommand(
            createUserModel.Login,
            hash.Hash,
            hash.Salt,
            createUserModel.Name,
            createUserModel.Surname,
            createUserModel.Age);

        return await _userRepository.CreateUserAsync(createUserData, token);
    }

    public async Task<User> GetUserByIdAsync(int id, CancellationToken token)
    {
        User? found = await _userRepository.GetUserByIdAsync(id, token);

        if (found is null) throw new UserNotFoundException(id);

        return found;
    }

    public async Task<User[]> GetUserByNameAsync(string name, string surname, CancellationToken token)
    {
        User[] found = await _userRepository.GetUserByNameAsync(name, surname, token);

        if (found.Length == 0) throw new UserNotFoundException(name, surname);

        return found;
    }

    public async Task<User> UpdateUserAsync(int userId, UpdateUserModel updateUserModel, CancellationToken token)
    {
        PasswordHash? passwordHash = null;

        if (updateUserModel.Password is not null)
        {
            passwordHash = _passwordHasher.Hash(updateUserModel.Password);
        }


        UpdateUserCommand userData = new UpdateUserCommand(
            passwordHash?.Hash, passwordHash?.Salt, updateUserModel.Name, updateUserModel.Surname, updateUserModel.Age);

        User? newUser = await _userRepository.UpdateUserAsync(userId, userData, token);

        if (newUser is null) throw new UserNotFoundException(userId);

        return newUser;
    }

    public async Task DeleteUserAsync(int id, CancellationToken token)
    {
        bool userDeleted = await _userRepository.DeleteUserAsync(id, token);

        if (!userDeleted) throw new UserNotFoundException(id);
    }
}
