using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenChat.API.Models;

public class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = "New Chat";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("totalTokens")]
    public int TotalTokens { get; set; } = 0;

    [BsonElement("model")]
    public string? Model { get; set; }
}
