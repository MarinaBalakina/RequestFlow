using Riok.Mapperly.Abstractions;
using UserService.Api.Contracts;
using UserService.Application.Models;
using UserService.Domain.Entity;

namespace UserService.Api.Mappers;

[Mapper]
public sealed partial class UserContractMapper
{
    public partial CreateUserModel CreateRequestToInput(CreateUserRequest request);

    [MapperIgnoreSource(nameof(User.PasswordHash))]
    [MapperIgnoreSource(nameof(User.PasswordSalt))]
    public partial UserResponse UserToResponse(User user);

    public UpdateUserModel UpdateRequestToInput(UpdateUserRequest request)
    {
        return new UpdateUserModel(
            request.HasPassword ? request.Password : null,
            request.HasName ? request.Name : null,
            request.HasSurname ? request.Surname : null,
            request.HasAge ? request.Age : null);
    }
}
