using System.Text.Json;

namespace OpenChat.API.Tools;

public interface IToolDefinition
{
    string Name { get; }
    string Description { get; }
    object JsonSchema { get; }
    Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, string userId, CancellationToken ct);
}
