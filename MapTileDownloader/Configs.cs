using System.Text.Json;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;

namespace MapTileDownloader;

public class Configs
{
    public const string CONFIG_FILE = "config.json";
    private static Configs instance;

    public static Configs Instance
    {
        get
        {
            if (instance == null)
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

    public List<TileDataSource> TileSources { get; set; } = new List<TileDataSource>
    {
        new TileDataSource
        {
            Name = "ESRI影像",
            Url = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
        }
    };

    public Coordinate[] DownloadArea { get; set; }
    public int MinLevel { get; set; } = 0;
    public int MaxLevel { get; set; } = 20;
    public string DownloadDir { get; set; } 

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, MapTileDownloaderJsonContext.Config.Configs);
        File.WriteAllText(CONFIG_FILE, json);
    }
}

