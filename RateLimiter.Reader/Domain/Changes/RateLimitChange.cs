namespace RateLimiter.Reader.Domain.Entity.Changes;

public sealed record RateLimitChange(
    RateLimitChangeType Type,
    string Route,
    RateLimit? RateLimit
);