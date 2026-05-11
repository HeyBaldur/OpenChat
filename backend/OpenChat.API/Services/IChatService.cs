using OpenChat.API.Models;

namespace OpenChat.API.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
    IAsyncEnumerable<AgenticStreamEvent> StreamMessageAsync(ChatRequest request, CancellationToken ct);
}
