namespace OpenChat.API.Tools;

public class ToolExecutionResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string ErrorReason { get; set; } = string.Empty;

    public static ToolExecutionResult Ok(string content, string sourceUrl) => new()
    {
        Success = true,
        Content = content,
        SourceUrl = sourceUrl
    };

    public static ToolExecutionResult Error(string reason, string message) => new()
    {
        Success = false,
        Content = message,
        ErrorReason = reason
    };
}
