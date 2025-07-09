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
        if (Configs.Instance.SelectedTileSourcesIndex >= 0 && Configs.Instance.SelectedTileSourcesIndex < Sources.Count)
        {
            SelectedDataSource = Sources[Configs.Instance.SelectedTileSourcesIndex];
        }
    }

    [RelayCommand]
    private void AddSource()
    {
        Sources.Add(new TileDataSource() { Name = "新数据源" });
        SelectedDataSource = Sources[^1];
    }

    [RelayCommand]
    private void CallSelectedSourceChanged()
    {
        OnSelectedDataSourceChanged(SelectedDataSource);
    }

    partial void OnSelectedDataSourceChanged(TileDataSource value)
    {
        Map.LoadTileMaps(value);
        Configs.Instance.SelectedTileSourcesIndex = Sources.IndexOf(value);
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