using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using OpenChat.Infrastructure.Mongo.Settings;

namespace OpenChat.Infrastructure.Mongo.Repositories;

public class LogRepository : ILogRepository
{
    private readonly IMongoCollection<Log> _collection;

    public LogRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<Log>("Logs");
    }

    public async Task AddAsync(Log log) =>
        await _collection.InsertOneAsync(log);

    public async Task<List<Log>> GetByConversationAsync(string conversationId) =>
        await _collection.Find(l => l.ConversationId == conversationId)
            .SortByDescending(l => l.Timestamp)
            .ToListAsync();
}
