using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Services;
using MapTileDownloader.UI.Messages;
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

namespace MapTileDownloader.UI.ViewModels;

public partial class MbtilesPickerViewModel : ViewModelBase
{
    public MbtilesPickerViewModel()
    {
        FileChanged += (s, e) => File = Configs.Instance.MbtilesFile;
        File = Configs.Instance.MbtilesFile ?? Path.Combine(AppContext.BaseDirectory, "tiles.mbtiles");
    }

    [ObservableProperty]
    private string file;

    [RelayCommand]
    private async Task OpenDirAsync()
    {
        if (string.IsNullOrWhiteSpace(File))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(Path.GetDirectoryName(File)) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("无法打开目录", ex);
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
        var storageProvider = SendMessage(new GetStorageProviderMessage()).StorageProvider;
        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "mbtiles",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("MB Tiles 地图瓦片数据库文件")
                {
                    Patterns = ["*.mbtiles"],
                }
            },
            ShowOverwritePrompt = false
        };
        var file = await storageProvider.SaveFilePickerAsync(options);
        if (file?.TryGetLocalPath() is not string filePath)
        {
            return;
        }

        File = filePath;
    }

    partial void OnFileChanged(string value)
    {
        Configs.Instance.MbtilesFile = value;
        FileChanged?.Invoke(this, EventArgs.Empty);
    }

    public static event EventHandler FileChanged;
}