using System.Text.Json;
using OpenChat.Ai.Models;

namespace OpenChat.Ai.Interfaces;

public interface IToolDefinition
{
    string Name { get; }
    string Description { get; }
    object JsonSchema { get; }
    Task<ToolExecutionResult> ExecuteAsync(JsonElement arguments, string userId, CancellationToken ct);
}
