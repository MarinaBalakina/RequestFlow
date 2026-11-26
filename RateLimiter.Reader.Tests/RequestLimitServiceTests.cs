using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using RateLimiter.Reader.Application;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Configuration;
using RateLimiter.Reader.Domain.Entity;

namespace RateLimiter.Reader.Tests;

public sealed class RequestLimitServiceTests
{
    [Theory]
    [InlineData(null, "/a")]
    [InlineData("", "/a")]
    [InlineData("alice", null)]
    [InlineData("alice", " ")]
    public async Task InvalidEventsAreRejectedBeforeStorage(string? user, string? route)
    {
        var (service, limits, blocks, _) = Create();
        await Assert.ThrowsAsync<InvalidRequestEventException>(() => Process(service, user!, route!, "1"));
        Assert.Equal(0, limits.Reads);
        Assert.Equal(0, blocks.Reads);
    }

    [Fact]
    public async Task AllowsExactlyTheLimitAndBlocksTheNextEvent()
    {
        var (service, _, blocks, _) = Create(limit: 2);
        Assert.False((await Process(service, "alice", "/a", "1")).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "2")).IsBlocked);
        BlockStatus result = await Process(service, "alice", "/a", "3");
        Assert.True(result.IsBlocked);
        Assert.Equal(300, result.RetryAfterSeconds);
        Assert.Equal(1, blocks.BlockCalls);
    }

    [Fact]
    public async Task CountsEachUserAndMethodSeparately()
    {
        var (service, limits, blocks, _) = Create(limit: 1);
        limits.Set("/b", 1);
        Assert.False((await Process(service, "alice", "/a", "1")).IsBlocked);
        Assert.False((await Process(service, "alice", "/b", "2")).IsBlocked);
        Assert.False((await Process(service, "bob", "/a", "3")).IsBlocked);
        Assert.True((await Process(service, "alice", "/a", "4")).IsBlocked);
        Assert.False((await blocks.GetBlockStatusAsync("bob", "/a", default)).IsBlocked);
        Assert.False((await blocks.GetBlockStatusAsync("alice", "/b", default)).IsBlocked);
        Assert.True((await blocks.GetBlockStatusAsync("alice", "/a", default)).IsBlocked);
    }

    [Fact]
    public async Task ExistingBanDoesNotApplyToMethodsWithoutLimits()
    {
        var (service, _, _, _) = Create(limit: 1);
        await Process(service, "alice", "/a", "1");
        await Process(service, "alice", "/a", "2");
        Assert.False((await Process(service, "alice", "/unlimited", "3")).IsBlocked);
        Assert.False((await Process(service, "bob", "/unlimited", "4")).IsBlocked);
    }

    [Fact]
    public async Task BlockingOneMethodPreservesQuotaForOtherMethods()
    {
        var (service, limits, _, _) = Create(limit: 1);
        limits.Set("/b", 1);
        Assert.False((await Process(service, "alice", "/b", "1")).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "2")).IsBlocked);
        Assert.True((await Process(service, "alice", "/a", "3")).IsBlocked);
        Assert.True((await Process(service, "alice", "/b", "4")).IsBlocked);
    }

    [Fact]
    public async Task RepeatedKafkaMessageDoesNotUseQuotaTwice()
    {
        var (service, _, _, _) = Create(limit: 2);
        Assert.False((await Process(service, "alice", "/a", "same-offset")).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "same-offset")).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "next-offset")).IsBlocked);
        Assert.True((await Process(service, "alice", "/a", "third-offset")).IsBlocked);
    }

    [Theory]
    [InlineData(59999, true)]
    [InlineData(60000, false)]
    [InlineData(60001, false)]
    public async Task OldEventsExpireAtTheMinuteBoundary(int milliseconds, bool blocked)
    {
        var (service, _, _, clock) = Create(limit: 1);
        await Process(service, "alice", "/a", "1");
        clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
        Assert.Equal(blocked, (await Process(service, "alice", "/a", "2")).IsBlocked);
    }

    [Fact]
    public async Task SlidingWindowKeepsRecentEventsAndRemovesOldOnes()
    {
        var (service, _, _, clock) = Create(limit: 3);
        await Process(service, "alice", "/a", "1");
        await Process(service, "alice", "/a", "2");
        clock.Advance(TimeSpan.FromSeconds(30));
        await Process(service, "alice", "/a", "3");
        clock.Advance(TimeSpan.FromSeconds(30));
        Assert.False((await Process(service, "alice", "/a", "4")).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "5")).IsBlocked);
        Assert.True((await Process(service, "alice", "/a", "6")).IsBlocked);
    }

    [Fact]
    public async Task UsesChangedLimitsOnTheNextEvent()
    {
        var (service, limits, _, _) = Create(limit: 10);
        await Process(service, "alice", "/a", "1");
        await Process(service, "alice", "/a", "2");
        limits.Set("/a", 2);
        Assert.True((await Process(service, "alice", "/a", "3")).IsBlocked);

        limits.Set("/a", 3);
        Assert.False((await Process(service, "bob", "/a", "4")).IsBlocked);
        Assert.False((await Process(service, "bob", "/a", "5")).IsBlocked);
        Assert.False((await Process(service, "bob", "/a", "6")).IsBlocked);
        limits.Remove("/a");
        Assert.False((await Process(service, "bob", "/a", "7")).IsBlocked);
    }

    [Fact]
    public async Task BlockedEventsDoNotRenewBanAndUserCanReturnAfterExpiry()
    {
        var (service, _, blocks, clock) = Create(limit: 1);
        await Process(service, "alice", "/a", "1");
        await Process(service, "alice", "/a", "2");
        clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Equal(60, (await Process(service, "alice", "/a", "3")).RetryAfterSeconds);
        Assert.Equal(1, blocks.BlockCalls);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.False((await blocks.GetBlockStatusAsync("alice", "/a", default)).IsBlocked);
        Assert.False((await Process(service, "alice", "/a", "4")).IsBlocked);
    }

    [Fact]
    public async Task ConcurrentEventsDoNotBypassTheLimit()
    {
        var (service, _, _, _) = Create(limit: 10);
        BlockStatus[] results = await Task.WhenAll(Enumerable.Range(0, 30)
            .Select(i => Task.Run(() => Process(service, "alice", "/a", i.ToString()))));
        Assert.Equal(10, results.Count(r => !r.IsBlocked));
        Assert.Equal(20, results.Count(r => r.IsBlocked));
    }

    [Fact]
    public async Task FailedBlockCanBeRetriedWithTheSameKafkaMessage()
    {
        var (service, _, blocks, _) = Create(limit: 1);
        await Process(service, "alice", "/a", "1");
        var failure = new InvalidOperationException("Redis unavailable");
        blocks.BlockFailure = failure;
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Process(service, "alice", "/a", "2")));
        blocks.BlockFailure = null;
        Assert.True((await Process(service, "alice", "/a", "2")).IsBlocked);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidConfiguredLimitIsNotTreatedAsUnlimited(int limit)
    {
        var (service, _, _, _) = Create(limit);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Process(service, "alice", "/a", "1"));
    }

    private static Task<BlockStatus> Process(RequestLimitService service, string user, string route, string id) =>
        service.ProcessAsync(new RequestEvent(user, route), id, CancellationToken.None);

    private static (RequestLimitService, TestLimits, TestBlocks, TestClock) Create(int limit = 2)
    {
        var limits = new TestLimits();
        limits.Set("/a", limit);
        var clock = new TestClock();
        var blocks = new TestBlocks(clock);
        var service = new RequestLimitService(limits, blocks, Options.Create(new RateLimiterOptions()), clock);
        return (service, limits, blocks, clock);
    }
}

