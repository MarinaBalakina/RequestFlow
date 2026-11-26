namespace UserRequestsKafkaGenerator.Models;

public sealed class GenerationJob
{
    public string JobId { get; set; } = string.Empty;
    
    public int UserId { get; set; }
    
    public string Endpoint { get; set; } = string.Empty;

    public int Rpm {get; set; }
}