using FluentValidation;
using UserRequestsKafkaGenerator.Configuration;
using UserRequestsKafkaGenerator.Models;

namespace UserRequestsKafkaGenerator.Validators;

public sealed class GeneratorOptionsValidator
    : AbstractValidator<GeneratorOptions>
{
    public GeneratorOptionsValidator(
        IValidator<GenerationJob> jobValidator)
    {
        RuleFor(options => options.RefreshIntervalMs)
            .GreaterThan(0)
            .WithMessage(
                "Период проверки конфигурации должен быть больше нуля.");

        RuleFor(options => options.Jobs)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("Массив Jobs должен быть задан.")
            .Must(jobs =>
                jobs.Select(job => job.JobId)
                    .Distinct(StringComparer.Ordinal)
                    .Count() == jobs.Length)
            .WithMessage("JobId заданий не должны повторяться.");

        RuleForEach(options => options.Jobs)
            .SetValidator(jobValidator);
    }
}