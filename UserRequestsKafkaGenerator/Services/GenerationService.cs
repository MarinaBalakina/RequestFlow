using System.Diagnostics;
using Confluent.Kafka;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserRequestsKafkaGenerator.Configuration;
using UserRequestsKafkaGenerator.Models;

namespace UserRequestsKafkaGenerator.Services;

public sealed class GenerationService
{
    private readonly KafkaEventPublisher _publisher;
    private readonly IOptionsFactory<GeneratorOptions> _optionsFactory;
    private readonly IValidator<GeneratorOptions> _validator;
    private readonly ILogger<GenerationService> _logger;

    private readonly Dictionary<string, RunningJob> _runningJobs =
        new(StringComparer.Ordinal);

    public GenerationService(
        KafkaEventPublisher publisher,
        IOptionsFactory<GeneratorOptions> optionsFactory,
        IValidator<GeneratorOptions> validator,
        ILogger<GenerationService> logger)
    {
        _publisher = publisher;
        _optionsFactory = optionsFactory;
        _validator = validator;
        _logger = logger;
    }

    private sealed record RunningJob(
        GenerationJob Job,
        CancellationTokenSource Cancellation,
        Task Task);

    public async Task RunAsync(
        CancellationToken cancellationToken)
    {
        var currentOptions = ReadAndValidateOptions();

        using var timer = new PeriodicTimer(
            TimeSpan.FromMilliseconds(
                currentOptions.RefreshIntervalMs));

        try
        {
            await ApplyConfigurationAsync(
                currentOptions.Jobs,
                cancellationToken);

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    currentOptions = ReadAndValidateOptions();
                    timer.Period = TimeSpan.FromMilliseconds(
                        currentOptions.RefreshIntervalMs);
                }
                catch (Exception exception) when (
                    exception is ValidationException or InvalidOperationException)
                {
                    _logger.LogWarning(
                        "Новые настройки не применены: {Message}",
                        exception.Message);
                }

                await ApplyConfigurationAsync(
                    currentOptions.Jobs,
                    cancellationToken);
            }
        }
        finally
        {
            await StopAllAsync(CancellationToken.None);
        }
    }

    private GeneratorOptions ReadAndValidateOptions()
    {
        var options = _optionsFactory.Create(Options.DefaultName);
        _validator.ValidateAndThrow(options);
        return options;
    }

    private async Task ApplyConfigurationAsync(
        GenerationJob[] jobs,
        CancellationToken cancellationToken)
    {
        var desiredJobs = jobs.ToDictionary(
            job => job.JobId,
            StringComparer.Ordinal);

        foreach (var pair in _runningJobs.ToArray())
        {
            var jobId = pair.Key;
            var runningJob = pair.Value;

            var mustStop =
                runningJob.Task.IsCompleted
                || !desiredJobs.TryGetValue(jobId, out var desiredJob)
                || !HasSameSettings(runningJob.Job, desiredJob);

            if (!mustStop)
            {
                continue;
            }

            _runningJobs.Remove(jobId);

            await StopJobAsync(
                runningJob,
                CancellationToken.None);

            _logger.LogInformation(
                "Задание {JobId} остановлено.",
                jobId);
        }

        foreach (var job in desiredJobs.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_runningJobs.ContainsKey(job.JobId))
            {
                continue;
            }

            var jobCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            var task = RunJobAsync(
                job,
                jobCancellation.Token);

            _runningJobs.Add(
                job.JobId,
                new RunningJob(
                    job,
                    jobCancellation,
                    task));

            _logger.LogInformation(
                "Задание {JobId} запущено: " +
                "User={UserId}, Endpoint={Endpoint}, RPM={Rpm}",
                job.JobId,
                job.UserId,
                job.Endpoint,
                job.Rpm);
        }
    }

    private static bool HasSameSettings(
        GenerationJob current,
        GenerationJob updated)
    {
        return current.UserId == updated.UserId
            && current.Endpoint == updated.Endpoint
            && current.Rpm == updated.Rpm;
    }

    private async Task StopJobAsync(
        RunningJob runningJob,
        CancellationToken cancellationToken)
    {
        runningJob.Cancellation.Cancel();

        try
        {
            await runningJob.Task.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
            when (
                runningJob.Cancellation.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (runningJob.Task.IsFaulted)
        {
            _logger.LogError(
                exception,
                "Задание {JobId} аварийно завершилось.",
                runningJob.Job.JobId);
        }
        finally
        {
            runningJob.Cancellation.Dispose();
        }
    }

    private async Task StopAllAsync(
        CancellationToken cancellationToken)
    {
        var stopTasks = _runningJobs.Values
            .Select(job => StopJobAsync(job, cancellationToken))
            .ToArray();

        try
        {
            await Task.WhenAll(stopTasks);
        }
        finally
        {
            _runningJobs.Clear();
        }
    }

    private async Task RunJobAsync(
        GenerationJob job,
        CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromMinutes(1.0 / job.Rpm);

        var stopwatch = Stopwatch.StartNew();
        var nextSendAt = TimeSpan.Zero;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var requestEvent = new UserRequestEvent(
                user_id: job.UserId,
                endpoint: job.Endpoint);

            try
            {
                await _publisher.PublishAsync(
                    requestEvent,
                    cancellationToken);

                _logger.LogInformation(
                    "{Time:HH:mm:ss.fff} | {JobId} | " +
                    "User={UserId}, Endpoint={Endpoint}, RPM={Rpm}",
                    DateTimeOffset.Now,
                    job.JobId,
                    job.UserId,
                    job.Endpoint,
                    job.Rpm);
            }
            catch (ProduceException<Null, string> exception)
            {
                _logger.LogError(
                    exception,
                    "Ошибка отправки задания {JobId}: {Reason}",
                    job.JobId,
                    exception.Error.Reason);
            }

            nextSendAt += interval;

            var elapsed = stopwatch.Elapsed;

            if (nextSendAt <= elapsed)
            {
                nextSendAt = elapsed + interval;
            }

            await Task.Delay(
                nextSendAt - elapsed,
                cancellationToken);
        }
    }
}
