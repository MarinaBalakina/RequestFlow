namespace RateLimiter.Reader.Application;

public sealed class InvalidRequestEventException(string message) : ArgumentException(message);

public sealed record RequestEvent(string UserId, string Route)
{
    public const int MaxUserIdLength = 128;
    public const int MaxRouteLength = 50;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(UserId) || UserId.Length > MaxUserIdLength)
            throw new InvalidRequestEventException($"userId must contain 1..{MaxUserIdLength} characters");
        if (string.IsNullOrWhiteSpace(Route) || Route.Length > MaxRouteLength)
            throw new InvalidRequestEventException($"route must contain 1..{MaxRouteLength} characters");
    }
}
