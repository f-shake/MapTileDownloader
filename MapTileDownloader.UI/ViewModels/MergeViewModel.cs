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
    [RelayCommand]
    private async Task TryAsync()
    {
        TileMergeService s = new TileMergeService("C:\\Users\\autod\\Desktop\\OSM.mbtiles");
        await s.MergeTilesAsync("temp.jpg", 16, 54774, 54980, 27008, 27187);
    }
}