using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenChat.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
