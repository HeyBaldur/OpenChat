namespace OpenChat.Domain.Dtos;

public class ToolCallRecord
{
    public string Tool { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? SourceUrl { get; set; }
}
