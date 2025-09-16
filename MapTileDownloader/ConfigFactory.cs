using FzLib.Text;
using MapTileDownloader.Models;
using Serilog;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace MapTileDownloader
{
    internal class ConfigFactory : IJsonFileSerializableFactory
    {
        public static ConfigFactory Instance { get; } = new ConfigFactory();

        public JsonSerializerContext Context { get; } = new MapTileDownloaderJsonContext(
            JsonSerializableExtensions.GetJsonSerializerOptions(
                converters: [new CoordinateConverter(), new CoordinateArrayConverter()]));

        public string FileName { get; } = "config.json";

        internal Lazy<Configs> LazyConfigs { get; private set; } = new Lazy<Configs>(() =>
                {
                    try
                    {
                        return Instance.LoadJsonFile(Instance.CreateDefaultConfig);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "加载配置文件失败");
                        Debug.Assert(false);
                        return Instance.CreateDefaultConfig();
                    }
                });

        internal Configs CreateDefaultConfig()
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

        internal void Save(Configs configs)
        {
            try
            {
                Instance.SaveJsonFile(configs);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "保存配置文件失败");
                Debug.Assert(false);
            }
        }
    }
}