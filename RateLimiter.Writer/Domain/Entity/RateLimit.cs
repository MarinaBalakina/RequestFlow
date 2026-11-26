namespace RateLimiter.Writer.Domain.Entity;

public sealed record RateLimit(string Route, int RequestsPerMinute);