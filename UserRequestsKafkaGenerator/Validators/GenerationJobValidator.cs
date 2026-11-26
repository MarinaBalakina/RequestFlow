using FluentValidation;
using UserRequestsKafkaGenerator.Models;

namespace UserRequestsKafkaGenerator.Validators;

public class GenerationJobValidator : AbstractValidator<GenerationJob>
{
    public GenerationJobValidator()
    {
        RuleFor(job => job.JobId).NotEmpty().WithMessage("ожидается JobId");
        
        RuleFor(job => job.Endpoint).NotEmpty().WithMessage("Endpoint не должен быть пустым");

        RuleFor(job => job.Rpm).GreaterThan(0).WithMessage("RPM должен быть больше 0");
        
        RuleFor(job => job.UserId).GreaterThan(0);
    }
}