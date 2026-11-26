using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UserRequestsKafkaGenerator.Configuration;
using UserRequestsKafkaGenerator.Models;
using UserRequestsKafkaGenerator.Services;
using UserRequestsKafkaGenerator.Validators;

if (args.Length > 1)
{
    Console.Error.WriteLine("Передайте один аргумент: путь к appsettings.json.");
    Environment.ExitCode = 1;
    return;
}

var configurationPath = Path.GetFullPath(
    args.Length == 1
        ? args[0]
        : Path.Combine(AppContext.BaseDirectory, "appsettings.json"));

Console.WriteLine($"Файл конфигурации: {configurationPath}");

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings
    {
        ContentRootPath = Path.GetDirectoryName(configurationPath)!
    });

builder.Configuration.Sources.Clear();

builder.Configuration.AddJsonFile(source =>
{
    source.Path = Path.GetFileName(configurationPath);
    source.Optional = false;
    source.ReloadOnChange = true;
    source.OnLoadException = context =>
    {
        Console.Error.WriteLine(
            $"Не удалось прочитать конфигурацию: {context.Exception.Message}");
        context.Ignore = true;
    };
});

builder.Services
    .AddOptions<KafkaOptions>()
    .BindConfiguration(KafkaOptions.SectionName);

builder.Services
    .AddOptions<GeneratorOptions>()
    .BindConfiguration(
        GeneratorOptions.SectionName,
        options => options.ErrorOnUnknownConfiguration = true);

builder.Services.AddSingleton<
    IValidator<KafkaOptions>,
    KafkaOptionsValidator>();

builder.Services.AddSingleton<IProducer<Null, string>>(provider =>
{
    var options = provider
        .GetRequiredService<IOptions<KafkaOptions>>()
        .Value;

    var validator = provider
        .GetRequiredService<IValidator<KafkaOptions>>();

    validator.ValidateAndThrow(options);

    var producerConfig = new ProducerConfig
    {
        BootstrapServers = options.BootstrapServers,
        MessageTimeoutMs = options.MessageTimeoutMs
    };

    return new ProducerBuilder<Null, string>(producerConfig).Build();
});

builder.Services.AddSingleton<KafkaEventPublisher>();

builder.Services.AddSingleton<
    IValidator<GenerationJob>,
    GenerationJobValidator>();

builder.Services.AddSingleton<
    IValidator<GeneratorOptions>,
    GeneratorOptionsValidator>();

builder.Services.AddSingleton<GenerationService>();

using var host = builder.Build();
using var cancellationTokenSource = new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

Console.CancelKeyPress += cancelHandler;

try
{
    var generationService = host.Services
        .GetRequiredService<GenerationService>();

    Console.WriteLine(
        "Генератор запущен. Для остановки нажмите Ctrl+C.");

    await generationService.RunAsync(
        cancellationTokenSource.Token);
}
catch (ValidationException exception)
{
    Console.Error.WriteLine("Ошибка конфигурации:");

    foreach (var error in exception.Errors)
    {
        Console.Error.WriteLine(
            $"{error.PropertyName}: {error.ErrorMessage}");
    }

    Environment.ExitCode = 1;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Ожидание отправки отменено");
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine(
        $"Ошибка настройки генератора: {exception.Message}");

    Environment.ExitCode = 1;
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
