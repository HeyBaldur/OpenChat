namespace OpenChat.Domain.Dtos;

public class ChatRequest
{
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
}
