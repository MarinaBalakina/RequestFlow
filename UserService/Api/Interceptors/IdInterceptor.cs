using System.Globalization;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace UserService.Api.Interceptors;

public sealed class IdInterceptor : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        string? userId = context.RequestHeaders.GetValue("user_id");
        
        if (!int.TryParse(userId, NumberStyles.None, CultureInfo.InvariantCulture, out int id) || id <= 0)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated,
                "Authentication required. Specify a valid user_id header."));
        }

        return continuation(request, context);
    }
}