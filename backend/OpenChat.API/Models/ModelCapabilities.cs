namespace OpenChat.API.Models;

public static class ModelCapabilities
{
    private static readonly Dictionary<string, bool> ByModelName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["qwen2.5"] = true,
        ["deepseek-r1"] = true,
        ["llama3.1"] = true,
    };

    private static readonly Dictionary<string, bool> ByFamily = new(StringComparer.OrdinalIgnoreCase)
    {
        ["qwen2"] = true,
        ["llama3.1"] = true,
        ["mistral"] = true,
        ["deepseek-r1"] = true,
        ["llama3"] = false,
        ["phi3"] = false,
        ["gemma2"] = false,
    };

    public static bool SupportsToolCalling(string modelName) =>
        SupportsToolCalling(modelName, string.Empty);

    public static bool SupportsToolCalling(string modelName, string family)
    {
        var baseName = modelName.Split(':')[0];
        if (ByModelName.TryGetValue(baseName, out var byName)) return byName;
        if (ByFamily.TryGetValue(family, out var byFamily)) return byFamily;
        return false;
    }
}
