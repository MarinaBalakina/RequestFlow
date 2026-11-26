using RateLimiter.Writer.Api.Contracts;
using RateLimiter.Writer.Domain.Entity;
using Riok.Mapperly.Abstractions;

namespace RateLimiter.Writer.Api.Mappers;

[Mapper]
public partial class LimitMapper
{
    public partial RateLimit ToDomain(CreateLimitRequest request);
    
    public partial RateLimit ToDomain(UpdateLimitRequest request);
    
    public partial RateLimitResponse ToResponse(RateLimit rateLimit);
}