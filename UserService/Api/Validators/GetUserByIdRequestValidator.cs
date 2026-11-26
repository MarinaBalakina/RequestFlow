using FluentValidation;
using UserService.Api.Contracts;

namespace UserService.Api.Validators;

public sealed class GetUserByIdRequestValidator : AbstractValidator<GetUserByIdRequest>
{
    public GetUserByIdRequestValidator()
    {
        RuleFor(request => request.Id).GreaterThan(0);
    }
}
