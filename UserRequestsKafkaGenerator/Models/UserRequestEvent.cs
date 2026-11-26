namespace UserRequestsKafkaGenerator.Models;

public sealed record UserRequestEvent(
    int user_id, 
    string endpoint);