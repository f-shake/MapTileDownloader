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
    private int imageQuality = Configs.Instance.MergeImageQuality;

    [ObservableProperty]
    private bool isOutOfMemory = false;

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
            or nameof(ImageQuality)
            or nameof(Level))
        {
            UpdateMessage();
        }
    }

    private async Task<string> GetSaveFilePathAsync()
    {
        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "jpg",
            SuggestedFileName = "tiles.jpg",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("JPEG 图片文件")
                {
                    Patterns = ["*.jpg", "*.jpeg"],
                    MimeTypes = ["image/jpeg"]
                },
                new FilePickerFileType("PNG 图片文件")
                {
                    Patterns = ["*.png"],
                    MimeTypes = ["image/png"]
                },
                new FilePickerFileType("BMP 图片文件")
                {
                    Patterns = ["*.bmp"],
                    MimeTypes = ["image/bmp"]
                },
                new FilePickerFileType("WebP 图片文件")
                {
                    Patterns = ["*.webp"],
                    MimeTypes = ["image/webp"]
                }
            }
        };
        var file = await SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(options);

        return file?.TryGetLocalPath();
    }

    private void MapAreaSelectorViewModelOnCoordinatesChanged(object sender, EventArgs e)
    {
        UpdateRange();
    }

    [RelayCommand]
    private async Task MergeAsync()
    {
        await using TileMergeService s = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = s.EstimateTileMergeMemory(MinX, MaxX, MinY, MaxY, Size);
        if (m > 0.75 * MemoryInfoService.Instance.TotalPhysicalMemory)
        {
            await ShowErrorAsync("内存不足",
                $"预计占用内存{1.0 * m / 1024 / 1024 / 1024:F2}GB，超过系统总内存的75%（共{1.0 * MemoryInfoService.Instance.TotalPhysicalMemory / 1024 / 1024 / 1024:F1}GB）");
            return;
        }

        var filePath = await GetSaveFilePathAsync();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        await TryWithLoadingAsync(
            () => Task.Run(async () =>
            {
                await s.MergeTilesAsync(filePath, Level, MinX, MaxX, MinY, MaxY, Size, ImageQuality);
            }), "拼接失败");
    }

    partial void OnImageQualityChanged(int value)
    {
        Configs.Instance.MergeImageQuality = value;
    }

    partial void OnLevelChanged(int value)
    {
        UpdateRange();
    }

    private void UpdateMessage()
    {
        using var tileServer = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = tileServer.EstimateTileMergeMemory(MinX, MaxX, MinY, MaxY, Size);
        Message =
            $"预计{p / 10000}万像素{Environment.NewLine}占用内存{1.0 * m / 1024 / 1024 / 1024:F2}GB（共{1.0 * MemoryInfoService.Instance.TotalPhysicalMemory / 1024 / 1024 / 1024:F1}GB）";
        IsOutOfMemory = m > 0.75 * MemoryInfoService.Instance.TotalPhysicalMemory;
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