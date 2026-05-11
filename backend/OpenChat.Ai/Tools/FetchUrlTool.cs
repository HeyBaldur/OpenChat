using System.Text.Json;
using OpenChat.Ai.Interfaces;
using OpenChat.Ai.Models;

namespace OpenChat.Ai.Tools;

public class FetchUrlTool : IToolDefinition
{
    private readonly IWebFetcherService _fetcher;

    public FetchUrlTool(IWebFetcherService fetcher)
    {
        _fetcher = fetcher;
    }

    public string Name => "fetch_url";

    public string Description =>
        "Fetches the content of a web page. Use this when the user asks about a topic where official documentation, tutorials, or specific reference information would help give an accurate answer. " +
        "Prefer specific documentation URLs (e.g. 'https://angular.dev/guide/signals', 'https://learn.microsoft.com/dotnet/csharp/'). " +
        "Only URLs from allowed domains can be fetched — if the fetch is blocked, try a different source or answer from general knowledge.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            url = new
            {
                type = "string",
                description = "Full URL to fetch, including https://. Must be from an allowed domain. " +
                              "Examples: 'https://angular.dev/guide/signals', " +
                              "'https://developer.mozilla.org/en-US/docs/Web/JavaScript/Closures', " +
                              "'https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/'."
            }
        },
        required = new[] { "url" }
    };

    public async Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, string userId, CancellationToken ct)
    {
        if (!arguments.TryGetProperty("url", out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
            return ToolExecutionResult.Error("invalid_arguments", "Missing required argument 'url'.");

        return await _fetcher.FetchAndExtractAsync(urlEl.GetString()!, userId, ct);
    }
}
