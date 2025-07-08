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
    private bool localHostOnly = true;

    [ObservableProperty]
    private string mbtilesPath = @"C:\Users\autod\Desktop\ESRI影像.mbtiles";

    [ObservableProperty]
    private ushort port = 8888;

    [ObservableProperty]
    private string message;

    [ObservableProperty]
    private bool returnEmptyPngWhenNotFound = true;

    [ObservableProperty]
    private bool route = true;

    [RelayCommand]
    private async Task PickMbtilesFileAsync()
    {
        var storageProvider = SendMessage(new GetStorageProviderMessage()).StorageProvider;
        var options = new FilePickerOpenOptions
        {
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("MB Tiles 地图瓦片数据库文件")
                {
                    Patterns = ["*.mbtiles"],
                }
            },
        };
        var files = await storageProvider.OpenFilePickerAsync(options);
        if (files == null || files.Count != 1 || files[0]?.TryGetLocalPath() is not string filePath)
        {
            return;
        }

        MbtilesPath = filePath;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        Message = $"http://(IP):{Port}/{{z}}/{{x}}/{{y}}";
        try
        {
            Map.LoadLocalTileMaps($"http://localhost:{port}/{{z}}/{{x}}/{{y}}", 20);
            await TileServerService.RunAsync(new TileServerService.TileServerOptions
            {
                LocalhostOnly = localHostOnly,
                MbtilesPath = mbtilesPath,
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
        }
    }
}