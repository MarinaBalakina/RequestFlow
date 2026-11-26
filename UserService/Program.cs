using FluentValidation;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;
using UserService.Api.Configuration;
using UserService.Api.Contracts;
using UserService.Api.Interceptors;
using UserService.Api.Mappers;
using UserService.Api.Services;
using UserService.Api.Validators;
using UserService.Application.Abstractions;
using UserService.Infrastructure.Mappers;
using UserService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<PostgresOptions>()
    .BindConfiguration(PostgresOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Postgres connection string is required")
    .ValidateOnStart();

builder.Services
    .AddOptions<RedisOptions>()
    .BindConfiguration(RedisOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Redis connection string is required")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.KeyPrefix),
        "Redis key prefix is required")
    .ValidateOnStart();

builder.Services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
{
    RedisOptions options = serviceProvider
        .GetRequiredService<IOptions<RedisOptions>>()
        .Value;

    return ConnectionMultiplexer.Connect(options.ConnectionString);
});

builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<GrpcExceptionInterceptor>();
    options.Interceptors.Add<IdInterceptor>();
    options.Interceptors.Add<RateLimitInterceptor>();
});

builder.Services.AddSingleton<GrpcExceptionInterceptor>();
builder.Services.AddSingleton<IUserService, UserService.Application.Services.UserService>();
builder.Services.AddSingleton<IPasswordHasher, UserService.Application.Services.PasswordHasher>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IUserBlockRepository, UserBlockRepository>();
builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<UserContractMapper>();
builder.Services.AddSingleton<RateLimitInterceptor>();
builder.Services.AddSingleton<IdInterceptor>();

builder.Services.AddSingleton<
    IUserBlockService,
    UserService.Application.Services.UserBlockService>();

builder.Services.AddSingleton<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddSingleton<IValidator<GetUserByIdRequest>, GetUserByIdRequestValidator>();
builder.Services.AddSingleton<IValidator<GetUserByNameRequest>, GetUserByNameRequestValidator>();
builder.Services.AddSingleton<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
builder.Services.AddSingleton<IValidator<DeleteUserRequest>, DeleteUserRequestValidator>();

builder.Services.AddSingleton(serviceProvider =>
{
    PostgresOptions options = serviceProvider.GetRequiredService<IOptions<PostgresOptions>>().Value;
    return NpgsqlDataSource.Create(options.ConnectionString);
});

var app = builder.Build();

app.MapGrpcService<UserGrpcService>();
app.MapGet("/", () => "UserService gRPC endpoint is running. Use a gRPC client with Protos/user-service.proto.");

app.Run();

public partial class Program;
