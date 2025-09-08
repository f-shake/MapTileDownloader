using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FzLib.Avalonia.Controls;
using MapTileDownloader.Models;
using MapTileDownloader.Services;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Services;
using MapTileDownloader.UI.Enums;
using MapTileDownloader.UI.Mapping;
using MapTileDownloader.UI.Views;

namespace MapTileDownloader.UI.ViewModels;

public abstract partial class ViewModelBase(
    IMapService mapService,
    IProgressOverlayService progressOverlay,
    IDialogService dialog,
    IStorageProviderService storage)
    : ObservableObject
{
    [ObservableProperty]
    private bool isInitialized;

    protected static event EventHandler BeginOperation;

    protected static event EventHandler EndOperation;

    protected static PanelType CurrentPanelType { get; set; } = PanelType.Online;
    protected IDialogService Dialog { get; } = dialog;

    protected IMapService Map { get; } = mapService;
    protected IProgressOverlayService ProgressOverlay { get; } = progressOverlay;

    protected IStorageProviderService Storage { get; } = storage;
    public virtual Task InitializeAsync()
    {
        Debug.Assert(IsInitialized == false);
        IsInitialized = true;
        return Task.CompletedTask;
    }
    protected async Task TryDoingAsync(Func<Task> func, string errorTitle = "错误")
    {
        BeginOperation?.Invoke(this, EventArgs.Empty);
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
            EndOperation?.Invoke(this, EventArgs.Empty);
        }
    }
}