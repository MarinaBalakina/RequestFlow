using UserService.Application.Abstractions;
using UserService.Application.Exceptions;
using UserService.Application.Models;
using UserService.Application.Services;
using UserService.Domain.Entity;
using ApplicationUserService = UserService.Application.Services.UserService;

namespace UserService.Tests;

public sealed class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_HashesPasswordBeforeSaving()
    {
        var repository = new UserRepositoryStub();
        var passwordHasher = new PasswordHasher();
        var service = new ApplicationUserService(repository, passwordHasher);
        var input = new CreateUserModel("alice", "plain-password", "Alice", "Smith", 27);

        User result = await service.CreateUserAsync(input, CancellationToken.None);

        Assert.Same(repository.CreateResult, result);
        Assert.NotNull(repository.LastCreateData);
        Assert.NotEqual(input.Password, repository.LastCreateData.PasswordHash);
        Assert.NotEmpty(repository.LastCreateData.PasswordSalt);
        Assert.True(passwordHasher.Verify(
            input.Password,
            repository.LastCreateData.PasswordHash,
            repository.LastCreateData.PasswordSalt));
    }

    [Fact]
    public async Task CreateUserAsync_WhenLoginExists_ThrowsAndDoesNotSave()
    {
        var repository = new UserRepositoryStub { LoginExists = true };
        var service = new ApplicationUserService(repository, new PasswordHasher());
        var input = new CreateUserModel("alice", "password", "Alice", "Smith", 27);

        await Assert.ThrowsAsync<UserAlreadyExistsException>(
            () => service.CreateUserAsync(input, CancellationToken.None));

        Assert.Null(repository.LastCreateData);
    }

    [Fact]
    public async Task GetUserByNameAsync_ReturnsEveryMatchingUser()
    {
        User first = CreateUser(1, "alice");
        User second = CreateUser(2, "alice-2");
        var repository = new UserRepositoryStub { UsersByName = [first, second] };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        User[] users = await service.GetUserByNameAsync("Alice", "Smith", CancellationToken.None);

        Assert.Equal([first, second], users);
    }

    [Fact]
    public async Task GetUserByNameAsync_WhenNoUsersFound_Throws()
    {
        var service = new ApplicationUserService(new UserRepositoryStub(), new PasswordHasher());

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.GetUserByNameAsync("Nobody", "Here", CancellationToken.None));
    }

    [Fact]
    public async Task GetUserByIdAsync_ReturnsFoundUser()
    {
        User expected = CreateUser(7, "found");
        var repository = new UserRepositoryStub { UserById = expected };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        User result = await service.GetUserByIdAsync(7, CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserDoesNotExist_Throws()
    {
        var repository = new UserRepositoryStub { UserById = null };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.GetUserByIdAsync(404, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_HashesChangedPasswordAndPreservesMissingFields()
    {
        var repository = new UserRepositoryStub();
        var passwordHasher = new PasswordHasher();
        var service = new ApplicationUserService(repository, passwordHasher);
        var input = new UpdateUserModel("new-password", null, "Jones", null);

        User result = await service.UpdateUserAsync(1, input, CancellationToken.None);

        Assert.Same(repository.UpdateResult, result);
        Assert.NotNull(repository.LastUpdateData);
        Assert.NotNull(repository.LastUpdateData.PasswordHash);
        Assert.NotNull(repository.LastUpdateData.PasswordSalt);
        Assert.True(passwordHasher.Verify(
            input.Password!,
            repository.LastUpdateData.PasswordHash,
            repository.LastUpdateData.PasswordSalt));
        Assert.Null(repository.LastUpdateData.Name);
        Assert.Equal("Jones", repository.LastUpdateData.Surname);
        Assert.Null(repository.LastUpdateData.Age);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenRepositoryDidNotDelete_Throws()
    {
        var repository = new UserRepositoryStub { DeleteResult = false };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.DeleteUserAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUserAsync_WhenUserDoesNotExist_Throws()
    {
        var repository = new UserRepositoryStub { UpdateResult = null };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => service.UpdateUserAsync(
                404,
                new UpdateUserModel(null, "New name", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUserAsync_WhenRepositoryDeletes_Completes()
    {
        var repository = new UserRepositoryStub { DeleteResult = true };
        var service = new ApplicationUserService(repository, new PasswordHasher());

        await service.DeleteUserAsync(1, CancellationToken.None);
    }

    private static User CreateUser(int id, string login)
    {
        return new User(id, login, "hash", "salt", "Alice", "Smith", 27);
    }

    private sealed class UserRepositoryStub : IUserRepository
    {
        public bool LoginExists { get; init; }
        public CreateUserCommand? LastCreateData { get; private set; }
        public UpdateUserCommand? LastUpdateData { get; private set; }
        public User CreateResult { get; } = CreateUser(1, "alice");
        public User? UserById { get; init; } = CreateUser(1, "alice");
        public User[] UsersByName { get; init; } = [];
        public User? UpdateResult { get; init; } = CreateUser(1, "alice");
        public bool DeleteResult { get; init; } = true;

        public Task<bool> ExistsByLoginAsync(string login, CancellationToken token)
        {
            return Task.FromResult(LoginExists);
        }

        public Task<User> CreateUserAsync(CreateUserCommand createUserData, CancellationToken token)
        {
            LastCreateData = createUserData;
            return Task.FromResult(CreateResult);
        }

        public Task<User?> GetUserByIdAsync(int userId, CancellationToken token)
        {
            return Task.FromResult(UserById);
        }

        public Task<User[]> GetUserByNameAsync(string name, string surname, CancellationToken token)
        {
            return Task.FromResult(UsersByName);
        }

        public Task<User?> UpdateUserAsync(
            int userId,
            UpdateUserCommand updateUserData,
            CancellationToken token)
        {
            LastUpdateData = updateUserData;
            return Task.FromResult(UpdateResult);
        }

        public Task<bool> DeleteUserAsync(int id, CancellationToken token)
        {
            return Task.FromResult(DeleteResult);
        }
    }
}
