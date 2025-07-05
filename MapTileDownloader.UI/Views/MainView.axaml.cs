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
using CommunityToolkit.Mvvm.Messaging;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia.Extensions;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        RegisterMessages();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        map.LoadTileMaps((DataContext as MainViewModel)!.DataSourceViewModel.SelectedDataSource);
    }

    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<UpdateTileSourceMessage>(this,
            (o, m) => { map.LoadTileMaps(m.TileDataSource); });
        WeakReferenceMessenger.Default.Register<SelectOnMapMessage>(this,
            (o, m) => { m.Task = map.DrawAsync(m.CancellationToken); });
        WeakReferenceMessenger.Default.Register<DisplayPolygonOnMapMessage>(this,
            (o, m) => { map.DisplayPolygon(m.Coordinates); });
        WeakReferenceMessenger.Default.Register<DisplayTilesOnMapMessage>(this,
            (o, m) => { map.DisplayTiles((DataContext as MainViewModel).DataSourceViewModel.SelectedDataSource,m.Tiles); });
        WeakReferenceMessenger.Default.Register<GetSelectedDataSourceMessage>(this,
            (o, m) => { m.DataSource = (DataContext as MainViewModel).DataSourceViewModel.SelectedDataSource; });
    }
}