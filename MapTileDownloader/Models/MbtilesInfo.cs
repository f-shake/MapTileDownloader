using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapTileDownloader.Models
{
    public class MbtilesInfo
    {
        public string Format { get; set; }
        public double MaxLatitude { get; set; }
        public double MaxLongitude { get; set; }
        public int MaxZoom { get; set; }
        public double MinLatitude { get; set; }
        public double MinLongitude { get; set; }
        public int MinZoom { get; set; }
        public string Path { get; set; }
        public int TileSize { get; set; }
        public int TileCount { get; set; }
        public List<TileLevelInfo> TileLevels { get; set; }
        public long  TotalLength { get; set; }

        public class TileLevelInfo
        {
            public int Count { get; set; }
            public int Level { get; set; }
            public long TotalLength { get; set; }
            public double Area { get; set; }
        }
    }
}
