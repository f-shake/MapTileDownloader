using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapTileDownloader.Models;
using System;
using System.Collections.ObjectModel;

namespace MapTileDownloader.UI.ViewModels;
public partial class DataSourceViewModel : ViewModelBase
{
    [ObservableProperty]
    private TileDataSource selectedDataSource;

    [ObservableProperty]
    private ObservableCollection<TileDataSource> sources;

    public DataSourceViewModel()
    {
        Sources = new ObservableCollection<TileDataSource>(Configs.Instance.TileSources);
        if (Sources.Count > 0)
        {
            SelectedDataSource = Sources[0];
        }
    }
    public event EventHandler SelectedSourceChanged;

    [RelayCommand]
    private void AddSource()
    {
        Sources.Add(new TileDataSource() { Name = "新数据源" });
        SelectedDataSource = Sources[^1];
    }

    [RelayCommand]
    private void CallSelectedSourceChanged()
    {
        SelectedSourceChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    partial void OnSelectedDataSourceChanged(TileDataSource value)
    {
        Map.LoadTileMaps(SelectedDataSource);
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