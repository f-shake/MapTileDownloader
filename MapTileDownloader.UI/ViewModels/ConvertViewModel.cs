using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FzLib.Avalonia.Messages;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.ViewModels;

public partial class ConvertViewModel : ViewModelBase
{
    [ObservableProperty]
    private string dir = Configs.Instance.ConvertDir;

    [ObservableProperty]
    private string pattern = Configs.Instance.ConvertPattern;

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private bool isConverting = false;

    [ObservableProperty]
    private bool isProgressIndeterminate;

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToFilesAsync(CancellationToken cancellationToken)
    {
        IsConverting = true;
        IsConverting = false;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ConvertToMbtilesAsync(CancellationToken cancellationToken)
    {
        IsProgressIndeterminate = true;
        try
        {
            if (string.IsNullOrWhiteSpace(Dir))
            {
                await ShowErrorAsync("转换失败", "目录为空");
                return;
            }

            IsConverting = true;
            var convertService = new TileConvertService();
            var p = new Progress<double>(v =>
            {
                IsProgressIndeterminate = false;
                Progress = v;
            });
            var dirs = Dir.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            await TryWithTabDisabledAsync(
                () => convertService.ConvertToMbtilesAsync(Configs.Instance.MbtilesFile, dirs, Pattern, p,
                    cancellationToken), "转换失败");
        }
        finally
        {
            IsConverting = false;
            IsProgressIndeterminate = false;
        }
    }


    [RelayCommand]
    private void Cancel()
    {
        IsConverting = false;
        ConvertToMbtilesCommand.Cancel();
        ConvertToFilesCommand.Cancel();
    }

    partial void OnDirChanged(string value)
    {
        Configs.Instance.ConvertDir = value;
    }

    partial void OnPatternChanged(string value)
    {
        Configs.Instance.ConvertPattern = value;
    }

    [RelayCommand]
    private async Task PickDirAsync()
    {
        var options = new FolderPickerOpenOptions
        {
            AllowMultiple = true
        };
        var provider = SendMessage(new GetStorageProviderMessage()).StorageProvider;
        var folders = await provider.OpenFolderPickerAsync(options);
        if (folders == null)
        {
            return;
        }

        Dir = string.Join(Environment.NewLine, folders.Select(p => p.TryGetLocalPath()).ToArray());
    }
}