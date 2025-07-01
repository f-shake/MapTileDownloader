using System.Collections.Generic;
using MapTileDownloader.Models;

namespace MapTileDownloader.UI.Messages
{
    public class GenerateTypoInlinesMessage(List<TypoSegment> segments)
    {
        public List<TypoSegment> Segments { get; } = segments;
    }
}
