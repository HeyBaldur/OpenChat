using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenChat.API.Models;

// ── /api/chat request ────────────────────────────────────────
public class OllamaChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<OllamaChatMessage> Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    [JsonPropertyName("tools"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object[]? Tools { get; set; }
}

public class OllamaChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("tool_calls"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<OllamaToolCallEntry>? ToolCalls { get; set; }
}

// ── Tool call structures (request + response) ────────────────
public class OllamaToolCallEntry
{
    [JsonPropertyName("function")]
    public OllamaToolCallFunction? Function { get; set; }
}

public class OllamaToolCallFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public JsonElement Arguments { get; set; }
}

// ── Internal parsed tool call (used by OllamaService / AgenticChatService) ──
public class OllamaToolCall
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}

// ── Return type of OllamaService.ChatWithToolsAsync ──────────
public class OllamaToolChatResponse
{
    public bool HasToolCalls { get; set; }
    public List<OllamaToolCall> ToolCalls { get; set; } = [];
    public string Content { get; set; } = string.Empty;
    public OllamaChatMessage AssistantMessage { get; set; } = new() { Role = "assistant" };
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
}

// ── /api/chat response (non-stream) ─────────────────────────
public class OllamaChatResponse
{
    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvalCount { get; set; }
}

// ── /api/chat stream chunk ───────────────────────────────────
public class OllamaChatChunk
{
    [JsonPropertyName("message")]
    public OllamaChatMessage? Message { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("prompt_eval_count")]
    public int PromptEvalCount { get; set; }

    [JsonPropertyName("eval_count")]
    public int EvalCount { get; set; }
}

// ── Internal result passed to ChatService ───────────────────
public class OllamaResult
{
    public string Response { get; set; } = string.Empty;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens => PromptTokens + CompletionTokens;
}
