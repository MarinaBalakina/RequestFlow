using RateLimiter.Reader.Domain.Entity;
using RateLimiter.Reader.Infrastructure.Models;
using Riok.Mapperly.Abstractions;

namespace RateLimiter.Reader.Infrastructure.Mappers;

[Mapper]
public partial class RateLimitMapper
{
    [MapperIgnoreSource(nameof(RateLimitDbModel.Id))]
    public partial RateLimit DbModelToRateLimit(RateLimitDbModel model);
}
