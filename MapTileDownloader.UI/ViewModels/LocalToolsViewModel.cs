using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using MapTileDownloader.TileSources;
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
    private bool isConvertingProgressIndeterminate;

    [ObservableProperty]
    private bool isOutOfMemory = false;
    [ObservableProperty]
    private bool isServerOn;

    [ObservableProperty]
    private int level = 15;

    [ObservableProperty]
    private bool localHostOnly = Configs.Instance.ServerLocalHostOnly;

    private MbtilesTileSource localTileSource;

    [ObservableProperty]
    private MbtilesInfo mbtilesInfo;

    [ObservableProperty]
    private int mergeMaxX;

    [ObservableProperty]
    private int mergeMaxY;

    [ObservableProperty]
    private string mergeMessage;

    [ObservableProperty]
    private int mergeMinX;

    [ObservableProperty]
    private int mergeMinY;

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
    public LocalToolsViewModel()
    {
        MapAreaSelectorViewModel.CoordinatesChanged += MapAreaSelectorViewModelOnCoordinatesChanged;
    }

    public override async ValueTask InitializeAsync()
    {
        UpdateMergeRange();
        UpdateMergeMessage();
        await UpdateLocalTileLayerAsync();
        await UpdateMbtilesInfoAsync();
        await base.InitializeAsync();
        MbtilesPickerViewModel.FileChanged += async (s, e) =>
        {
            await UpdateLocalTileLayerAsync();
            await UpdateMbtilesInfoAsync();
        };
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(MergeMinX)
            or nameof(MergeMinY)
            or nameof(MergeMaxX)
            or nameof(MergeMaxY)
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

    private async Task ConvertAsync(Func<TileConvertService, Progress<double>, Task> convertAction)
    {
        IsConvertingProgressIndeterminate = true;
        IsConverting = true;
        await TryWithTabDisabledAsync(async () =>
        {

            var convertService = new TileConvertService();
            var p = new Progress<double>(v =>
            {
                IsConvertingProgressIndeterminate = false;
                Progress = v;
            });
            await convertAction(convertService, p);
        });
        IsConverting = false;
        IsConvertingProgressIndeterminate = false;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToFilesAsync(CancellationToken cancellationToken)
    {
        var dirs = Dir.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        if (dirs.Length > 1)
        {
            await ShowErrorAsync("转换失败", "请只选择一个目录进行转换");
            return;
        }
        await ConvertAsync(async (s, p) =>
        {
            await s.ConvertToFilesAsync(Configs.Instance.MbtilesFile, dirs[0], Pattern, SkipExisted, p, cancellationToken);
        });
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToMbtilesAsync(CancellationToken cancellationToken)
    {
        var dirs = Dir.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        if (dirs.Length > 1)
        {
            await ShowErrorAsync("转换失败", "请只选择一个目录进行转换");
            return;
        }
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                await ShowErrorAsync("转换失败", $"目录{dir}不存在");
                return;
            }
        }
        await ConvertAsync(async (s, p) =>
        {
            await s.ConvertToMbtilesAsync(Configs.Instance.MbtilesFile, dirs, Pattern, SkipExisted, p, cancellationToken);
        });
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
        UpdateMergeRange();
    }

    [RelayCommand]
    private async Task MergeAsync()
    {
        TileMergeService s = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = s.EstimateTileMergeMemory(MergeMinX, MergeMaxX, MergeMinY, MergeMaxY, Size);
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
                await s.MergeTilesAsync(filePath, Configs.Instance.MbtilesUseTms, Level, MergeMinX, MergeMaxX, MergeMinY, MergeMaxY, Size, ImageQuality);
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
        UpdateMergeRange();
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

    private async Task UpdateLocalTileLayerAsync()
    {
        if (localTileSource != null)
        {
            await localTileSource.DisposeAsync();
        }

        if (File.Exists(Configs.Instance.MbtilesFile))
        {
            localTileSource = new MbtilesTileSource(Configs.Instance.MbtilesFile, Configs.Instance.MbtilesUseTms);
            await localTileSource.InitializeAsync();
        }
        else
        {
            localTileSource = null;
        }

        Map.LoadLocalTileMaps(localTileSource);
    }

    [RelayCommand]
    private async Task UpdateMbtilesInfoAsync()
    {
        if (File.Exists(Configs.Instance.MbtilesFile))
        {
            try
            {
                using var s = new MbtilesService(Configs.Instance.MbtilesFile, true);
                await s.InitializeAsync();
                MbtilesInfo = await s.GetMbtilesInfoAsync(Configs.Instance.MbtilesUseTms);
            }
            catch (Exception ex)
            {
                MbtilesInfo = null;
            }
        }
        else
        {
            MbtilesInfo = null;
        }
    }
    private void UpdateMergeMessage()
    {
        var tileServer = new TileMergeService(Configs.Instance.MbtilesFile);
        (long p, long m) = tileServer.EstimateTileMergeMemory(MergeMinX, MergeMaxX, MergeMinY, MergeMaxY, Size);
        MergeMessage =
            $"预计{p / 10000}万像素{Environment.NewLine}占用内存{1.0 * m / 1024 / 1024 / 1024:F2}GB（共{1.0 * MemoryInfoService.Instance.TotalPhysicalMemory / 1024 / 1024 / 1024:F1}GB）";
        IsOutOfMemory = m > 0.75 * MemoryInfoService.Instance.TotalPhysicalMemory;
    }

    private void UpdateMergeRange()
    {
        if (Configs.Instance.Coordinates == null)
        {
            return;
        }

        var tileServer = new TileIntersectionService(Configs.Instance.MbtilesUseTms);
        (int minRow, int maxRow, int minCol, int maxCol) = tileServer.GetTileRange(Configs.Instance.Coordinates, Level);
        MergeMinX = minCol;
        MergeMaxX = maxCol;
        MergeMinY = minRow;
        MergeMaxY = maxRow;
    }
}