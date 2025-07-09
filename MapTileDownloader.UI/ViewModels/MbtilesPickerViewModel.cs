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
        File = Configs.Instance.MbtilesFile ?? Path.Combine(AppContext.BaseDirectory, "tiles", GetSanitizeFileName());
    }

    [ObservableProperty]
    private string file;

    private string GetSanitizeFileName()
    {
        string name;
        if (!string.IsNullOrWhiteSpace(TileSource?.Name))
        {
            name = TileSource.Name;
        }
        else if(Configs.Instance.SelectedTileSourcesIndex>=0 && Configs.Instance.SelectedTileSourcesIndex<Configs.Instance.TileSources.Count)
        {
            name = Configs.Instance.TileSources[Configs.Instance.SelectedTileSourcesIndex].Name;
        }
        else
        {
            return "未知.mbtiles";
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // 替换非法字符（Windows 不允许的）
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder();

        foreach (var c in name)
        {
            sanitized.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
        }

        // 去除前后空格，防止出现空文件名
        var result = sanitized.ToString().Trim();

        // 限制长度（如需符合 Windows 限制 255）
        if (result.Length > 128)
        {
            result = result[..128];
        }

        return (string.IsNullOrWhiteSpace(result) ? "未知" : result) + ".mbtiles";
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
            SuggestedFileName = GetSanitizeFileName(),
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

    private static event EventHandler FileChanged;
}