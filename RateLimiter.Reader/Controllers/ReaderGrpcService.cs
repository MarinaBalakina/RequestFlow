using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using RateLimiter.Reader.Application.Abstractions;

namespace RateLimiter.Reader.Controllers;

public class ReaderGrpcService : Reader.ReaderBase
{
    private readonly IReaderService _readerService;

    public ReaderGrpcService(IReaderService readerService)
    {
        _readerService = readerService;
    }

    public override Task<GetAllLimitsResponse> GetAllLimits(Empty request, ServerCallContext context)
    {
        var response = new GetAllLimitsResponse();
        foreach (var limit in _readerService.GetAllLimits())
            response.Limits.Add(new RateLimitItem { Route = limit.Route, RequestsPerMinute = limit.RequestsPerMinute });
        return Task.FromResult(response);
    }
}
