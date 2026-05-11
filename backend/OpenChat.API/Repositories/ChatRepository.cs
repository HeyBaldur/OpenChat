using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.API.Models;

namespace OpenChat.API.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly IMongoCollection<ChatMessage> _collection;

    public ChatRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<ChatMessage>("ChatMessages");

        var indexKeys = Builders<ChatMessage>.IndexKeys
            .Ascending(m => m.ConversationId)
            .Ascending(m => m.Timestamp);
        _collection.Indexes.CreateOne(new CreateIndexModel<ChatMessage>(indexKeys));
    }

    public async Task AddMessageAsync(ChatMessage message) =>
        await _collection.InsertOneAsync(message);

    public async Task<List<ChatMessage>> GetByConversationAsync(string conversationId, int limit = 30) =>
        await _collection
            .Find(m => m.ConversationId == conversationId)
            .SortByDescending(m => m.Timestamp)
            .Limit(limit)
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderBy(m => m.Timestamp).ToList());

    public async Task DeleteByConversationAsync(string conversationId) =>
        await _collection.DeleteManyAsync(m => m.ConversationId == conversationId);
}
