using OpenChat.Application.Models;

namespace OpenChat.Application.Interfaces.Services;

public interface IAgenticChatService
{
    IAsyncEnumerable<AgenticStreamEvent> StreamWithToolsAsync(
        List<OllamaChatMessage> messages,
        string model,
        string userId,
        CancellationToken ct);
}
