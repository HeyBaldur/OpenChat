using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OpenChat.API.Models;

public class AllowedDomain
{
    [BsonId, BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = default!;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("domain")]
    public string Domain { get; set; } = default!;

    [BsonElement("enabled")]
    public bool Enabled { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = default!;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("allowSubdomains")]
    public bool AllowSubdomains { get; set; }

    [BsonElement("addedBy")]
    public string AddedBy { get; set; } = default!;

    [BsonElement("addedAt")]
    public DateTime AddedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
