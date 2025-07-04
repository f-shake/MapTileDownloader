using MapTileDownloader.Models;

namespace MapTileDownloader.UI.Messages;

public class UpdateTileSourceMessage(TileSource tileSource)
{
    public TileSource TileSource { get; } = tileSource;
}