using System.Text.Json;

namespace OpenChat.Ai.Models;

public class ToolExecuteRequest
{
    public string Tool { get; set; } = string.Empty;
    public JsonElement Arguments { get; set; }
}
