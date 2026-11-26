using FluentValidation;
using UserService.Api.Contracts;

namespace UserService.Api.Validators;

public sealed class GetUserByNameRequestValidator : AbstractValidator<GetUserByNameRequest>
{
    public GetUserByNameRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Surname).NotEmpty().MaximumLength(100);
    }
}
