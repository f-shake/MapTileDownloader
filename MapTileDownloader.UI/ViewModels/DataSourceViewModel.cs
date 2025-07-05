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
        Sources = new ObservableCollection<TileDataSource>(Configs.Instance.TileSources);
        if (Sources.Count > 0)
        {
            SelectedDataSource = Sources[0];
        }
    }

    [ObservableProperty]
    private TileDataSource selectedDataSource;

    [ObservableProperty]
    private ObservableCollection<TileDataSource> sources;

    public event EventHandler SelectedSourceChanged;
    
    [RelayCommand]

    partial void OnSelectedDataSourceChanged(TileDataSource value)
    {
        CallSelectedSourceChanged();
    }

    [RelayCommand]
    private void CallSelectedSourceChanged()
    {
        SelectedSourceChanged?.Invoke(this,EventArgs.Empty);
    }
    [RelayCommand]

    private void AddSource()
    {
        Sources.Add(new TileDataSource() { Name = "新数据源" });
        SelectedDataSource = Sources[^1];
    }
    
    [RelayCommand]
    private void RemoveSource()
    {
        if (SelectedDataSource != null)
        {
            Sources.Remove(SelectedDataSource);
        }
    }
}