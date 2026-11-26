using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RateLimiter.Writer.Api.Contracts;
using RateLimiter.Writer.Api.Mappers;
using RateLimiter.Writer.Application.Abstractions;
using WriterContract = RateLimiter.Writer.Api.Contracts.Writer;

namespace RateLimiter.Writer.Api.Controller;

public class WriterGrpcService : WriterContract.WriterBase
{
    private readonly IRateLimitService _service;
    private readonly IValidator<CreateLimitRequest> _createLimitValidator;
    private readonly IValidator<UpdateLimitRequest> _updateLimitValidator;
    private readonly IValidator<GetLimitByRouteRequest> _getLimitValidator;
    private readonly IValidator<DeleteLimitRequest> _deleteLimitValidator;
    private readonly LimitMapper _mapper;

    public WriterGrpcService(
        IRateLimitService service,
        IValidator<CreateLimitRequest> createLimitValidator,
        IValidator<UpdateLimitRequest> updateLimitValidator,
        IValidator<GetLimitByRouteRequest> getLimitValidator,
        IValidator<DeleteLimitRequest> deleteLimitValidator,
        LimitMapper mapper
    )
    {
        _service = service;
        _createLimitValidator = createLimitValidator;
        _updateLimitValidator = updateLimitValidator;
        _getLimitValidator = getLimitValidator;
        _deleteLimitValidator = deleteLimitValidator;
        _mapper = mapper;
    }

    public override async Task<RateLimitResponse> CreateLimit(CreateLimitRequest request, ServerCallContext context)
    {
        _createLimitValidator.ValidateAndThrow(request);
        
        var rateLimit = await _service.CreateLimitAsync(_mapper.ToDomain(request),  context.CancellationToken);

        return _mapper.ToResponse(rateLimit);
    }

    public override  async Task<RateLimitResponse> GetLimitByRoute(GetLimitByRouteRequest request, ServerCallContext context)
    {
        _getLimitValidator.ValidateAndThrow(request);
        
        var rateLimit = await _service.GetLimitByRouteAsync(request.Route, context.CancellationToken);

        return _mapper.ToResponse(rateLimit);
    }

    public override async Task<RateLimitResponse> UpdateLimit(UpdateLimitRequest request, ServerCallContext context)
    {
        _updateLimitValidator.ValidateAndThrow(request);
        
        var rateLimit = await _service.UpdateLimitAsync(_mapper.ToDomain(request), context.CancellationToken);

        return _mapper.ToResponse(rateLimit);
    }

    public override async Task<Empty> DeleteLimit(DeleteLimitRequest request, ServerCallContext context)
    {
        _deleteLimitValidator.ValidateAndThrow(request);
        
        await _service.DeleteLimitAsync(request.Route, context.CancellationToken);
        
        return new Empty();
    }
}