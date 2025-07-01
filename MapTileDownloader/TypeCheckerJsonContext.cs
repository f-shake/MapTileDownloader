using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MapTileDownloader.Models;

[JsonSerializable(typeof(OllamaRequestData))]
[JsonSerializable(typeof(OllamaResponseData))]
[JsonSourceGenerationOptions(WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class MapTileDownloaderJsonContext : JsonSerializerContext
{
    private static MapTileDownloaderJsonContext config;

    private static MapTileDownloaderJsonContext web;

    static MapTileDownloaderJsonContext()
    {
        web= new MapTileDownloaderJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }); 
        config= new MapTileDownloaderJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        });
    }
    public static MapTileDownloaderJsonContext Config => config;
    public static MapTileDownloaderJsonContext Web => web;
}
