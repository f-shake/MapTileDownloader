using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using MapTileDownloader.Enums;

namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedTabIndex;

    public MainViewModel()
    {
    }

    public DataSourceViewModel DataSourceViewModel { get; } = new DataSourceViewModel();

    public DownloadViewModel DownloadViewModel { get; } = new DownloadViewModel();

    public ServerViewModel ServerViewModel { get; } = new ServerViewModel();

    public MergeViewModel MergeViewModel { get; } = new MergeViewModel();

    public override void Initialize()
    {
        DataSourceViewModel.Initialize();
        DownloadViewModel.Initialize();
        ServerViewModel.Initialize();
        Map.LoadTileMaps(DataSourceViewModel.SelectedDataSource);
        base.Initialize();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        Map.SetEnable(AppLayer.BaseLayer,value is 0 or 1);
        Map.SetEnable(AppLayer.LocalBaseLayer,value is 2 or 3);
        Map.SetEnable(AppLayer.TileGridLayer,value is 1);
        Map.SetEnable(AppLayer.DrawingLayer,value is 0 or 1);
    }
}