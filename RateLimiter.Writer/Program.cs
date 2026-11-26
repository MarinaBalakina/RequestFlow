using FluentValidation;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RateLimiter.Writer.Api.Configuration;
using RateLimiter.Writer.Api.Contracts;
using RateLimiter.Writer.Api.Controller;
using RateLimiter.Writer.Api.Interceptors;
using RateLimiter.Writer.Api.Mappers;
using RateLimiter.Writer.Api.Validators;
using RateLimiter.Writer.Application.Abstractions;
using RateLimiter.Writer.Application.Services;
using RateLimiter.Writer.Infrastructure.Database;
using RateLimiter.Writer.Infrastructure.Mappers;
using RateLimiter.Writer.Infrastructure.Models;
using RateLimiter.Writer.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<MongoDbOptions>()
    .BindConfiguration(MongoDbOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "MongoDB connection string is required")
    .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "MongoDB database name is required")
    .Validate(options => !string.IsNullOrWhiteSpace(options.CollectionName), "MongoDB collection name is required")
    .ValidateOnStart();

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

builder.Services.AddSingleton<GrpcExceptionInterceptor>();
builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
builder.Services.AddSingleton<IRateLimitRepository, RateLimitRepository>();
builder.Services.AddSingleton<RateLimitDbMapper>();

builder.Services.AddSingleton<LimitMapper>();

builder.Services.AddSingleton<IValidator<CreateLimitRequest>, CreateLimitRequestValidator>();
builder.Services.AddSingleton<IValidator<GetLimitByRouteRequest>, GetLimitByRouteRequestValidator>();
builder.Services.AddSingleton<IValidator<UpdateLimitRequest>, UpdateLimitRequestValidator>();
builder.Services.AddSingleton<IValidator<DeleteLimitRequest>, DeleteLimitRequestValidator>();

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    MongoDbOptions options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    return new MongoClient(options.ConnectionString);
});

builder.Services.AddSingleton(serviceProvider =>
{
    MongoDbOptions options = serviceProvider.GetRequiredService<IOptions<MongoDbOptions>>().Value;
    IMongoClient client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(options.DatabaseName).GetCollection<RateLimitDbModel>(options.CollectionName);
});

builder.Services.AddHostedService<RateLimitDb>();

var app = builder.Build();

app.MapGrpcService<WriterGrpcService>();
app.MapGet("/", () => "RateLimiter.Writer gRPC endpoint is running. Use Protos/writer-service.proto.");

await app.RunAsync();

public partial class Program;