using System.Text.Json;

namespace OpenChat.API.Models;

public enum AgenticEventType { Token, ToolStart, ToolEnd, Done, Error }

public class AgenticStreamEvent
{
    public AgenticEventType Type { get; set; }

    // Token event
    public string? TokenText { get; set; }

    // ToolStart event
    public string? ToolName { get; set; }
    public JsonElement? ToolArguments { get; set; }

    // ToolEnd event
    public bool? ToolSuccess { get; set; }
    public string? SourceUrl { get; set; }
    public string? ErrorReason { get; set; }
    public string? ContentPreview { get; set; }

    // Done event
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public List<ToolCallRecord>? ToolCallsUsed { get; set; }
    public string? ConversationId { get; set; }
    public string? ConversationTitle { get; set; }

    // Error event
    public string? ErrorMessage { get; set; }

    public static AgenticStreamEvent Token(string text) => new()
    {
        Type = AgenticEventType.Token,
        TokenText = text
    };

    public static AgenticStreamEvent ToolStart(string toolName, JsonElement args) => new()
    {
        Type = AgenticEventType.ToolStart,
        ToolName = toolName,
        ToolArguments = args
    };

    public static AgenticStreamEvent ToolEnd(
        string toolName,
        bool ok,
        string? sourceUrl,
        string? errorReason,
        string contentPreview) => new()
        {
            Type = AgenticEventType.ToolEnd,
            ToolName = toolName,
            ToolSuccess = ok,
            SourceUrl = sourceUrl,
            ErrorReason = errorReason,
            ContentPreview = contentPreview
        };

    public static AgenticStreamEvent Done(
        int promptTokens,
        int completionTokens,
        List<ToolCallRecord> toolCallsUsed,
        string? conversationId = null,
        string? conversationTitle = null) => new()
        {
            Type = AgenticEventType.Done,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            ToolCallsUsed = toolCallsUsed,
            ConversationId = conversationId,
            ConversationTitle = conversationTitle
        };

    public static AgenticStreamEvent Error(string message) => new()
    {
        Type = AgenticEventType.Error,
        ErrorMessage = message
    };
}
