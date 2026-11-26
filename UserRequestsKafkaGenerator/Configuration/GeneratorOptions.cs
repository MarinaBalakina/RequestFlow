using UserRequestsKafkaGenerator.Models;

namespace UserRequestsKafkaGenerator.Configuration;

public sealed class GeneratorOptions
{
    public const string SectionName = "Generator";
    
    public int RefreshIntervalMs { get; set; }

    public GenerationJob[] Jobs { get; set; } = [];
}