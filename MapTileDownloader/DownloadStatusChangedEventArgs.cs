namespace MapTileDownloader;

public class DownloadStatusChangedEventArgs(DownloadStatus oldStatus, DownloadStatus newStatus, string message = null)
{
    public DownloadStatus OldStatus { get; } = oldStatus;
    public DownloadStatus NewStatus { get; } = newStatus;
    public string Message { get; } = message;
}