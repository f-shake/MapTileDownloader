using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Mapping;

namespace MapTileDownloader.UI.ViewModels;

public partial class LayerListViewModel(IMapService mapService) : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LayerInfo> layers;

    [RelayCommand]
    private void Initialize()
    {
        Layers = new ObservableCollection<LayerInfo>(mapService.Layers);
    }
}