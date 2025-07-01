using System.Text.Json.Serialization;

namespace MapTileDownloader.Models;

public class OllamaResponseData
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
