using Microsoft.Extensions.Caching.Memory;
using OpenChat.API.Constants;
using OpenChat.API.Models;
using OpenChat.API.Repositories;
using System.Text.RegularExpressions;

namespace OpenChat.API.Services;

public partial class AllowlistService : IAllowlistService
{
    private readonly IAllowedDomainRepository _repository;
    private readonly IMemoryCache _cache;

    private static readonly string[] ReservedDomains =
        ["localhost", "local", "internal", "localdomain"];

    private static readonly Regex PrivateIpPattern = PrivateIp();

    [GeneratedRegex(@"^(10\.|172\.(1[6-9]|2\d|3[01])\.|192\.168\.|127\.|0\.0\.0\.0)", RegexOptions.Compiled)]
    private static partial Regex PrivateIp();

    [GeneratedRegex(@"^[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?(\.[a-z0-9]([a-z0-9\-]{0,61}[a-z0-9])?)+$", RegexOptions.Compiled)]
    private static partial Regex ValidDomain();

    public AllowlistService(IAllowedDomainRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<List<AllowedDomainResponse>> GetAllForUserAsync(string userId)
    {
        var domains = await _repository.GetAllByUserAsync(userId);

        if (domains.Count == 0)
        {
            await SeedDefaultsForUserAsync(userId);
            domains = await _repository.GetAllByUserAsync(userId);
        }

        return domains.Select(AllowedDomainResponse.From).ToList();
    }

    public async Task<AllowedDomainResponse?> GetByIdAsync(string id, string userId)
    {
        var domain = await _repository.GetByIdAsync(id, userId);
        return domain is null ? null : AllowedDomainResponse.From(domain);
    }

    public async Task<AllowedDomainResponse> CreateAsync(AllowedDomainRequest request, string userId)
    {
        var normalized = NormalizeAndValidate(request.Domain);

        var existing = await _repository.GetByDomainAsync(userId, normalized);
        if (existing is not null)
            throw new InvalidOperationException("DUPLICATE");

        var now = DateTime.UtcNow;
        var entity = new AllowedDomain
        {
            UserId = userId,
            Domain = normalized,
            Enabled = request.Enabled,
            Category = request.Category,
            Description = request.Description,
            AllowSubdomains = request.AllowSubdomains,
            AddedBy = userId,
            AddedAt = now,
            UpdatedAt = now
        };

        await _repository.CreateAsync(entity);
        InvalidateCache(userId);
        return AllowedDomainResponse.From(entity);
    }

    public async Task<AllowedDomainResponse> UpdateAsync(string id, AllowedDomainRequest request, string userId)
    {
        var existing = await _repository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException();

        var normalized = NormalizeAndValidate(request.Domain);

        var conflict = await _repository.GetByDomainAsync(userId, normalized);
        if (conflict is not null && conflict.Id != id)
            throw new InvalidOperationException("DUPLICATE");

        existing.Domain = normalized;
        existing.Enabled = request.Enabled;
        existing.Category = request.Category;
        existing.Description = request.Description;
        existing.AllowSubdomains = request.AllowSubdomains;
        existing.UpdatedAt = DateTime.UtcNow;

        var matched = await _repository.UpdateAsync(id, userId, existing);
        if (!matched) throw new KeyNotFoundException();

        InvalidateCache(userId);
        return AllowedDomainResponse.From(existing);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var existing = await _repository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException();

        if (existing.AddedBy == "system")
            throw new UnauthorizedAccessException("SYSTEM_DEFAULT");

        var deleted = await _repository.DeleteAsync(id, userId);
        if (!deleted) throw new KeyNotFoundException();

        InvalidateCache(userId);
    }

    public async Task<AllowedDomainResponse> ToggleAsync(string id, string userId)
    {
        var existing = await _repository.GetByIdAsync(id, userId)
            ?? throw new KeyNotFoundException();

        existing.Enabled = !existing.Enabled;
        existing.UpdatedAt = DateTime.UtcNow;

        var matched = await _repository.UpdateAsync(id, userId, existing);
        if (!matched) throw new KeyNotFoundException();

        InvalidateCache(userId);
        return AllowedDomainResponse.From(existing);
    }

    public async Task<bool> IsDomainAllowedAsync(string urlOrDomain, string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        var canonical = ParseCanonicalDomain(urlOrDomain);
        if (canonical is null) return false;

        var enabled = await GetEnabledCachedAsync(userId);

        foreach (var entry in enabled)
        {
            if (entry.Domain == canonical) return true;

            if (entry.AllowSubdomains && canonical.EndsWith("." + entry.Domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task SeedDefaultsForUserAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var docs = DefaultAllowedDomains.SeedData.Select(s => new AllowedDomain
        {
            UserId = userId,
            Domain = s.Domain,
            Enabled = true,
            Category = s.Category,
            Description = s.Description,
            AllowSubdomains = s.AllowSubdomains,
            AddedBy = "system",
            AddedAt = now,
            UpdatedAt = now
        });

        await _repository.CreateManyAsync(docs);
    }

    private async Task<List<AllowedDomain>> GetEnabledCachedAsync(string userId)
    {
        var key = $"allowlist:enabled:{userId}";
        if (_cache.TryGetValue(key, out List<AllowedDomain>? cached) && cached is not null)
            return cached;

        var list = await _repository.GetEnabledByUserAsync(userId);
        _cache.Set(key, list);
        return list;
    }

    private void InvalidateCache(string userId) =>
        _cache.Remove($"allowlist:enabled:{userId}");

    private static string NormalizeAndValidate(string input)
    {
        var value = input.Trim().ToLowerInvariant();

        if (value.StartsWith("https://")) value = value[8..];
        else if (value.StartsWith("http://")) value = value[7..];

        var slashIndex = value.IndexOf('/');
        if (slashIndex >= 0) value = value[..slashIndex];

        var atIndex = value.IndexOf('@');
        if (atIndex >= 0) value = value[(atIndex + 1)..];

        var portIndex = value.LastIndexOf(':');
        if (portIndex >= 0) value = value[..portIndex];

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("INVALID_DOMAIN");

        if (PrivateIpPattern.IsMatch(value))
            throw new ArgumentException("RESERVED_DOMAIN");

        foreach (var reserved in ReservedDomains)
        {
            if (value == reserved || value.EndsWith("." + reserved))
                throw new ArgumentException("RESERVED_DOMAIN");
        }

        if (!ValidDomain().IsMatch(value))
            throw new ArgumentException("INVALID_DOMAIN");

        return value;
    }

    private static string? ParseCanonicalDomain(string urlOrDomain)
    {
        try
        {
            if (!urlOrDomain.Contains("://"))
                urlOrDomain = "https://" + urlOrDomain;

            var uri = new Uri(urlOrDomain);
            return uri.Host.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }
}
