using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MapTileDownloader.Models;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadingLevelViewModel : ObservableObject, IDownloadingLevel
{
    [ObservableProperty]
    private int downloadedCount;

    public DownloadingLevelViewModel(int level, IEnumerable<DownloadingTileViewModel> tiles)
    {
        Level = level;
        Tiles = new List<IDownloadingTile>(tiles);
        foreach (var tile in Tiles)
        {
            tile.DownloadStatusChanged += (s, e) =>
            {
                if (e.OldStatus == e.NewStatus)
                {
                    return;
                }

                if (e.OldStatus is DownloadStatus.Ready or DownloadStatus.Downloading)
                {
                    DownloadedCount++;
                }

                if (e.NewStatus is DownloadStatus.Ready or DownloadStatus.Downloading)
                {
                    DownloadedCount--;
                }
            };
        }
    }

    public int Count => Tiles.Count;
    public int Level { get; set; }
    public IList<IDownloadingTile> Tiles { get; set; }
}