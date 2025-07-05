using System;
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
using System.Threading;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BruTile.Predefined;
using BruTile.Web;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Messages;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia.Extensions;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class MainView : UserControl
{
    private CancellationTokenSource loadingToken = null;

    public MainView()
    {
        InitializeComponent();
        RegisterMessages();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        map.LoadTileMaps((DataContext as MainViewModel)!.DataSourceViewModel.SelectedDataSource);
        (DataContext as MainViewModel)!.Initialize();
    }

    private void RegisterMessages()
    {
        this.RegisterCommonDialogMessage();
        this.RegisterDialogHostMessage();
        this.RegisterGetClipboardMessage();
        this.RegisterGetStorageProviderMessage();
        WeakReferenceMessenger.Default.Register<UpdateTileSourceMessage>(this,
            (o, m) => { map.LoadTileMaps(m.TileDataSource); });
        WeakReferenceMessenger.Default.Register<SelectOnMapMessage>(this,
            (o, m) => { m.Task = map.DrawAsync(m.CancellationToken); });
        WeakReferenceMessenger.Default.Register<DisplayPolygonOnMapMessage>(this,
            (o, m) => { map.DisplayPolygon(m.Coordinates); });
        WeakReferenceMessenger.Default.Register<DisplayTilesOnMapMessage>(this,
            async (o, m) =>
            {
                await map.DisplayTilesAsync((DataContext as MainViewModel).DataSourceViewModel.SelectedDataSource,
                    m.Tiles);
            });
        WeakReferenceMessenger.Default.Register<GetSelectedDataSourceMessage>(this,
            (o, m) => { m.DataSource = (DataContext as MainViewModel).DataSourceViewModel.SelectedDataSource; });
        WeakReferenceMessenger.Default.Register<LoadingMessage>(this, (o, m) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                if (m.IsVisible && o is Visual v)
                {
                    try
                    {
                        loadingToken ??= LoadingOverlay.ShowLoading(v);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                    if (loadingToken != null)
                    {
                        loadingToken.Cancel();
                        loadingToken = null;
                    }
                }
            });
        });
    }
}