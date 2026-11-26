namespace RateLimiter.Reader.Domain.Entity;

public sealed record RateLimit(
    string Route,
    int RequestsPerMinute
);