internal sealed class TestLimits : IReaderService
{
    private readonly ConcurrentDictionary<string, RateLimit> _limits = new();
    public int Reads { get; private set; }
    public void Set(string route, int rpm) => _limits[route] = new RateLimit(route, rpm);
    public void Remove(string route) => _limits.TryRemove(route, out _);
    public RateLimit? GetLimitByRoute(string route)
    {
        Reads++;
        return _limits.GetValueOrDefault(route);
    }
    public RateLimit[] GetAllLimits() => _limits.Values.ToArray();
    public Task InitializeLimitsAsync() => Task.CompletedTask;
    public Task StartSynchronization() => Task.CompletedTask;
}

internal sealed class TestClock : TimeProvider
{
    private long _ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _ticks);
    public override DateTimeOffset GetUtcNow() => DateTimeOffset.UnixEpoch.AddTicks(GetTimestamp());
    public void Advance(TimeSpan elapsed) => Interlocked.Add(ref _ticks, elapsed.Ticks);
}

internal sealed class TestBlocks(TestClock clock) : IUserBlockService
{
    private readonly ConcurrentDictionary<(string UserId, string Route), DateTimeOffset> _expires = new();
    public int Reads { get; private set; }
    public int BlockCalls { get; private set; }
    public Exception? BlockFailure { get; set; }
    public Task BlockAsync(string userId, string route, CancellationToken token)
    {
        if (BlockFailure is not null) return Task.FromException(BlockFailure);
        BlockCalls++;
        _expires.TryAdd((userId, route), clock.GetUtcNow() + TimeSpan.FromMinutes(5));
        return Task.CompletedTask;
    }
    public Task<BlockStatus> GetBlockStatusAsync(string userId, string route, CancellationToken token)
    {
        Reads++;
        if (_expires.TryGetValue((userId, route), out var expiry) && expiry > clock.GetUtcNow())
            return Task.FromResult(new BlockStatus(true, (int)Math.Ceiling((expiry - clock.GetUtcNow()).TotalSeconds)));
        _expires.TryRemove((userId, route), out _);
        return Task.FromResult(new BlockStatus(false, 0));
    }
}
