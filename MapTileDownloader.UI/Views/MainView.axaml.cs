using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using MapTileDownloader.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;

namespace MapTileDownloader.UI.Views;

public class MainViewControl : IMainViewControl
{
    public void SetLoadingVisible(bool isVisible)
    {
         App.Services.GetRequiredService<MainView>().SetLoadingVisible(isVisible);
    }

    public void SetTabSelectable(bool isEnabled)
    {
        App.Services.GetRequiredService<MainView>().SetTabSelectable(isEnabled);
    }
}

public partial class MainView : UserControl
{
    private CancellationTokenSource loadingToken = null;

    public MainView(IDialogService dialog,MainViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Dialog = dialog;
    }

    public IDialogService Dialog { get; }

    public void SetLoadingVisible(bool isVisible)
    {
        (DataContext as MainViewModel).IsProgressRingVisible = isVisible;
    }

    public void SetTabSelectable(bool isEnabled)
    {
        if (isEnabled)
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
            await Dialog.ShowErrorDialogAsync("初始化失败", ex);
        }
    }
}