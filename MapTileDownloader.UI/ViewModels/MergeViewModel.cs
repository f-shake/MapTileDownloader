using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.ViewModels;

public partial class MergeViewModel : ViewModelBase
{
    [ObservableProperty]
    private int level = 15;

    [ObservableProperty]
    private int maxX;

    [ObservableProperty]
    private int maxY;

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private int minX;

    [ObservableProperty]
    private int minY;

    [ObservableProperty]
    private int size = 256;

    public MergeViewModel()
    {
        MapAreaSelectorViewModel.CoordinatesChanged += MapAreaSelectorViewModelOnCoordinatesChanged;
    }

    public override void Initialize()
    {
        base.Initialize();
        UpdateRange();
        UpdateMessage();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(MinX)
            or nameof(MinY)
            or nameof(MaxX)
            or nameof(MaxY)
            or nameof(Size)
            or nameof(Level))
        {
            UpdateMessage();
        }
    }

    private void MapAreaSelectorViewModelOnCoordinatesChanged(object sender, EventArgs e)
    {
        UpdateRange();
    }

    [RelayCommand]
    private async Task MergeAsync()
    {
        //待检查内存
        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "png",
            SuggestedFileName = "tiles.png",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("PNG 图片文件")
                {
                    Patterns = ["*.png"],
                    MimeTypes = ["image/png"]
                },
                new FilePickerFileType("JPEG 图片文件")
                {
                    Patterns = ["*.jpg", "*.jpeg"],
                    MimeTypes = ["image/jpeg"]
                },
                new FilePickerFileType("BMP 图片文件")
                {
                    Patterns = ["*.bmp"],
                    MimeTypes = ["image/bmp"]
                },
            }
        };
        var file = await SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(options);

        var filePath = file?.TryGetLocalPath();
        if (filePath == null)
        {
            return;
        }

        TileMergeService s = new TileMergeService(Configs.Instance.MbtilesFile);
        await s.MergeTilesAsync(filePath, Level, MinX, MaxX, MinY, MaxY);
    }

    partial void OnLevelChanged(int value)
    {
        UpdateRange();
    }

    private void UpdateMessage()
    {
        var tileServer = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = tileServer.EstimateTileMergeMemory(MinX, MaxX, MinY, MaxY, Size);
        Message = $"预计{p / 10000}万像素，占用内存{1.0 * m / 1024 / 1024 / 1024:F2}GB";
    }

    private void UpdateRange()
    {
        if (Configs.Instance.Coordinates == null)
        {
            return;
        }

        var tileServer = new TileIntersectionService(TileSource);
        (int minRow, int maxRow, int minCol, int maxCol) =
            tileServer.GetTileRange(Configs.Instance.Coordinates, Level);
        MinX = minCol;
        MaxX = maxCol;
        MinY = minRow;
        MaxY = maxRow;
    }
}