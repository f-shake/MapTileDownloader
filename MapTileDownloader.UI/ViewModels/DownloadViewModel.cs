using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Messages;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Mapping;
using System.Diagnostics;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool canDownload = false;

    [ObservableProperty]
    private int downloadedCount;

    [ObservableProperty]
    private ObservableCollection<DownloadStatusChangedEventArgs> errorTiles = new();

    [ObservableProperty]
    private int failedCount;

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private ObservableCollection<DownloadingLevelViewModel> levels;

    [ObservableProperty]
    private int maxConcurrency = 10;

    private int maxDownloadingLevel;

    [ObservableProperty]
    private int maxLevel = Configs.Instance.MaxLevel;

    [ObservableProperty]
    private int minLevel = Configs.Instance.MinLevel;

    [ObservableProperty]
    private DownloadingLevelViewModel selectedLevel;

    [ObservableProperty]
    private int skipCount;

    [ObservableProperty]
    private int successCount;

    [ObservableProperty]
    private int totalCount;

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanDownload))]
    private async Task DownloadTilesAsync(CancellationToken cancellationToken)
    {
        IsDownloading = true;
        using var downloader = new TileDownloadService(TileSource, Configs.Instance.MbtilesFile, MaxConcurrency);

        try
        {
            await downloader.InitializeAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("初始化失败", ex);
            IsDownloading = false;
            return;
        }

        foreach (var level in Levels)
        {
            RegisterDownloadEvent(level);
        }

        try
        {
            await Task.Run(async () =>
            {
                await downloader.DownloadTilesAsync(Levels, cancellationToken);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("下载失败", ex);
        }
        finally
        {
            CanDownload = false;
            DownloadTilesCommand.NotifyCanExecuteChanged();
        }
        IsDownloading = false;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (Configs.Instance.Coordinates == null || Configs.Instance.Coordinates.Length < 3)
        {
            await ShowErrorAsync("初始化失败", "请先选择下载区域");
            return;
        }

        TotalCount = 0;
        DownloadedCount = 0;
        successCount = 0;
        failedCount = 0;
        skipCount = 0;
        maxDownloadingLevel = 0;
        ErrorTiles.Clear();

        var tileSource = TileSource;
        var tileHelper = new TileService(tileSource);

        await TryWithLoadingAsync(Task.Run(() =>
        {
            Levels = new ObservableCollection<DownloadingLevelViewModel>();
            var count = tileHelper.EstimateIntersectingTileCount(Configs.Instance.Coordinates, MaxLevel);
            if (count > 1_000_000)
            {
                throw new Exception("当前设置下，需要下载的瓦片数量可能超过100万个，请缩小区域或降低最大级别");
            }

            for (int i = MinLevel; i <= MaxLevel; i++)
            {
                var tiles = tileHelper.GetIntersectingTiles(Configs.Instance.Coordinates, i);
                var levelTile = new DownloadingLevelViewModel(i, tiles.Select(p => new DownloadingTileViewModel(p)));
                Levels.Add(levelTile);
            }

            TotalCount = Levels.Select(p => p.Count).Sum();


            Map.LoadTileGridsAsync(tileSource, Levels);
        }));

        CanDownload = true;
        DownloadTilesCommand.NotifyCanExecuteChanged();
    }

    partial void OnMaxLevelChanged(int value)
    {
        Configs.Instance.MaxLevel = MaxLevel;
    }

    partial void OnMinLevelChanged(int value)
    {
        Configs.Instance.MinLevel = MaxLevel;
    }

    partial void OnSelectedLevelChanged(DownloadingLevelViewModel value)
    {
        if (value == null)
        {
            return;
        }

        Map.DisplayTileGrids(value.Level);
    }

    private void RegisterDownloadEvent(DownloadingLevelViewModel level)
    {
        level.DownloadedCountIncrease += (s, e) =>
        {
            Interlocked.Increment(ref downloadedCount);
            switch (e.NewStatus)
            {
                case DownloadStatus.Success:
                    Interlocked.Increment(ref successCount);
                    break;
                case DownloadStatus.Skip:
                    Interlocked.Increment(ref skipCount);
                    break;
                case DownloadStatus.Failed:
                    Interlocked.Increment(ref failedCount);
                    ErrorTiles.Add(e);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            maxDownloadingLevel = Math.Max(maxDownloadingLevel, e.Tile.TileIndex.Level);
            if (SelectedLevel != Levels[maxDownloadingLevel])
            {
                SelectedLevel = Levels[maxDownloadingLevel];
            }
            OnPropertyChanged(nameof(DownloadedCount));
            OnPropertyChanged(nameof(SkipCount));
            OnPropertyChanged(nameof(DownloadedCount));
            OnPropertyChanged(nameof(SuccessCount));
            OnPropertyChanged(nameof(FailedCount));
        };
    }
}