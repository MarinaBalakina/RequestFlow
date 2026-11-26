using FluentValidation;
using RateLimiter.Writer.Api.Contracts;

namespace RateLimiter.Writer.Api.Validators;

public class UpdateLimitRequestValidator : AbstractValidator<UpdateLimitRequest>
{
    public UpdateLimitRequestValidator()
    {
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RequestsPerMinute).GreaterThan(0);
    }
}