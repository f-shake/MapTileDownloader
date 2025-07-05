using System.Collections.Generic;
using BruTile;

namespace MapTileDownloader.UI.Messages;

public class DisplayTilesOnMapMessage(IList<TileIndex> tiles)
{
    public IList<TileIndex> Tiles { get; } = tiles;
}