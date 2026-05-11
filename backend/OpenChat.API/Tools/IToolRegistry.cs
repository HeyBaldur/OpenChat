namespace OpenChat.API.Tools;

public interface IToolRegistry
{
    IReadOnlyList<IToolDefinition> GetAll();
    IToolDefinition? GetByName(string name);
    object[] GetOllamaFormatDefinitions();
}
