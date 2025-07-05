using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Helpers;
using MapTileDownloader.UI.Messages;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.ViewModels;

public partial class DownloadViewModel : ViewModelBase
{
    public DownloadViewModel()
    {
        Coordinates = Configs.Instance.DownloadArea;
        MinLevel = Configs.Instance.MinLevel;
        MaxLevel = Configs.Instance.MaxLevel;
        DownloadDir = Configs.Instance.DownloadDir ?? Path.Combine(AppContext.BaseDirectory, "tiles");
        if (Coordinates != null)
        {
            SendMessage(new DisplayPolygonOnMapMessage(Coordinates));
        }
    }

    [ObservableProperty]
    private string selectionMessage;

    [ObservableProperty]
    private bool isEnabled = true;

    [ObservableProperty]
    private Coordinate[] coordinates;

    [ObservableProperty]
    private int minLevel;

    [ObservableProperty]
    private int maxLevel;

    [ObservableProperty]
    private string downloadDir;

    [ObservableProperty]
    private ObservableCollection<LevelTilesViewModel> levels;

    [ObservableProperty]
    private LevelTilesViewModel selectedLevel;

    partial void OnCoordinatesChanged(Coordinate[] value)
    {
        if (value == null)
        {
            SelectionMessage = "还未选择区域";
        }
        else
        {
            SelectionMessage = $"已选择区域（{value.Length}边形）";
        }

        Configs.Instance.DownloadArea = value;
        Configs.Instance.Save();
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task SelectOnMapAsync(CancellationToken cancellationToken)
    {
        IsEnabled = false;
        var m = SendMessage(new SelectOnMapMessage(cancellationToken));
        try
        {
            Coordinates = await m.Task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsEnabled = true;
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        var tileHelper = new TileHelper(SendMessage(new GetSelectedDataSourceMessage()).DataSource);
        Levels = new ObservableCollection<LevelTilesViewModel>();
        var count = tileHelper.EstimateIntersectingTileCount(Coordinates, MaxLevel);
        if (count > 1_000_000)
        {
            await ShowErrorAsync($"瓦片数量过多", "当前设置下，需要下载的瓦片数量可能超过100万个，请缩小区域或降低最大级别");
            return;
        }

        for (int i = MinLevel; i <= MaxLevel; i++)
        {
            var tiles = tileHelper.GetIntersectingTiles(Coordinates, i);
            var levelTile = new LevelTilesViewModel(i, tiles);
            Levels.Add(levelTile);
        }
    }

    partial void OnSelectedLevelChanged(LevelTilesViewModel value)
    {
        if (value == null)
        {
            return;
        }

        if (value.Count < 10_000)
        {
            SendMessage(new DisplayTilesOnMapMessage(value.Tiles));
        }
        else
        {
            SendMessage(new DisplayTilesOnMapMessage(null));
        }
    }
}