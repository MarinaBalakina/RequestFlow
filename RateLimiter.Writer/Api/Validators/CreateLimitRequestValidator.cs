using FluentValidation;
using RateLimiter.Writer.Api.Contracts;

namespace RateLimiter.Writer.Api.Validators;

public sealed class CreateLimitRequestValidator : AbstractValidator<CreateLimitRequest>
{
    public CreateLimitRequestValidator()
    {
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RequestsPerMinute).GreaterThan(0);
    }
}