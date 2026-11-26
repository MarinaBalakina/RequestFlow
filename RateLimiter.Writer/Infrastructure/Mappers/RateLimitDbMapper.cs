using RateLimiter.Writer.Domain.Entity;
using RateLimiter.Writer.Infrastructure.Models;
using Riok.Mapperly.Abstractions;

namespace RateLimiter.Writer.Infrastructure.Mappers;

[Mapper]
public partial class RateLimitDbMapper
{
    public partial RateLimitDbModel RateLimitToDbModel(RateLimit rateLimit);
    
    public partial RateLimit DbModelToRateLimit (RateLimitDbModel model);
}