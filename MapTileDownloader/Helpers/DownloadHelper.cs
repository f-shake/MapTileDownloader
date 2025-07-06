using MapTileDownloader.Models;

namespace MapTileDownloader.Helpers;

public class DownloadHelper
{
    // private async Task<byte[]> HttpDownloadAsync(string url, CancellationToken cancellationToken)
    // {
    //     
    // }
    //
    public async Task DownloadTilesAsync(IEnumerable<IDownloadingLevel> levels,CancellationToken cancellationToken)
    {
        foreach (var level in levels)
        {
            foreach (var tile in level.Tiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(100);
                tile.SetStatus(DownloadStatus.Success, null);
            }
        }
    }
}