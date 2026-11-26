using FluentValidation;
using UserService.Api.Contracts;

namespace UserService.Api.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.Login).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Password).NotEmpty();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Surname).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Age).InclusiveBetween(0, 150);
    }
}
