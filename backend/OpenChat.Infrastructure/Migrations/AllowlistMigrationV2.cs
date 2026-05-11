using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenChat.Domain.Entities;
using OpenChat.Infrastructure.Mongo.Settings;

namespace OpenChat.Infrastructure.Migrations;

public class AllowlistMigrationV2 : IHostedService
{
    private readonly IMongoCollection<AllowedDomain> _collection;
    private readonly ILogger<AllowlistMigrationV2> _logger;

    public AllowlistMigrationV2(IOptions<MongoDbSettings> settings, ILogger<AllowlistMigrationV2> logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _collection = db.GetCollection<AllowedDomain>("AllowedDomains");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var legacyFilter = Builders<AllowedDomain>.Filter.Or(
            Builders<AllowedDomain>.Filter.Exists(x => x.UserId, false),
            Builders<AllowedDomain>.Filter.Eq(x => x.UserId, null!),
            Builders<AllowedDomain>.Filter.Eq(x => x.UserId, "")
        );

        var legacyCount = await _collection.CountDocumentsAsync(legacyFilter, cancellationToken: cancellationToken);

        if (legacyCount == 0) return;

        _logger.LogInformation("AllowlistMigrationV2: found {Count} legacy documents without UserId — removing", legacyCount);

        await _collection.DeleteManyAsync(legacyFilter, cancellationToken);

        _logger.LogInformation("AllowlistMigrationV2: removed legacy documents. Each user will receive their seed on first GET /api/allowlist");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
