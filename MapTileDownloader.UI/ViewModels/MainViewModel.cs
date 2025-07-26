using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using Mapsui;
using MapTileDownloader.Enums;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Views;

namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel(
    IMapService mapService,
    IMainViewControl mainView,
    IDialogService dialog,
    IStorageProviderService storage,
    DownloadViewModel downloadViewModel,
    LocalToolsViewModel localToolsPanel)
    : ViewModelBase(mapService, mainView, dialog, storage)
{
    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private bool isProgressRingVisible = false;

    public DownloadViewModel DownloadViewModel { get; } = downloadViewModel;

    public LocalToolsViewModel LocalToolsViewModel { get; } = localToolsPanel;

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