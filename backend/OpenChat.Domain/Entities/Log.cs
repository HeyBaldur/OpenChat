using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenChat.Domain.Entities;

public class Log
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("model")]
    public string Model { get; set; } = string.Empty;

    [BsonElement("promptTokens")]
    public int PromptTokens { get; set; }

    [BsonElement("completionTokens")]
    public int CompletionTokens { get; set; }

    [BsonElement("totalTokens")]
    public int TotalTokens { get; set; }

    [BsonElement("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
