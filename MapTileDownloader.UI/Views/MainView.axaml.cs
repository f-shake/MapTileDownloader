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
using FzLib.Avalonia.Controls;
using MapTileDownloader.UI.Mapping;
using Microsoft.Extensions.DependencyInjection;

namespace MapTileDownloader.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        App.Services.GetRequiredService<IProgressOverlayService>().Attach(ring);
        ((MapService)App.Services.GetRequiredService<IMapService>()).Attach(map);
    }
}