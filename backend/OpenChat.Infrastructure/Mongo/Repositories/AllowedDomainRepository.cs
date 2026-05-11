using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using OpenChat.Infrastructure.Mongo.Settings;

namespace OpenChat.Infrastructure.Mongo.Repositories;

public class AllowedDomainRepository : IAllowedDomainRepository
{
    private readonly IMongoCollection<AllowedDomain> _collection;

    public AllowedDomainRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<AllowedDomain>("AllowedDomains");

        // Drop the old global unique index on Domain (if it still exists)
        try { _collection.Indexes.DropOne("domain_1"); }
        catch (MongoCommandException ex) when (ex.CodeName is "IndexNotFound" or "NamespaceNotFound") { }

        // Compound unique index: one domain per user
        _collection.Indexes.CreateOne(new CreateIndexModel<AllowedDomain>(
            Builders<AllowedDomain>.IndexKeys
                .Ascending(d => d.UserId)
                .Ascending(d => d.Domain),
            new CreateIndexOptions { Unique = true, Name = "userId_domain_unique" }
        ));

        // Non-unique index for fast per-user queries
        _collection.Indexes.CreateOne(new CreateIndexModel<AllowedDomain>(
            Builders<AllowedDomain>.IndexKeys.Ascending(d => d.UserId),
            new CreateIndexOptions { Name = "userId_idx" }
        ));
    }

    public async Task<List<AllowedDomain>> GetAllByUserAsync(string userId) =>
        await _collection.Find(d => d.UserId == userId).ToListAsync();

    public async Task<AllowedDomain?> GetByIdAsync(string id, string userId) =>
        await _collection.Find(d => d.Id == id && d.UserId == userId).FirstOrDefaultAsync();

    public async Task<AllowedDomain?> GetByDomainAsync(string userId, string domain) =>
        await _collection.Find(d => d.UserId == userId && d.Domain == domain).FirstOrDefaultAsync();

    public async Task<List<AllowedDomain>> GetEnabledByUserAsync(string userId) =>
        await _collection.Find(d => d.UserId == userId && d.Enabled).ToListAsync();

    public async Task<AllowedDomain> CreateAsync(AllowedDomain domain)
    {
        await _collection.InsertOneAsync(domain);
        return domain;
    }

    public async Task CreateManyAsync(IEnumerable<AllowedDomain> domains)
    {
        try
        {
            await _collection.InsertManyAsync(domains, new InsertManyOptions { IsOrdered = false });
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.All(e => e.Code == 11000))
        {
            // All failures were duplicate keys — safe during concurrent seeding
        }
    }

    public async Task<bool> UpdateAsync(string id, string userId, AllowedDomain domain)
    {
        var update = Builders<AllowedDomain>.Update
            .Set(d => d.Domain, domain.Domain)
            .Set(d => d.Enabled, domain.Enabled)
            .Set(d => d.Category, domain.Category)
            .Set(d => d.Description, domain.Description)
            .Set(d => d.AllowSubdomains, domain.AllowSubdomains)
            .Set(d => d.UpdatedAt, domain.UpdatedAt);

        var result = await _collection.UpdateOneAsync(
            d => d.Id == id && d.UserId == userId, update);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id, string userId)
    {
        var result = await _collection.DeleteOneAsync(d => d.Id == id && d.UserId == userId);
        return result.DeletedCount > 0;
    }
}
