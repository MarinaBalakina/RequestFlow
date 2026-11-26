using FluentValidation;
using UserService.Api.Contracts;

namespace UserService.Api.Validators;

public sealed class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequest>
{
    public DeleteUserRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
