using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Dialogs;
using MapTileDownloader.UI.ViewModels;
using System;
using System.Linq;
using System.Threading;
using MapTileDownloader.UI.Services;

namespace MapTileDownloader.UI.Views;

public partial class MainView : UserControl
{
    public MainView(IDialogService dialog,MainViewModel vm,IMainViewService mainViewService)
    {
        mainViewService.Attach(this);
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