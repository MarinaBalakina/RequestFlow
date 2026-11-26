using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using RateLimiter.Writer.Application.Exceptions;

namespace RateLimiter.Writer.Api.Interceptors;

public sealed class GrpcExceptionInterceptor : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger;

    public GrpcExceptionInterceptor(ILogger<GrpcExceptionInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (ValidationException exception)
        {
            string message = string.Join(
                "; ",
                exception.Errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));

            throw new RpcException(new Status(StatusCode.InvalidArgument, message));
        }
        catch (RateLimitAlreadyExistsException exception)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, exception.Message));
        }
        catch (RateLimitNotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            throw new RpcException(new Status(StatusCode.Cancelled, "Request was cancelled"));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error"));
        }
    }
}