using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Services;
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
using BruTile;
using FzLib.Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;

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
    private int maxConcurrency = Configs.Instance.MaxDownloadConcurrency;

    private int maxDownloadingLevel;

    [ObservableProperty]
    private int maxLevel = Configs.Instance.MaxLevel;

    [ObservableProperty]
    private int minLevel = Configs.Instance.MinLevel;

    [ObservableProperty]
    private TileDataSource selectedDataSource;

    [ObservableProperty]
    private DownloadingLevelViewModel selectedLevel;

    [ObservableProperty]
    private int skipCount;

    [ObservableProperty]
    private ObservableCollection<TileDataSource> sources;

    [ObservableProperty]
    private int successCount;

    [ObservableProperty]
    private int totalCount;

    public DownloadViewModel(IMapService mapService, IDialogService dialog, IProgressOverlayService progressOverlay,
        IStorageProviderService storage)
        : base(mapService, progressOverlay, dialog, storage)
    {
        Sources = new ObservableCollection<TileDataSource>(Configs.Instance.TileSources);
    }

    public override Task InitializeAsync()
    {
        if (Configs.Instance.SelectedTileSourcesIndex >= 0 && Configs.Instance.SelectedTileSourcesIndex < Sources.Count)
        {
            SelectedDataSource = Sources[Configs.Instance.SelectedTileSourcesIndex];
        }

        return base.InitializeAsync();
    }

    [RelayCommand]
    private void AddSource()
    {
        Sources.Add(new TileDataSource() { Name = "新数据源" });
        SelectedDataSource = Sources[^1];
        SaveToConfig();
    }

    [RelayCommand]
    private void CallSelectedSourceChanged()
    {
        OnSelectedDataSourceChanged(SelectedDataSource);
    }

    [RelayCommand(IncludeCancelCommand = true, CanExecute = nameof(CanDownload))]
    private async Task DownloadTilesAsync(CancellationToken cancellationToken)
    {
        IsDownloading = true;
        await using var downloader =
            new TileDownloadService(SelectedDataSource, Configs.Instance.MbtilesFile, MaxConcurrency);
        await TryDoingAsync(async () =>
        {
            try
            {
                await downloader.InitializeAsync();
            }
            catch (Exception ex)
            {
                await Dialog.ShowErrorDialogAsync("初始化失败", ex);
                IsDownloading = false;
                return;
            }

            foreach (var level in Levels)
            {
                RegisterDownloadEvent(level);
            }

            try
            {
                await Task.Run(async () => { await downloader.DownloadTilesAsync(Levels, cancellationToken); },
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                await Dialog.ShowErrorDialogAsync("下载失败", ex);
            }
            finally
            {
                CanDownload = false;
                DownloadTilesCommand.NotifyCanExecuteChanged();
            }
        }, "地图选择错误");
        IsDownloading = false;
    }

    [RelayCommand]
    private async Task InitializeDownloadingAsync()
    {
        if (Configs.Instance.Coordinates == null || Configs.Instance.Coordinates.Length < 3)
        {
            await Dialog.ShowErrorDialogAsync("初始化失败", "请先选择下载区域");
            return;
        }

        TotalCount = 0;
        DownloadedCount = 0;
        SuccessCount = 0;
        FailedCount = 0;
        SkipCount = 0;
        maxDownloadingLevel = 0;
        ErrorTiles.Clear();

        var tileHelper = new TileIntersectionService(false);
        bool succeed = false;
        await ProgressOverlay.WithOverlayAsync(async () =>
        {
            Levels = new ObservableCollection<DownloadingLevelViewModel>();
            var count = tileHelper.EstimateIntersectingTileCount(Configs.Instance.Coordinates, MaxLevel);
            if (count > 1_000_000)
            {
                throw new Exception("当前设置下，需要下载的瓦片数量可能超过100万个，请缩小区域或降低最大级别");
            }

            ISet<TileIndex> existingTiles = null;
            using (var mbtilesService = new MbtilesService(Configs.Instance.MbtilesFile, false))
            {
                await mbtilesService.InitializeAsync();
                existingTiles = await mbtilesService.GetExistingTilesAsync();
            }

            for (int i = MinLevel; i <= MaxLevel; i++)
            {
                var tiles = tileHelper.GetIntersectingTiles(Configs.Instance.Coordinates, i);
                var levelTile = new DownloadingLevelViewModel(i, tiles.Select(p =>
                {
                    var tile = new DownloadingTileViewModel(p);
                    if (existingTiles.Contains(p))
                    {
                        tile.SetStatus(DownloadStatus.Skip, null, null);
                    }

                    return tile;
                }));
                Levels.Add(levelTile);
            }

            TotalCount = Levels.Select(p => p.Count).Sum();
            DownloadedCount = Levels.Select(p => p.DownloadedCount).Sum();
            SkipCount = Levels.Select(p => p.DownloadedCount).Sum();
            await base.Map.LoadTileGridsAsync(SelectedDataSource, (IEnumerable<IDownloadingLevel>)Levels);
            succeed = true;
        }, async ex => { await Dialog.ShowErrorDialogAsync("初始化失败", ex); });

        if (succeed)
        {
            if (Levels.All(p => p.DownloadedCount == p.Count))
            {
                await Dialog.ShowOkDialogAsync("无需下载", "所选范围内的指定瓦片均已下载");
            }
            else
            {
                CanDownload = true;
                DownloadTilesCommand.NotifyCanExecuteChanged();
            }
        }
    }

    partial void OnMaxConcurrencyChanged(int value)
    {
        Configs.Instance.MaxDownloadConcurrency = value;
    }

    partial void OnMaxLevelChanged(int value)
    {
        Configs.Instance.MaxLevel = MaxLevel;
    }

    partial void OnMinLevelChanged(int value)
    {
        Configs.Instance.MinLevel = MaxLevel;
    }

    partial void OnSelectedDataSourceChanged(TileDataSource value)
    {
        Map.LoadTileMaps(value);
        SaveToConfig();
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

    [RelayCommand]
    private void RemoveSource()
    {
        if (SelectedDataSource != null)
        {
            Sources.Remove(SelectedDataSource);
        }

        if (Sources.Count == 0)
        {
            Sources.Add(new TileDataSource { Name = "新数据源" });
            SelectedDataSource = Sources[0];
        }

        SaveToConfig();
    }

    private void SaveToConfig()
    {
        Configs.Instance.TileSources = [.. Sources];
        Configs.Instance.SelectedTileSourcesIndex = Sources.IndexOf(SelectedDataSource);
    }
}