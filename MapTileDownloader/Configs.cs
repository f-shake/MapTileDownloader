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

    public Coordinate[] Coordinates { get; set; }

    public int MaxDownloadConcurrency { get; set; } = 10;
    public int MaxLevel { get; set; } = 20;
    public string MbtilesFile { get; set; }

    public int MergeJpgQuality { get; set; } = 90;

    public int MinLevel { get; set; } = 0;

    public int SelectedTileSourcesIndex { get; set; } = 0;

    public bool ServerLocalHostOnly { get; set; } = true;

    public ushort ServerPort { get; set; } = 8888;

    public bool ServerReturnEmptyPngWhenNotFound { get; set; } = true;


    public List<TileDataSource> TileSources { get; set; } = new List<TileDataSource>
    {
        new TileDataSource
        {
            Name = "ESRI影像",
            Url = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
        }
    };

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, MapTileDownloaderJsonContext.Config.Configs);
        File.WriteAllText(CONFIG_FILE, json);
    }
}

