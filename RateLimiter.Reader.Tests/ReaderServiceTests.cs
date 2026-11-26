using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Application.Services;
using RateLimiter.Reader.Controllers;
using RateLimiter.Reader.Domain.Entity;
using RateLimiter.Reader.Domain.Entity.Changes;
using Google.Protobuf.WellKnownTypes;

namespace RateLimiter.Reader.Tests;

public sealed class ReaderServiceTests
{
    [Fact]
    public async Task LoadsStreamedLimitsAndAppliesChangesToTheCache()
    {
        var initial = Enumerable.Range(0, 1001)
            .Select(index => new RateLimit($"/route/{index}", 100))
            .ToArray();
        var changes = new[]
        {
            new RateLimitChange(RateLimitChangeType.Upsert, "/route/0", new RateLimit("/route/0", 20)),
            new RateLimitChange(RateLimitChangeType.Delete, "/route/1", null),
            new RateLimitChange(RateLimitChangeType.Upsert, "/new", new RateLimit("/new", 30))
        };
        var service = new ReaderService(new StubRepository(initial, changes));

        await service.InitializeLimitsAsync();
        Assert.Equal(1001, service.GetAllLimits().Length);

        await service.StartSynchronization();

        var limits = service.GetAllLimits().ToDictionary(limit => limit.Route);
        Assert.Equal(1001, limits.Count);
        Assert.Equal(20, limits["/route/0"].RequestsPerMinute);
        Assert.False(limits.ContainsKey("/route/1"));
        Assert.Equal(30, limits["/new"].RequestsPerMinute);
    }

    [Fact]
    public async Task GrpcMethodReturnsTheCurrentInMemorySnapshot()
    {
        var service = new ReaderService(new StubRepository(
            [new RateLimit("/orders", 120)],
            []));
        await service.InitializeLimitsAsync();
        var grpc = new ReaderGrpcService(service);
        var response = await grpc.GetAllLimits(new Empty(), null!);

        var limit = Assert.Single(response.Limits);
        Assert.Equal("/orders", limit.Route);
        Assert.Equal(120, limit.RequestsPerMinute);
    }

    private sealed class StubRepository : IRateLimitRepository
    {
        private readonly IReadOnlyList<RateLimit> _initial;
        private readonly IReadOnlyList<RateLimitChange> _changes;

        public StubRepository(IReadOnlyList<RateLimit> initial, IReadOnlyList<RateLimitChange> changes)
        {
            _initial = initial;
            _changes = changes;
        }

        public async IAsyncEnumerable<RateLimit> GetAllLimitsAsync()
        {
            foreach (var limit in _initial)
            {
                yield return limit;
            }

            await Task.CompletedTask;
        }

        public async IAsyncEnumerable<RateLimitChange> GetLimitChangesAsync()
        {
            foreach (var change in _changes)
            {
                yield return change;
            }

            await Task.CompletedTask;
        }
    }
}
