using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Messages;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.ViewModels;
using System;
using System.Threading;

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