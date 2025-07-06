using BruTile;

namespace MapTileDownloader.Models;

public interface IDownloadingTile
{
    public TileIndex TileIndex { get; }

    public DownloadStatus Status { get; }

    public void SetStatus(DownloadStatus newStatus, string newMessage);

    public event EventHandler<DownloadStatusChangedEventArgs> DownloadStatusChanged;
}