using System;
using BruTile;
using CommunityToolkit.Mvvm.ComponentModel;
using MapTileDownloader.Models;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadingTileViewModel(TileIndex tileIndex) : ObservableObject, IDownloadingTile
{
    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private DownloadStatus status;

    public event EventHandler<DownloadStatusChangedEventArgs> DownloadStatusChanged;

    public TileIndex TileIndex { get; } = tileIndex;

    public void SetStatus(DownloadStatus newStatus, string message, string detail)
    {
        if (newStatus == Status)
        {
            return;
        }

        var oldStatus = Status;
        Status = newStatus;
        DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEventArgs(this, oldStatus, newStatus, message, detail));
    }
}