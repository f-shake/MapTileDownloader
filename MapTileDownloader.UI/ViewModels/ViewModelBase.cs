using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using MapTileDownloader.UI.Services;
using MapTileDownloader.UI.Views;

namespace MapTileDownloader.UI.ViewModels;

public abstract partial class ViewModelBase(IMapService mapService, 
    IMainViewService mainView,
    IDialogService dialog,
    IStorageProviderService storage) 
    : ObservableObject
{
    [ObservableProperty]
    private bool isInitialized;

    public IDialogService Dialog { get; } = dialog;
    
    public IStorageProviderService Storage { get; } = storage;

    public IMainViewService MainView { get; } = mainView;

    public IMapService Map { get; } = mapService;

    public virtual ValueTask InitializeAsync()
    {
        Debug.Assert(IsInitialized == false);
        IsInitialized = true;
        return ValueTask.CompletedTask;
    }

    public TMessage SendMessage<TMessage>(TMessage message) where TMessage : class
    {
        return WeakReferenceMessenger.Default.Send(message);
    }

    public async Task TryWithLoadingAsync(Func<Task> func, string errorTitle = "错误")
    {
        MainView.SetLoadingVisible(true);
        try
        {
            await func();
        }
        catch (Exception ex)
        {
            await Dialog.ShowErrorDialogAsync(errorTitle, ex);
        }
        finally
        {
            MainView.SetLoadingVisible(false);
        }
    }

    public async Task TryWithTabDisabledAsync(Func<Task> func, string errorTitle = "错误")
    {
        MainView.SetTabSelectable(false);
        try
        {
            await func();
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception ex)
        {
            await Dialog.ShowErrorDialogAsync(errorTitle, ex);
        }
        finally
        {
            MainView.SetTabSelectable(true);
        }
    }
}