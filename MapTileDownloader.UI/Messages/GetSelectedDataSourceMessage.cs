using MapTileDownloader.Models;

namespace MapTileDownloader.UI.Messages;

public class GetSelectedDataSourceMessage
{
    public TileDataSource DataSource { get; set; }
}