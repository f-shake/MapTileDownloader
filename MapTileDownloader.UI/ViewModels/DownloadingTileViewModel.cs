using System;
using BruTile;
using CommunityToolkit.Mvvm.ComponentModel;
using MapTileDownloader.Models;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadingTileViewModel(TileIndex tileIndex) : ObservableObject, IDownloadingTile
{
    public TileIndex TileIndex { get; } = tileIndex;

    public void SetStatus(DownloadStatus newStatus, string newMessage)
    {
        if (newStatus == Status)
        {
            return;
        }

        var oldStatus = Status;
        Status = newStatus;
        DownloadStatusChanged?.Invoke(this, new DownloadStatusChangedEventArgs(oldStatus, newStatus, newMessage));
    }

    public event EventHandler<DownloadStatusChangedEventArgs> DownloadStatusChanged;

    [ObservableProperty]
    private DownloadStatus status;

    [ObservableProperty]
    private string message;
}