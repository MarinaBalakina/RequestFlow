using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UserService.Api.Contracts;
using UserService.Api.Mappers;
using UserService.Application.Abstractions;
using UserService.Domain.Entity;

namespace UserService.Api.Services;

public sealed class UserGrpcService : UsersService.UsersServiceBase
{
    private readonly IUserService _userService;
    private readonly UserContractMapper _mapper;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<GetUserByIdRequest> _getByIdValidator;
    private readonly IValidator<GetUserByNameRequest> _getByNameValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;
    private readonly IValidator<DeleteUserRequest> _deleteValidator;

    public UserGrpcService(
        IUserService userService,
        UserContractMapper mapper,
        IValidator<CreateUserRequest> createValidator,
        IValidator<GetUserByIdRequest> getByIdValidator,
        IValidator<GetUserByNameRequest> getByNameValidator,
        IValidator<UpdateUserRequest> updateValidator,
        IValidator<DeleteUserRequest> deleteValidator)
    {
        _userService = userService;
        _mapper = mapper;
        _createValidator = createValidator;
        _getByIdValidator = getByIdValidator;
        _getByNameValidator = getByNameValidator;
        _updateValidator = updateValidator;
        _deleteValidator = deleteValidator;
    }

    public override async Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        _createValidator.ValidateAndThrow(request);
        User created = await _userService.CreateUserAsync(
            _mapper.CreateRequestToInput(request),
            context.CancellationToken);

        return _mapper.UserToResponse(created);
    }

    public override async Task<UserResponse> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        _getByIdValidator.ValidateAndThrow(request);
        User user = await _userService.GetUserByIdAsync(request.Id, context.CancellationToken);
        return _mapper.UserToResponse(user);
    }

    public override async Task<GetUsersByNameResponse> GetUserByName(
        GetUserByNameRequest request,
        ServerCallContext context)
    {
        _getByNameValidator.ValidateAndThrow(request);
        User[] users = await _userService.GetUserByNameAsync(
            request.Name,
            request.Surname,
            context.CancellationToken);

        var response = new GetUsersByNameResponse();
        response.Users.AddRange(users.Select(_mapper.UserToResponse));
        return response;
    }

    public override async Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        _updateValidator.ValidateAndThrow(request);
        User updated = await _userService.UpdateUserAsync(
            request.Id,
            _mapper.UpdateRequestToInput(request),
            context.CancellationToken);

        return _mapper.UserToResponse(updated);
    }

    public override async Task<Empty> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        _deleteValidator.ValidateAndThrow(request);
        await _userService.DeleteUserAsync(request.Id, context.CancellationToken);
        return new Empty();
    }
}
