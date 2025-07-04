using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;

namespace MapTileDownloader.UI.ViewModels;

public class ViewModelBase : ObservableObject
{
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

    public Task ShowOkAsync(string title, string message)
    {
        return SendMessage(new CommonDialogMessage()
        {
            Type = CommonDialogMessage.CommonDialogType.Ok,
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
}