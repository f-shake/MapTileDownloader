﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using MapTileDownloader.UI.Messages;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using MapTileDownloader.Services;

namespace MapTileDownloader.UI.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isInitialized;

    protected IMapService Map => SendMessage(new GetMapServiceMessage()).MapService;

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


    public async Task TryWithLoadingAsync(Func<Task> func, string errorTitle = "错误")
    {
        SendMessage(new LoadingMessage(true));
        try
        {
            await func();
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

    public async Task TryWithTabDisabledAsync(Func<Task> func, string errorTitle = "错误")
    {
        SendMessage(new TabEnableMessage(false));
        try
        {
            await func();
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(errorTitle, ex);
        }
        finally
        {
            SendMessage(new TabEnableMessage(true));
        }
    }
}