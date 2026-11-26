using FluentValidation;
using UserService.Api.Contracts;

namespace UserService.Api.Validators;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
        RuleFor(request => request)
            .Must(request => request.HasPassword || request.HasName || request.HasSurname || request.HasAge)
            .WithMessage("At least one field must be provided for update");

        RuleFor(request => request.Password)
            .NotEmpty()
            .When(request => request.HasPassword);

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(100)
            .When(request => request.HasName);

        RuleFor(request => request.Surname)
            .NotEmpty()
            .MaximumLength(100)
            .When(request => request.HasSurname);

        RuleFor(request => request.Age)
            .InclusiveBetween(0, 150)
            .When(request => request.HasAge);
    }
}
