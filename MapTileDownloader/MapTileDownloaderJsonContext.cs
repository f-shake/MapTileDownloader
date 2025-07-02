using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MapTileDownloader.Models;

namespace MapTileDownloader;

[JsonSerializable(typeof(Configs))]
[JsonSerializable(typeof(List<TileSource>))]
[JsonSerializable(typeof(TileSource))]
[JsonSourceGenerationOptions(WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class MapTileDownloaderJsonContext : JsonSerializerContext
{
    static MapTileDownloaderJsonContext()
    {
        Config= new MapTileDownloaderJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        });
    }
    public static MapTileDownloaderJsonContext Config { get; }
}