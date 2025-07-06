using BruTile;
using System.Collections.Generic;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Mapping;

namespace MapTileDownloader.UI.Messages;

public class GetMapServiceMessage
{
    public IMapService MapService { get; set; }
}