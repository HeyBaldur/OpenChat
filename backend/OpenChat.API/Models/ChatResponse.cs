namespace OpenChat.API.Models;

public class ChatResponse
{
    public string ConversationId { get; set; } = string.Empty;
    public string ConversationTitle { get; set; } = string.Empty;
    public string Reply { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int TokensUsed { get; set; }
}
