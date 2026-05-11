namespace OpenChat.API.Tools;

public class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<IToolDefinition> _tools;

    public ToolRegistry(IEnumerable<IToolDefinition> tools)
    {
        _tools = tools.ToList().AsReadOnly();
    }

    public IReadOnlyList<IToolDefinition> GetAll() => _tools;

    public IToolDefinition? GetByName(string name) =>
        _tools.FirstOrDefault(t => t.Name == name);

    public object[] GetOllamaFormatDefinitions() =>
        _tools.Select(t => (object)new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = t.JsonSchema
            }
        }).ToArray();
}
