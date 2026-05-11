using OpenChat.Application.Models;
using OpenChat.Domain.Dtos;

namespace OpenChat.Application.Interfaces.Services;

public interface IChatService
{
    Task<ChatResponse> ProcessMessageAsync(ChatRequest request);
    IAsyncEnumerable<AgenticStreamEvent> StreamMessageAsync(ChatRequest request, CancellationToken ct);
}
