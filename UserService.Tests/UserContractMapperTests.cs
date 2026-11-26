using UserService.Api.Contracts;
using UserService.Api.Mappers;
using UserService.Domain.Entity;

namespace UserService.Tests;

public sealed class UserContractMapperTests
{
    [Fact]
    public void UserToResponse_DoesNotExposePasswordData()
    {
        var user = new User(1, "alice", "secret-hash", "secret-salt", "Alice", "Smith", 27);
        var mapper = new UserContractMapper();

        UserResponse response = mapper.UserToResponse(user);

        Assert.Equal(1, response.Id);
        Assert.Equal("alice", response.Login);
        Assert.Equal("Alice", response.Name);
        Assert.Equal("Smith", response.Surname);
        Assert.Equal(27, response.Age);
        Assert.DoesNotContain("secret", response.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateRequestToInput_PreservesOptionalFieldPresence()
    {
        var mapper = new UserContractMapper();
        var request = new UpdateUserRequest { Id = 1, Name = "Alice", Age = 0 };

        var input = mapper.UpdateRequestToInput(request);

        Assert.Null(input.Password);
        Assert.Equal("Alice", input.Name);
        Assert.Null(input.Surname);
        Assert.Equal(0, input.Age);
    }
}
