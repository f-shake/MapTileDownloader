﻿using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using MapTileDownloader.Enums;

namespace MapTileDownloader.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int selectedTabIndex;

    public DownloadViewModel DownloadViewModel { get; } = new DownloadViewModel();

    public LocalToolsViewModel LocalToolsViewModel { get; } = new LocalToolsViewModel();

    public override async ValueTask InitializeAsync()
    {
        await DownloadViewModel.InitializeAsync();
        await LocalToolsViewModel.InitializeAsync();
        Map.LoadTileMaps(DownloadViewModel.SelectedDataSource);
        if (Configs.Instance.Coordinates != null && Configs.Instance.Coordinates.Length >= 3)
        {
            Map.DisplayPolygon(Configs.Instance.Coordinates);
        }

        await base.InitializeAsync();
    }

    partial void OnSelectedTabIndexChanged(int value)
    {
        Map.SetEnable(AppLayer.BaseLayer, value is 0);
        Map.SetEnable(AppLayer.LocalBaseLayer, value is 1);
        Map.SetEnable(AppLayer.TileGridLayer, value is 0);
        Map.SetEnable(AppLayer.DrawingLayer, true);
        Map.Refresh();
    }
}