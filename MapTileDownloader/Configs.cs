using System.Text.Json;
using MapTileDownloader.Models;

namespace MapTileDownloader;

public class Configs
{
    public const string CONFIG_FILE = "config.json";
    private static Configs instance;

    public List<TileSource> TileSources { get; set; } = new List<TileSource>
    {
        new TileSource
        {
            Name = "ESRI影像",
            Url = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
        }
    };

    public static Configs Instance
    {
        get
        {
            if (instance != null)
            {
                if (File.Exists(CONFIG_FILE))
                {
                    try
                    {
                        instance = JsonSerializer.Deserialize(File.ReadAllText(CONFIG_FILE),
                            MapTileDownloaderJsonContext.Config.Configs);
                    }
                    catch
                    {
                    }
                }
            }

            if (instance == null)
            {
                instance = new Configs();
            }

            return instance;
        }
    }
}