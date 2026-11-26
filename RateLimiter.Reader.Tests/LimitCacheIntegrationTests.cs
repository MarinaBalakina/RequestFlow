using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using RateLimiter.Reader.Application.Services;
using RateLimiter.Reader.Configuration;
using RateLimiter.Reader.Infrastructure.Mappers;
using RateLimiter.Reader.Infrastructure.Models;
using RateLimiter.Reader.Infrastructure.Repositories;

namespace RateLimiter.Reader.Tests;

public sealed class LimitCacheIntegrationTests
{
    [IntegrationFact]
    public async Task ReplicaSetSynchronizesUpdatedAndDeletedLimits()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var streamOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27018/?directConnection=true");
        settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandSucceededEvent>(command =>
        {
            if (command.CommandName == "aggregate")
                streamOpened.TrySetResult();
        });

        using var mongo = new MongoClient(settings);
        var hello = await mongo.GetDatabase("admin").RunCommandAsync<BsonDocument>(
            new BsonDocument("hello", 1), cancellationToken: timeout.Token);
        Assert.True(hello.Contains("setName"), "This test requires a MongoDB replica set.");

        string database = $"reader_cache_test_{Guid.NewGuid():N}";
        var collection = mongo.GetDatabase(database).GetCollection<RateLimitDbModel>("rate_limits");
        Task? synchronization = null;
        try
        {
            await collection.InsertManyAsync([
                new RateLimitDbModel { Route = "/a", RequestsPerMinute = 2 },
                new RateLimitDbModel { Route = "/b", RequestsPerMinute = 3 }
            ], cancellationToken: timeout.Token);

            var repository = new RateLimitRepository(
                collection, new RateLimitMapper(), Options.Create(new MongoOptions()));
            var service = new ReaderService(repository);
            await service.InitializeLimitsAsync();
            Assert.Equal(2, service.GetAllLimits().Length);

            synchronization = service.StartSynchronization();
            await streamOpened.Task.WaitAsync(timeout.Token);

            await collection.UpdateOneAsync(x => x.Route == "/a",
                Builders<RateLimitDbModel>.Update.Set(x => x.RequestsPerMinute, 5),
                cancellationToken: timeout.Token);
            await collection.DeleteOneAsync(x => x.Route == "/b", timeout.Token);

            while (service.GetLimitByRoute("/a")?.RequestsPerMinute != 5 ||
                   service.GetLimitByRoute("/b") is not null)
            {
                await Task.Delay(25, timeout.Token);
            }
        }
        finally
        {
            using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await mongo.DropDatabaseAsync(database, cleanupTimeout.Token);
            if (synchronization is not null)
                await synchronization.WaitAsync(cleanupTimeout.Token);
        }
    }
}
