using Microsoft.Extensions.Caching.Memory;
using OpenChat.API.Models;
using System.Text.RegularExpressions;

namespace OpenChat.API.Services;

public class ModelCatalogService : IModelCatalogService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private const string CacheKey = "ollama_models";

    public ModelCatalogService(HttpClient httpClient, IMemoryCache cache, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(config["Ollama:BaseUrl"] ?? "http://localhost:11434");
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _cache = cache;
    }

    public async Task<List<ModelDto>> GetModelsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out List<ModelDto>? cached) && cached is not null)
            return cached;

        var response = await _httpClient.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", ct);
        var models = (response?.Models ?? [])
            .Select(m => new ModelDto
            {
                Name = m.Name,
                DisplayName = BuildDisplayName(m.Name),
                SizeBytes = m.Size,
                SizeFormatted = FormatSize(m.Size),
                Family = m.Details.Family,
                ParameterSize = m.Details.ParameterSize,
                SupportsToolCalling = ModelCapabilities.SupportsToolCalling(m.Name, m.Details.Family)
            })
            .ToList();

        _cache.Set(CacheKey, models, TimeSpan.FromSeconds(60));
        return models;
    }

    private static string BuildDisplayName(string name)
    {
        var parts = name.Split(':');
        var modelPart = parts[0];
        var tag = parts.Length > 1 ? parts[1] : string.Empty;

        var tokens = Regex.Split(modelPart, @"([.\-_])")
            .Where(t => !string.IsNullOrEmpty(t) && t is not "." and not "-" and not "_")
            .Select(t => char.ToUpper(t[0]) + t[1..]);

        var display = string.Join(" ", tokens);

        if (!string.IsNullOrEmpty(tag) && !tag.Equals("latest", StringComparison.OrdinalIgnoreCase))
            display += $" {tag.ToUpper()}";

        return display;
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_000_000_000 => $"{bytes / 1_000_000_000.0:F1} GB",
        >= 1_000_000 => $"{bytes / 1_000_000.0:F0} MB",
        _ => $"{bytes / 1_000.0:F0} KB"
    };
}
