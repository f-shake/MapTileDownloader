using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MapTileDownloader.Models;
using MapTileDownloader.UI.Messages;
using MapTileDownloader.UI.Views;
using Mapsui;

namespace MapTileDownloader.UI.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
    }
}
