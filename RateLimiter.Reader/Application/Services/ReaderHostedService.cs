using RateLimiter.Reader.Application.Abstractions;

namespace RateLimiter.Reader.Application.Services;

public sealed class ReaderHostedService : IHostedService
{
    private readonly IReaderService _readerService;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ReaderHostedService> _logger;

    public ReaderHostedService(
        IReaderService readerService,
        IHostApplicationLifetime lifetime,
        ILogger<ReaderHostedService> logger)
    {
        _readerService = readerService;
        _lifetime = lifetime;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _readerService.InitializeLimitsAsync();

        Task synchronization = _readerService.StartSynchronization();
        _ = ObserveSynchronizationAsync(synchronization);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task ObserveSynchronizationAsync(Task synchronization)
    {
        try
        {
            await synchronization;
            _logger.LogError("MongoDB change stream stopped unexpectedly");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "MongoDB change stream failed");
        }

        _lifetime.StopApplication();
    }
}
