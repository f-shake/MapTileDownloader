using MapTileDownloader.Models;

namespace MapTileDownloader.UI.Messages;

public class UpdateTileSourceMessage(TileDataSource tileDataSource)
{
    public TileDataSource TileDataSource { get; } = tileDataSource;
}