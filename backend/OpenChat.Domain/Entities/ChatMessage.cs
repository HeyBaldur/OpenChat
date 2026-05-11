using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using OpenChat.Domain.Dtos;

namespace OpenChat.Domain.Entities;

public class ChatMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = string.Empty;

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [BsonElement("stopped")]
    public bool Stopped { get; set; }

    [BsonElement("toolCallsUsed")]
    public List<ToolCallRecord>? ToolCallsUsed { get; set; }
}
