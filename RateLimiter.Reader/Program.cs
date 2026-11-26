using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RateLimiter.Reader.Application;
using RateLimiter.Reader.Application.Abstractions;
using RateLimiter.Reader.Application.Services;
using RateLimiter.Reader.Configuration;
using RateLimiter.Reader.Controllers;
using RateLimiter.Reader.Infrastructure;
using RateLimiter.Reader.Infrastructure.Mappers;
using RateLimiter.Reader.Infrastructure.Models;
using RateLimiter.Reader.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddOptions<KafkaOptions>().BindConfiguration("Kafka")
    .Validate(o => !string.IsNullOrWhiteSpace(o.BootstrapServers) &&
                   !string.IsNullOrWhiteSpace(o.Topic) && !string.IsNullOrWhiteSpace(o.GroupId),
        "Kafka BootstrapServers, Topic and GroupId are required")
    .Validate(o => o.RetryDelay > TimeSpan.Zero, "Kafka RetryDelay must be positive").ValidateOnStart();
builder.Services.AddOptions<MongoOptions>().BindConfiguration("MongoDb")
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString) &&
                   !string.IsNullOrWhiteSpace(o.DatabaseName) && !string.IsNullOrWhiteSpace(o.CollectionName),
        "MongoDb connection and collection settings are required")
    .Validate(o => o.SynchronizationPollInterval > TimeSpan.Zero, "MongoDb polling interval must be positive")
    .ValidateOnStart();
builder.Services.AddOptions<RateLimiterOptions>().BindConfiguration("RateLimiter")
    .Validate(o => o.Window == TimeSpan.FromMinutes(1), "RPM window must be one minute")
    .ValidateOnStart();

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<RedisOptions>()
    .BindConfiguration(RedisOptions.SectionName)
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
        "Redis connection string is required")
    .Validate(o => !string.IsNullOrWhiteSpace(o.KeyPrefix),
        "Redis key prefix is required")
    .Validate(o => o.BlockDuration == TimeSpan.FromMinutes(5),
        "Block duration must be five minutes")
    .ValidateOnStart();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    RedisOptions settings = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(settings.ConnectionString);
});

builder.Services.AddSingleton<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddSingleton<IUserBlockService, UserBlockService>();
builder.Services.AddSingleton<RequestLimitService>();
builder.Services.AddSingleton(sp =>
{
    MongoOptions settings = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    return sp.GetRequiredService<IMongoClient>().GetDatabase(settings.DatabaseName)
        .GetCollection<RateLimitDbModel>(settings.CollectionName);
});
builder.Services.AddSingleton<RateLimitMapper>();
builder.Services.AddSingleton<IRateLimitRepository, RateLimitRepository>();
builder.Services.AddSingleton<IReaderService, ReaderService>();
builder.Services.AddHostedService<ReaderHostedService>();
builder.Services.AddHostedService<KafkaRequestConsumer>();

var app = builder.Build();

app.MapGrpcService<ReaderGrpcService>();

await app.RunAsync();