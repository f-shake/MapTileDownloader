using System.Diagnostics;
using System.Text.Json;
using FzLib.Text;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;
using Serilog;

namespace MapTileDownloader
{
    public class Configs
    {
        public static Configs Instance => ConfigFactory.Instance.LazyConfigs.Value;

        public string ConvertDir { get; set; }

        public string ConvertPattern { get; set; }

        public Coordinate[] Coordinates { get; set; }

        public int MaxDownloadConcurrency { get; set; }

        public int MaxLevel { get; set; }

        public string MbtilesFile { get; set; }

        public bool MbtilesUseTms { get; set; }

        public int MergeImageQuality { get; set; }

        public int MinLevel { get; set; }

        public int SelectedTileSourcesIndex { get; set; }

        public bool ServerLocalHostOnly { get; set; }

        public ushort ServerPort { get; set; }

        public bool ServerReturnEmptyPngWhenNotFound { get; set; }

        public List<TileDataSource> TileSources { get; set; }

        public void Save() => ConfigFactory.Instance.Save(this);
    }
}