using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.API.Models;

namespace OpenChat.API.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly IMongoCollection<Conversation> _collection;

    public ConversationRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Conversation>("Conversations");

        var index = Builders<Conversation>.IndexKeys
            .Ascending(c => c.UserId)
            .Descending(c => c.UpdatedAt);
        _collection.Indexes.CreateOne(new CreateIndexModel<Conversation>(index));
    }

    public async Task<Conversation> CreateAsync(string userId, string title, string? model = null)
    {
        var conversation = new Conversation
        {
            UserId = userId,
            Title = title,
            Model = model,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _collection.InsertOneAsync(conversation);
        return conversation;
    }

    public async Task<List<Conversation>> GetByUserAsync(string userId) =>
        await _collection.Find(c => c.UserId == userId)
            .SortByDescending(c => c.UpdatedAt)
            .ToListAsync();

    public async Task<Conversation?> GetByIdAsync(string conversationId) =>
        await _collection.Find(c => c.Id == conversationId).FirstOrDefaultAsync();

    public async Task UpdateTitleAsync(string conversationId, string title)
    {
        var update = Builders<Conversation>.Update
            .Set(c => c.Title, title)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(c => c.Id == conversationId, update);
    }

    public async Task AddTokensAsync(string conversationId, int tokens)
    {
        var update = Builders<Conversation>.Update
            .Inc(c => c.TotalTokens, tokens)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(c => c.Id == conversationId, update);
    }

    public async Task UpdateModelAsync(string conversationId, string model)
    {
        var update = Builders<Conversation>.Update
            .Set(c => c.Model, model)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await _collection.UpdateOneAsync(c => c.Id == conversationId, update);
    }

    public async Task DeleteAsync(string conversationId) =>
        await _collection.DeleteOneAsync(c => c.Id == conversationId);
}
