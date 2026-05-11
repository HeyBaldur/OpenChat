using OpenChat.API.Models;

namespace OpenChat.API.Services;

public interface IAgenticChatService
{
    IAsyncEnumerable<AgenticStreamEvent> StreamWithToolsAsync(
        List<OllamaChatMessage> messages,
        string model,
        string userId,
        CancellationToken ct);
}
