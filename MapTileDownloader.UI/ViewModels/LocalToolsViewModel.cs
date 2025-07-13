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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.ViewModels;

public partial class LocalToolsViewModel : ViewModelBase
{
    [ObservableProperty]
    private string dir = Configs.Instance.ConvertDir;

    [ObservableProperty]
    private int imageQuality = Configs.Instance.MergeImageQuality;

    [ObservableProperty]
    private string information;

    [ObservableProperty]
    private bool isConverting = false;

    [ObservableProperty]
    private bool isOutOfMemory = false;

    [ObservableProperty]
    private bool isProgressIndeterminate;

    [ObservableProperty]
    private bool isServerOn;

    [ObservableProperty]
    private int level = 15;

    [ObservableProperty]
    private bool localHostOnly = Configs.Instance.ServerLocalHostOnly;

    [ObservableProperty]
    private int maxX;

    [ObservableProperty]
    private int maxY;

    [ObservableProperty]
    private string mergeMessage;

    [ObservableProperty]
    private int minX;

    [ObservableProperty]
    private int minY;

    [ObservableProperty]
    private string pattern = Configs.Instance.ConvertPattern;

    [ObservableProperty]
    private ushort port = Configs.Instance.ServerPort;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private bool returnEmptyPngWhenNotFound = Configs.Instance.ServerReturnEmptyPngWhenNotFound;

    [ObservableProperty]
    private string serverMessage;

    [ObservableProperty]
    private int size = 256;

    [ObservableProperty]
    private bool skipExisted = true;

    private MbtilesTileSource localTileSource;

    public LocalToolsViewModel()
    {
        MapAreaSelectorViewModel.CoordinatesChanged += MapAreaSelectorViewModelOnCoordinatesChanged;
    }

    public override async ValueTask InitializeAsync()
    {
        UpdateRange();
        UpdateMergeMessage();
        await UpdateLocalTileLayer();
        await base.InitializeAsync();
        MbtilesPickerViewModel.FileChanged += async (s, e) => await UpdateLocalTileLayer();
    }

    private async Task UpdateLocalTileLayer()
    {
        if (localTileSource != null)
        {
            await localTileSource.DisposeAsync();
        }

        if (File.Exists(Configs.Instance.MbtilesFile))
        {
            localTileSource = new MbtilesTileSource(Configs.Instance.MbtilesFile, Configs.Instance.UseTms);
            await localTileSource.InitializeAsync();
        }
        else
        {
            localTileSource = null;
        }

        Map.LoadLocalTileMaps(localTileSource);
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
            UpdateMergeMessage();
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConverting = false;
        ConvertToMbtilesCommand.Cancel();
        ConvertToFilesCommand.Cancel();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToFilesAsync(CancellationToken cancellationToken)
    {
        IsConverting = true;
        await Task.Delay(1000);
        IsConverting = false;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToMbtilesAsync(CancellationToken cancellationToken)
    {
        IsProgressIndeterminate = true;
        try
        {
            if (string.IsNullOrWhiteSpace(Dir))
            {
                await ShowErrorAsync("转换失败", "目录为空");
                return;
            }

            IsConverting = true;
            var convertService = new TileConvertService();
            var p = new Progress<double>(v =>
            {
                IsProgressIndeterminate = false;
                Progress = v;
            });
            var dirs = Dir.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            await TryWithTabDisabledAsync(
                () => convertService.ConvertToMbtilesAsync(Configs.Instance.MbtilesFile, dirs, Pattern, SkipExisted, p,
                    cancellationToken), "转换失败");
        }
        finally
        {
            IsConverting = false;
            IsProgressIndeterminate = false;
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
        TileMergeService s = new TileMergeService(Configs.Instance.MbtilesFile);
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

    partial void OnDirChanged(string value)
    {
        Configs.Instance.ConvertDir = value;
    }

    partial void OnImageQualityChanged(int value)
    {
        Configs.Instance.MergeImageQuality = value;
    }

    partial void OnLevelChanged(int value)
    {
        UpdateRange();
    }

    partial void OnLocalHostOnlyChanged(bool value)
    {
        Configs.Instance.ServerLocalHostOnly = value;
    }

    partial void OnPatternChanged(string value)
    {
        Configs.Instance.ConvertPattern = value;
    }

    partial void OnPortChanged(ushort value)
    {
        Configs.Instance.ServerPort = value;
    }

    partial void OnReturnEmptyPngWhenNotFoundChanged(bool value)
    {
        Configs.Instance.ServerReturnEmptyPngWhenNotFound = value;
    }

    [RelayCommand]
    private async Task PickDirAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            AllowMultiple = true
        };
        var provider = SendMessage(new GetStorageProviderMessage()).StorageProvider;
        var folders = await provider.OpenFolderPickerAsync(options);
        if (folders == null)
        {
            return;
        }

        Dir = string.Join(Environment.NewLine, folders.Select(p => p.TryGetLocalPath()).ToArray());
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartServerAsync(CancellationToken cancellationToken)
    {
        ServerMessage = $"http://(IP):{Port}/{{z}}/{{x}}/{{y}}";
        try
        {
            IsServerOn = true;
            // Map.LoadLocalTileMaps($"http://localhost:{port}/{{z}}/{{x}}/{{y}}", 20);
            await TileServerService.RunAsync(new TileServerService.TileServerOptions
            {
                LocalhostOnly = LocalHostOnly,
                MbtilesPath = Configs.Instance.MbtilesFile,
                ReturnEmptyPngWhenNotFound = ReturnEmptyPngWhenNotFound,
                Port = Port
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("HTTP服务器出现错误", ex);
        }
        finally
        {
            // Map.LoadLocalTileMaps(null, 0);
            ServerMessage = null;
            IsServerOn = false;
        }
    }

    private void UpdateMergeMessage()
    {
        var tileServer = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = tileServer.EstimateTileMergeMemory(MinX, MaxX, MinY, MaxY, Size);
        MergeMessage =
            $"预计{p / 10000}万像素{Environment.NewLine}占用内存{1.0 * m / 1024 / 1024 / 1024:F2}GB（共{1.0 * MemoryInfoService.Instance.TotalPhysicalMemory / 1024 / 1024 / 1024:F1}GB）";
        IsOutOfMemory = m > 0.75 * MemoryInfoService.Instance.TotalPhysicalMemory;
    }

    private void UpdateRange()
    {
        if (Configs.Instance.Coordinates == null)
        {
            return;
        }

        var tileServer = new TileIntersectionService();
        (int minRow, int maxRow, int minCol, int maxCol) = tileServer.GetTileRange(Configs.Instance.Coordinates, Level);
        MinX = minCol;
        MaxX = maxCol;
        MinY = minRow;
        MaxY = maxRow;
    }
}