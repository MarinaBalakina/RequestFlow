using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Application.Services;
using RateLimiter.Reader.Domain.Entity;

namespace RateLimiter.Reader.Tests;

public sealed class ReaderHostedServiceTests
{
    [Fact]
    public async Task StartsChangeStreamOnlyAfterInitialLoadCompletes()
    {
        var reader = new DeferredReaderService();
        var lifetime = new StubLifetime();
        var hosted = new ReaderHostedService(reader, lifetime, NullLogger<ReaderHostedService>.Instance);

        Task startup = hosted.StartAsync(CancellationToken.None);
        Assert.False(startup.IsCompleted);
        Assert.False(reader.SynchronizationStarted);

        reader.FinishInitialization();
        await startup;

        Assert.True(reader.SynchronizationStarted);
        Assert.False(lifetime.StopRequested);
    }

    private sealed class DeferredReaderService : IReaderService
    {
        private readonly TaskCompletionSource _initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _synchronization = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool SynchronizationStarted { get; private set; }

        public Task InitializeLimitsAsync() => _initialization.Task;

        public Task StartSynchronization()
        {
            SynchronizationStarted = true;
            return _synchronization.Task;
        }

        public RateLimit[] GetAllLimits() => [];
        
        public RateLimit? GetLimitByRoute(string route) => null;

        public void FinishInitialization() => _initialization.SetResult();
    }

    private sealed class StubLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public bool StopRequested { get; private set; }

        public void StopApplication() => StopRequested = true;
    }
}
