using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.ViewModels;

public partial class ServerViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool isServerOn;

    [ObservableProperty]
    private bool localHostOnly = Configs.Instance.ServerLocalHostOnly;

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private ushort port = Configs.Instance.ServerPort;

    [ObservableProperty]
    private bool returnEmptyPngWhenNotFound = Configs.Instance.ServerReturnEmptyPngWhenNotFound;

    partial void OnLocalHostOnlyChanged(bool value)
    {
        Configs.Instance.ServerLocalHostOnly= value;
    }

    partial void OnPortChanged(ushort value)
    {
        Configs.Instance.ServerPort = value;
    }

    partial void OnReturnEmptyPngWhenNotFoundChanged(bool value)
    {
        Configs.Instance.ServerReturnEmptyPngWhenNotFound= value;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        Message = $"http://(IP):{Port}/{{z}}/{{x}}/{{y}}";
        try
        {
            IsServerOn = true;
            Map.LoadLocalTileMaps($"http://localhost:{port}/{{z}}/{{x}}/{{y}}", 20);
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
            Map.LoadLocalTileMaps(null, 0);
            Message = null;
            IsServerOn = false;
        }
    }
}