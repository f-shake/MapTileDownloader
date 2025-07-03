using Avalonia.Controls;
using Avalonia.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Avalonia;
using Mapsui.Extensions;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia.Extensions;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        RegisterSelectedTileSourceChanged();
        map.LoadTileMaps((DataContext as MainViewModel)!.DataSourceViewModel.SelectedSource);
    }

    private void RegisterSelectedTileSourceChanged()
    {
        (DataContext as MainViewModel)!.DataSourceViewModel.SelectedSourceChanged += (s, e) =>
        {
            map.LoadTileMaps((DataContext as MainViewModel)!.DataSourceViewModel.SelectedSource);
        };
    }
}