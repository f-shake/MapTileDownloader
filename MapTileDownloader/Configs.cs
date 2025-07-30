using System.Diagnostics;
using System.Text.Json;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;

namespace MapTileDownloader
{
    public class Configs
    {
        private const string CONFIG_FILE = "config.json";
        private static readonly Lazy<Configs> lazyInstance = new Lazy<Configs>(LoadOrCreateConfig);
        private static readonly object lockObj = new object();
        private static Timer savingTimer;
        static Configs()
        {
            return;
            savingTimer = new Timer(state =>
                {
                    try
                    {
                        lock (lockObj)
                        {
                            Instance.Save();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Assert(false);
                    }
                },
                null,
#if DEBUG
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10)
#else
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(10)
#endif
            );
        }

        public static Configs Instance => lazyInstance.Value;

        public string ConvertDir { get; set; }

        public string ConvertPattern { get; set; }

        public Coordinate[] Coordinates { get; set; }

        public int MaxDownloadConcurrency { get; set; }

        public int MaxLevel { get; set; }

        public string MbtilesFile { get; set; }

        public int MergeImageQuality { get; set; }

        public int MinLevel { get; set; }

        public int SelectedTileSourcesIndex { get; set; }

        public bool ServerLocalHostOnly { get; set; }

        public ushort ServerPort { get; set; }

        public bool ServerReturnEmptyPngWhenNotFound { get; set; }

        public List<TileDataSource> TileSources { get; set; }

        public bool MbtilesUseTms { get; set; }

        public void Save()
        {
            lock (lockObj)
            {
                try
                {
                    Debug.WriteLine(MbtilesFile);
                    var tempFile = Path.GetTempFileName();
                    var json = JsonSerializer.Serialize(this, MapTileDownloaderJsonContext.Config.Configs);
                    File.WriteAllText(tempFile, json);
                    if (File.Exists(CONFIG_FILE))
                    {
                        File.Replace(tempFile, CONFIG_FILE, null);
                    }
                    else
                    {
                        File.Move(tempFile, CONFIG_FILE);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false);
                    throw;
                }
            }
        }

        private static Configs CreateDefaultConfig()
        {
            return new Configs
            {
                TileSources =
                [
                    new TileDataSource
                    {
                        Name = "ESRI影像",
                        Url =
                            "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}",
                        Format = "JPG"
                    },
                    new TileDataSource
                    {
                        Name = "谷歌卫星",
                        Url = "https://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}",
                        Format = "JPG"
                    },
                    new TileDataSource
                    {
                        Name = "OpenStreetMap",
                        Url = "http://a.tile.openstreetmap.org/{z}/{x}/{y}.png",
                        Format = "PNG"
                    },
                ],
                ConvertPattern = "{z}/{x}/{y}.{ext}",
                MaxDownloadConcurrency = 10,
                MinLevel = 0,
                MaxLevel = 19,
                ServerPort = 8888,
                MergeImageQuality = 90,
                ServerLocalHostOnly = true,
                ServerReturnEmptyPngWhenNotFound = true
            };
        }

        private static Configs LoadOrCreateConfig()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    var json = File.ReadAllText(CONFIG_FILE);
                    return JsonSerializer.Deserialize(json, MapTileDownloaderJsonContext.Config.Configs)
                           ?? CreateDefaultConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);
            }

            return CreateDefaultConfig();
        }
    }
}