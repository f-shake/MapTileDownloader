namespace MapTileDownloader.Models;

public interface IDownloadingLevel
{
    public int Level { get; }
    public IList<IDownloadingTile> Tiles { get; }
}