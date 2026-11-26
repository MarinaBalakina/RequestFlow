using FluentValidation;
using UserRequestsKafkaGenerator.Configuration;

namespace UserRequestsKafkaGenerator.Validators;

public sealed class KafkaOptionsValidator : AbstractValidator<KafkaOptions>
{
    public KafkaOptionsValidator()
    {
        RuleFor(options => options.BootstrapServers)
            .NotEmpty()
            .WithMessage("Адрес Kafka BootstrapServers должен быть задан.");

        RuleFor(options => options.Topic)
            .NotEmpty()
            .WithMessage("Название топика Topic должно быть задано.");

        RuleFor(options => options.MessageTimeoutMs)
            .GreaterThan(0)
            .WithMessage("Таймаут отправки MessageTimeoutMs должен быть больше нуля.");
    }
}