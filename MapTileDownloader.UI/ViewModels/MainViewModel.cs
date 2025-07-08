using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;

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
        Map.SetEnable(0,value is 0 or 1);
        Map.SetEnable(1,value is 2 or 3);
    }
}