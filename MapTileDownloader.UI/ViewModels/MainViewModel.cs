using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using Mapsui;
using MapTileDownloader.Enums;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Enums;
using MapTileDownloader.UI.Mapping;
using MapTileDownloader.UI.Views;

namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool canPickMbtiles = true;

    [ObservableProperty]
    private bool canSelectMapArea = true;

    [ObservableProperty]
    private bool isLocalTabEnabled = true;

    [ObservableProperty]
    private bool isOnlineTabEnabled = true;

    [ObservableProperty]
    private int selectedTabIndex = -1;

    public MainViewModel(IMapService mapService,
        IDialogService dialog,
        IStorageProviderService storage,
        IProgressOverlayService progressOverlay,
        DownloadViewModel downloadViewModel,
        LocalToolsViewModel localToolsPanel) : base(mapService, progressOverlay, dialog, storage)
    {
        DownloadViewModel = downloadViewModel;
        LocalToolsViewModel = localToolsPanel;

        BeginOperation += (s, e) =>
        {
            if (e.DisablePickingMbtiles)
            {
                CanPickMbtiles = false;
            }

            if (e.DisableSelectingMapArea)
            {
                CanSelectMapArea = false;
            }

            if (e.DisableSelectingTab)
            {
                IsOnlineTabEnabled = SelectedTabIndex == 0;
                IsLocalTabEnabled = SelectedTabIndex == 1;
            }
        };

        EndOperation += (s, e) =>
        {
            IsOnlineTabEnabled = true;
            IsLocalTabEnabled = true;
            CanPickMbtiles = true;
            CanSelectMapArea = true;
        };
    }

    public DownloadViewModel DownloadViewModel { get; }

    public LocalToolsViewModel LocalToolsViewModel { get; }

    [RelayCommand]
    public override async Task InitializeAsync()
    {
        await ProgressOverlay.WithOverlayAsync(async () =>
        {
            await DownloadViewModel.InitializeAsync();
            await LocalToolsViewModel.InitializeAsync();
            // Map.LoadOnlineTileMaps(DownloadViewModel.SelectedDataSource);
            if (Configs.Instance.Coordinates != null && Configs.Instance.Coordinates.Length >= 3)
            {
                Map.DisplayPolygon(Configs.Instance.Coordinates);
            }

            Map.SetVisible(PanelType.Online);

            await base.InitializeAsync();
        }, initialMessage: "正在初始化");
    }

    async partial void OnSelectedTabIndexChanged(int value)
    {
        if (value == 0)
        {
            CurrentPanelType = PanelType.Online;
        }
        else
        {
            await LocalToolsViewModel.UpdateLocalTileAsync();
            CurrentPanelType = PanelType.Local;
        }

        Map.SetVisible(CurrentPanelType);
    }
}