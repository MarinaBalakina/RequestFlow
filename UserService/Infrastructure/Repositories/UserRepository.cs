using Dapper;
using Npgsql;
using UserService.Application.Abstractions;
using UserService.Application.Models;
using UserService.Domain.Entity;
using UserService.Infrastructure.Mappers;
using UserService.Infrastructure.Models;

namespace UserService.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private const string UserColumns =
        "id, login, password_hash as \"PasswordHash\", password_salt as \"PasswordSalt\", name, surname, age";

    private readonly NpgsqlDataSource _source;
    private readonly UserMapper _userMapper;

    public UserRepository(NpgsqlDataSource source, UserMapper userMapper)
    {
        _source = source;
        _userMapper = userMapper;
    }

    public async Task<bool> ExistsByLoginAsync(string login, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_login", login);

        var command = new CommandDefinition(
            "select exists_user_by_login(@input_login)",
            parameters,
            cancellationToken: token);

        return await connection.QuerySingleAsync<bool>(command);
    }

    public async Task<User> CreateUserAsync(CreateUserCommand createUserCommand, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_login", createUserCommand.Login);
        parameters.Add("input_password_hash", createUserCommand.PasswordHash);
        parameters.Add("input_password_salt", createUserCommand.PasswordSalt);
        parameters.Add("input_name", createUserCommand.Name);
        parameters.Add("input_surname", createUserCommand.Surname);
        parameters.Add("input_age", createUserCommand.Age);

        var command = new CommandDefinition(
            $"select {UserColumns} from create_user(@input_login, @input_password_hash, @input_password_salt, @input_name, @input_surname, @input_age)",
            parameters,
            cancellationToken: token);

        UserDbModel dbUser = await connection.QuerySingleAsync<UserDbModel>(command);
        return _userMapper.UserDbModelToUser(dbUser);
    }

    public async Task<User?> GetUserByIdAsync(int userId, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_id", userId);

        var command = new CommandDefinition(
            $"select {UserColumns} from get_user_by_id(@input_id)",
            parameters,
            cancellationToken: token);

        UserDbModel? dbUser = await connection.QuerySingleOrDefaultAsync<UserDbModel>(command);
        return dbUser is null ? null : _userMapper.UserDbModelToUser(dbUser);
    }

    public async Task<User[]> GetUserByNameAsync(string name, string surname, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_name", name);
        parameters.Add("input_surname", surname);

        var command = new CommandDefinition(
            $"select {UserColumns} from get_user_by_name(@input_name, @input_surname)",
            parameters,
            cancellationToken: token);

        IEnumerable<UserDbModel> dbUsers = await connection.QueryAsync<UserDbModel>(command);
        return dbUsers.Select(_userMapper.UserDbModelToUser).ToArray();
    }

    public async Task<User?> UpdateUserAsync(int userId, UpdateUserCommand updateUserCommand, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_id", userId);
        parameters.Add("input_password_hash", updateUserCommand.PasswordHash);
        parameters.Add("input_password_salt", updateUserCommand.PasswordSalt);
        parameters.Add("input_name", updateUserCommand.Name);
        parameters.Add("input_surname", updateUserCommand.Surname);
        parameters.Add("input_age", updateUserCommand.Age);

        var command = new CommandDefinition(
            $"select {UserColumns} from update_user(@input_id, @input_password_hash, @input_password_salt, @input_name, @input_surname, @input_age)",
            parameters,
            cancellationToken: token);

        UserDbModel? dbUser = await connection.QuerySingleOrDefaultAsync<UserDbModel>(command);
        return dbUser is null ? null : _userMapper.UserDbModelToUser(dbUser);
    }

    public async Task<bool> DeleteUserAsync(int id, CancellationToken token)
    {
        await using var connection = await _source.OpenConnectionAsync(token);

        var parameters = new DynamicParameters();
        parameters.Add("input_id", id);

        var command = new CommandDefinition(
            "select delete_user(@input_id)",
            parameters,
            cancellationToken: token);

        return await connection.QuerySingleAsync<bool>(command);
    }
}
