using FluentValidation;
using RateLimiter.Writer.Api.Contracts;

namespace RateLimiter.Writer.Api.Validators;

public class GetLimitByRouteRequestValidator : AbstractValidator<GetLimitByRouteRequest>
{
    public GetLimitByRouteRequestValidator()
    {
        RuleFor(x => x.Route).NotEmpty().MaximumLength(50);
    }
}