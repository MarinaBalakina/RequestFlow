using Grpc.Core;
using Grpc.Core.Interceptors;
using UserService.Application.Abstractions;

namespace UserService.Api.Interceptors;

public sealed class RateLimitInterceptor : Interceptor
{
    private readonly IUserBlockService _userBlockService;

    public RateLimitInterceptor(IUserBlockService userBlockService)
    {
        _userBlockService = userBlockService;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        string userId = context.RequestHeaders.GetValue("user_id")!;

        string route = context.Method;

        bool isBlocked = await _userBlockService.IsBlockedAsync(
            userId,
            route,
            context.CancellationToken);

        if (isBlocked)
        {
            throw new RpcException(new Status(
                StatusCode.ResourceExhausted,
                $"Requests per minute limit exceeded for endpoint '{route}'."));
        }

        return await continuation(request, context);
    }
}