using BruTile;
using System.Collections.Generic;

namespace MapTileDownloader.UI.Messages;

public class DisplayTilesOnMapMessage(IList<TileIndex> tiles)
{
    public IList<TileIndex> Tiles { get; } = tiles;
}