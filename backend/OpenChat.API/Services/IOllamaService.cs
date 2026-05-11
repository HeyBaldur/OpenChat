using OpenChat.API.Models;

namespace OpenChat.API.Services;

public interface IOllamaService
{
    Task<OllamaResult> GenerateResponseAsync(string userMessage, List<ChatMessage> history, string? modelOverride = null, CancellationToken ct = default);
    IAsyncEnumerable<OllamaChatChunk> StreamResponseAsync(string userMessage, List<ChatMessage> history, string? modelOverride = null, CancellationToken ct = default);

    Task<OllamaToolChatResponse> ChatWithToolsAsync(List<OllamaChatMessage> messages, string model, object[] tools, CancellationToken ct = default);
    IAsyncEnumerable<OllamaChatChunk> StreamChatAsync(List<OllamaChatMessage> messages, string model, CancellationToken ct = default);
    List<OllamaChatMessage> BuildConversationMessages(string userMessage, List<ChatMessage> history, bool withToolGuidance = false);
}
