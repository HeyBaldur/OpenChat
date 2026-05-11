using OpenChat.Ai.Models;

namespace OpenChat.Ai.Interfaces;

public interface IWebFetcherService
{
    Task<ToolExecutionResult> FetchAndExtractAsync(string url, string userId, CancellationToken ct);
}
