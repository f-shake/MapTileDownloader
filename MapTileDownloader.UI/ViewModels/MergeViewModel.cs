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
public partial class MergeViewModel : ViewModelBase
{
    [ObservableProperty]
    private int level;

    [ObservableProperty]
    private int maxX;

    [ObservableProperty]
    private int maxY;

    [ObservableProperty]
    private int minX;

    [ObservableProperty]
    private int minY;

    [RelayCommand]
    private async Task MergeAsync()
    {
        var options = new FilePickerSaveOptions
        {
            DefaultExtension = "png",
            SuggestedFileName = "tiles.png",
            FileTypeChoices = new List<FilePickerFileType>
            {
                new FilePickerFileType("PNG 图片文件")
                {
                    Patterns = ["*.png"],
                    MimeTypes = ["image/png"]
                },
                new FilePickerFileType("JPEG 图片文件")
                {
                    Patterns = ["*.jpg", "*.jpeg"],
                    MimeTypes = ["image/jpeg"]
                },
                new FilePickerFileType("BMP 图片文件")
                {
                    Patterns = ["*.bmp"],
                    MimeTypes = ["image/bmp"]
                },
            }
        };
        var file = await SendMessage(new GetStorageProviderMessage()).StorageProvider.SaveFilePickerAsync(options);

        var filePath = file?.TryGetLocalPath();
        if (filePath == null)
        {
            return;
        }

        TileMergeService s = new TileMergeService(Configs.Instance.MbtilesFile);
        await s.MergeTilesAsync(filePath, Level, MinX, MaxX, MinY, MaxY);
    }
}