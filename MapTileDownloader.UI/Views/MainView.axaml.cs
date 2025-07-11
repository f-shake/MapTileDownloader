﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Messages;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.ViewModels;
using System;
using System.Linq;
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

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        try
        {
            await (DataContext as MainViewModel).InitializeAsync();
        }
        catch (Exception ex)
        {
            await (DataContext as MainViewModel).ShowErrorAsync("初始化失败", ex);
        }
    }

    private void RegisterMessages()
    {
        this.RegisterCommonDialogMessage();
        this.RegisterDialogHostMessage();
        this.RegisterGetClipboardMessage();
        this.RegisterGetStorageProviderMessage();
        WeakReferenceMessenger.Default.Register<GetMapServiceMessage>(this,
            (o, m) => { m.MapService = map; });
        WeakReferenceMessenger.Default.Register<TabEnableMessage>(this,
            (o, m) =>
            {
                if (m.Enabled)
                {
                    foreach (var ti in tab.Items.Cast<TabItem>())
                    {
                        ti.IsEnabled = true;
                    }
                }
                else
                {
                    foreach (var ti in tab.Items.Cast<TabItem>())
                    {
                        ti.IsEnabled = ti == tab.SelectedItem;
                    }
                }
            });
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