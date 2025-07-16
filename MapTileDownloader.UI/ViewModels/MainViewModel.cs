using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using MapTileDownloader.Enums;

namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedTabIndex;

    public DownloadViewModel DownloadViewModel { get; } = new DownloadViewModel();

    public LocalToolsViewModel LocalToolsViewModel { get; } = new LocalToolsViewModel();

    public override async ValueTask InitializeAsync()
    {
        await DownloadViewModel.InitializeAsync();
        await LocalToolsViewModel.InitializeAsync();
        Map.LoadTileMaps(DownloadViewModel.SelectedDataSource);
        if (Configs.Instance.Coordinates != null && Configs.Instance.Coordinates.Length >= 3)
        {
            Map.DisplayPolygon(Configs.Instance.Coordinates);
        }

        await base.InitializeAsync();
    }

    async partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 0)
        {

            Map.SetEnable(AppLayer.BaseLayer);
        }
        else
        {
            await LocalToolsViewModel.UpdateLocalTileAsync();
            Map.SetEnable(AppLayer.LocalBaseLayer);
        }
    }
}