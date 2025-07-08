using BruTile;
using System.Collections.Generic;
using MapTileDownloader.Models;
using MapTileDownloader.Services;

namespace MapTileDownloader.UI.Messages;

public class GetMapServiceMessage
{
    public IMapService MapService { get; set; }
}