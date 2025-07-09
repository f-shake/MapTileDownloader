using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using MapTileDownloader.UI.Messages;
using System;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using MapTileDownloader.Services;

namespace MapTileDownloader.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isInitialized;
    
    protected IMapService Map => SendMessage(new GetMapServiceMessage()).MapService;
    
    protected TileDataSource TileSource => SendMessage(new GetSelectedDataSourceMessage()).DataSource;


    public virtual void Initialize()
    {
        IsInitialized = true;
    }

    public TMessage SendMessage<TMessage>(TMessage message) where TMessage : class
    {
        return WeakReferenceMessenger.Default.Send(message);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return SendMessage(new CommonDialogMessage()
        {
            Type = CommonDialogMessage.CommonDialogType.Error,
            Title = title,
            Message = message
        }).Task;
    }

    public Task ShowErrorAsync(string title, Exception exception)
    {
        return SendMessage(new CommonDialogMessage()
        {
            Type = CommonDialogMessage.CommonDialogType.Error,
            Title = title,
            Exception = exception
        }).Task;
    }

    public Task ShowOkAsync(string title, string message)
    {
        return SendMessage(new CommonDialogMessage()
        {
            Type = CommonDialogMessage.CommonDialogType.Ok,
            Title = title,
            Message = message
        }).Task;
    }
    public async Task TryWithLoadingAsync(Task task, string errorTitle = "错误")
    {
        SendMessage(new LoadingMessage(true));
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(errorTitle, ex);
        }
        finally
        {
            SendMessage(new LoadingMessage(false));
        }
    }
}