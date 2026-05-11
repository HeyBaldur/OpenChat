using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using OpenChat.Infrastructure.Mongo.Settings;

namespace OpenChat.Infrastructure.Mongo.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _collection;

    public UserRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<User>("Users");

        _collection.Indexes.CreateOne(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task<User?> FindByEmailAsync(string email) =>
        await _collection.Find(u => u.Email == email.ToLowerInvariant()).FirstOrDefaultAsync();

    public async Task CreateAsync(User user) =>
        await _collection.InsertOneAsync(user);
}
