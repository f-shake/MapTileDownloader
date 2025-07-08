using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.ViewModels;
public partial class ServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string mbtilesPath = @"C:\Users\autod\Desktop\ESRI影像.mbtiles";

    [ObservableProperty]
    private ushort port = 8888;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        Map.LoadLocalTileMaps($"http://localhost:{port}/{{z}}/{{x}}/{{y}}", 20);
        await TileServerService.RunAsync(MbtilesPath, Port, cancellationToken);
        Map.LoadLocalTileMaps(null, 0);
    }
}