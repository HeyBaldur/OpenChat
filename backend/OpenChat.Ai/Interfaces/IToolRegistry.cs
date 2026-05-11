namespace OpenChat.Ai.Interfaces;

public interface IToolRegistry
{
    IReadOnlyList<IToolDefinition> GetAll();
    IToolDefinition? GetByName(string name);
    object[] GetOllamaFormatDefinitions();
}
