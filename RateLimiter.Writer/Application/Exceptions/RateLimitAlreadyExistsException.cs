namespace RateLimiter.Writer.Application.Exceptions;

public sealed class RateLimitAlreadyExistsException : Exception
{
    public RateLimitAlreadyExistsException(string route)
        : base($"Rate limit for route '{route}' already exists")
    {
        Route = route;
    }

    public string Route { get; }
}