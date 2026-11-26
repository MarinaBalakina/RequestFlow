using FluentValidation;
using RateLimiter.Writer.Api.Contracts;

namespace RateLimiter.Writer.Api.Validators;

public class DeleteLimitRequestValidator :AbstractValidator<DeleteLimitRequest>
{
    public DeleteLimitRequestValidator()
    {
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
    }

}