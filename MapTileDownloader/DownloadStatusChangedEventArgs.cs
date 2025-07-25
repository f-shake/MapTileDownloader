using MapTileDownloader.Models;

namespace MapTileDownloader;

public class DownloadStatusChangedEventArgs(
    IDownloadingTile tile,
    DownloadStatus oldStatus,
    DownloadStatus newStatus,
    string message = null,
    string detail=null)
{
    public IDownloadingTile Tile { get; } = tile;
    public DownloadStatus OldStatus { get; } = oldStatus;
    public DownloadStatus NewStatus { get; } = newStatus;
    public string Message { get; } = message;
    public string Detail { get; } = detail;
}