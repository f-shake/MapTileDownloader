using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
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
            if(tile.Status is DownloadStatus.Skip or DownloadStatus.Failed or DownloadStatus.Success)
            {
                downloadedCount++;
            }
            tile.DownloadStatusChanged += (s, e) =>
            {
                if (e.OldStatus == e.NewStatus)
                {
                    return;
                }

                if (e.OldStatus < e.NewStatus && e.OldStatus <= DownloadStatus.Downloading &&
                    e.NewStatus > DownloadStatus.Downloading)
                {
                    Interlocked.Increment(ref downloadedCount);
                    OnPropertyChanged(nameof(DownloadedCount));
                    DownloadedCountIncrease?.Invoke(this, e);
                }
            };
        }
    }

    public event EventHandler<DownloadStatusChangedEventArgs> DownloadedCountIncrease;

    public int Count => Tiles.Count;
    public int Level { get; set; }
    public IList<IDownloadingTile> Tiles { get; set; }
}