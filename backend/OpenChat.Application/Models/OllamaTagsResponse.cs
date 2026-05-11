using System.Text.Json.Serialization;

namespace OpenChat.Application.Models;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")]
    public List<OllamaModelInfo> Models { get; set; } = [];
}

public class OllamaModelInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("details")]
    public OllamaModelDetails Details { get; set; } = new();
}

public class OllamaModelDetails
{
    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    [JsonPropertyName("parameter_size")]
    public string ParameterSize { get; set; } = string.Empty;
}
