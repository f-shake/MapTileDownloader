using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Services;
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
using MapTileDownloader.UI.Views;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;

namespace MapTileDownloader.UI.ViewModels;

public partial class MbtilesPickerViewModel : ViewModelBase
{
    [ObservableProperty]
    private string file;

    [ObservableProperty]
    private bool useTms = Configs.Instance.MbtilesUseTms;

    public MbtilesPickerViewModel(IMapService mapService, IMainViewControl mainView, IDialogService dialog,
        IStorageProviderService storage)
        : base(mapService, mainView, dialog, storage)
    {
        File = Configs.Instance.MbtilesFile ?? Path.Combine(AppContext.BaseDirectory, "tiles.mbtiles");
    }

    public static event EventHandler FileChanged;

    partial void OnFileChanged(string value)
    {
        Configs.Instance.MbtilesFile = value;
        FileChanged?.Invoke(this, EventArgs.Empty);
    }

    partial void OnUseTmsChanged(bool value)
    {
        Configs.Instance.MbtilesUseTms = value;
        FileChanged?.Invoke(this, EventArgs.Empty);
        Map.RefreshBaseTileGrid();
    }

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
            await Dialog.ShowErrorDialogAsync("无法打开目录", ex);
        }
    }

    [RelayCommand]
    private async Task PickFileAsync()
    {
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
        var file = await Storage.SaveFilePickerAndGetPathAsync(options);
        if (file != null)
        {
            File = file;
        }
    }
}