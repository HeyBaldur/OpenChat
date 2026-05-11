using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using OpenChat.Application.Constants;
using OpenChat.Domain.Entities;
using OpenChat.Infrastructure.Mongo.Settings;

namespace OpenChat.Infrastructure.Migrations;

public class AllowlistMigrationV3 : IHostedService
{
    private readonly IMongoCollection<AllowedDomain> _domains;
    private readonly IMongoCollection<BsonDocument> _migrations;
    private readonly ILogger<AllowlistMigrationV3> _logger;

    public AllowlistMigrationV3(IOptions<MongoDbSettings> settings, ILogger<AllowlistMigrationV3> logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _domains = db.GetCollection<AllowedDomain>("AllowedDomains");
        _migrations = db.GetCollection<BsonDocument>("Migrations");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var alreadyRan = await _migrations.Find(
            Builders<BsonDocument>.Filter.Eq("name", "allowlist-v3")
        ).AnyAsync(ct);

        if (alreadyRan)
        {
            _logger.LogDebug("AllowlistMigrationV3: already applied, skipping");
            return;
        }

        _logger.LogInformation("AllowlistMigrationV3: adding new default domains for existing users");

        var userIds = await _domains
            .Distinct<string>("userId", Builders<AllowedDomain>.Filter.Ne(x => x.UserId, ""))
            .ToListAsync(ct);

        var added = 0;
        foreach (var userId in userIds)
        {
            var existingDomains = await _domains
                .Find(Builders<AllowedDomain>.Filter.Eq(x => x.UserId, userId))
                .Project(x => x.Domain)
                .ToListAsync(ct);

            var existingSet = existingDomains.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = DefaultAllowedDomains.SeedData
                .Where(s => !existingSet.Contains(s.Domain))
                .Select(s => new AllowedDomain
                {
                    UserId = userId,
                    Domain = s.Domain,
                    Enabled = true,
                    Category = s.Category,
                    Description = s.Description,
                    AllowSubdomains = s.AllowSubdomains,
                    AddedBy = "system",
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (missing.Count == 0) continue;

            try
            {
                await _domains.InsertManyAsync(missing, new InsertManyOptions { IsOrdered = false }, ct);
                added += missing.Count;
            }
            catch (MongoBulkWriteException ex) when (ex.WriteErrors.All(e => e.Code == 11000))
            {
                // Duplicate key — already seeded by concurrent request; not an error
            }
        }

        await _migrations.InsertOneAsync(new BsonDocument
        {
            ["name"] = "allowlist-v3",
            ["appliedAt"] = DateTime.UtcNow
        }, cancellationToken: ct);

        _logger.LogInformation("AllowlistMigrationV3: inserted {Count} new default domain entries across {Users} users",
            added, userIds.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
