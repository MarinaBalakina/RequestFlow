using UserService.Api.Contracts;
using UserService.Api.Validators;

namespace UserService.Tests;

public sealed class RequestValidatorTests
{
    [Fact]
    public void CreateUserRequest_WithValidFields_IsValid()
    {
        var request = new CreateUserRequest
        {
            Login = "alice",
            Password = "password",
            Name = "Alice",
            Surname = "Smith",
            Age = 27,
        };

        var result = new CreateUserRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateUserRequest_WithInvalidFields_IsRejected()
    {
        var request = new CreateUserRequest
        {
            Login = string.Empty,
            Password = string.Empty,
            Name = string.Empty,
            Surname = string.Empty,
            Age = 151,
        };

        var result = new CreateUserRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateUserRequest.Login));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateUserRequest.Age));
    }

    [Fact]
    public void UpdateUserRequest_WithoutChanges_IsRejected()
    {
        var request = new UpdateUserRequest { Id = 1 };

        var result = new UpdateUserRequestValidator().Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateUserRequest_WithExplicitZeroAge_IsValid()
    {
        var request = new UpdateUserRequest { Id = 1, Age = 0 };

        var result = new UpdateUserRequestValidator().Validate(request);

        Assert.True(request.HasAge);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void GetUserByNameRequest_RequiresBothNames()
    {
        var request = new GetUserByNameRequest { Name = "Alice" };

        var result = new GetUserByNameRequestValidator().Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetUserByNameRequest.Surname));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IdRequests_RequirePositiveId(int id)
    {
        var getResult = new GetUserByIdRequestValidator().Validate(new GetUserByIdRequest { Id = id });
        var deleteResult = new DeleteUserRequestValidator().Validate(new DeleteUserRequest { Id = id });

        Assert.False(getResult.IsValid);
        Assert.False(deleteResult.IsValid);
    }
}
