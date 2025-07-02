using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.Views;
using Mapsui;

namespace MapTileDownloader.UI.ViewModels;

public partial class DataSourceViewModel : ViewModelBase
{
    public DataSourceViewModel()
    {
        Sources = new ObservableCollection<TileSource>(Configs.Instance.TileSources);
        if (Sources.Count > 0)
        {
            SelectedSource = Sources[0];
        }
    }

    [ObservableProperty]
    private TileSource selectedSource;

    [ObservableProperty]
    private ObservableCollection<TileSource> sources;

    [RelayCommand]
    private void AddSource()
    {
        Sources.Add(new TileSource() { Name = "新数据源" });
        SelectedSource = Sources[^1];
    }
    
    [RelayCommand]
    private void RemoveSource()
    {
        if (SelectedSource != null)
        {
            Sources.Remove(SelectedSource);
        }
    }
}