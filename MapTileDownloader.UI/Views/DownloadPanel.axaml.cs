﻿using Avalonia.Controls;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;
public partial class DownloadPanel : UserControl
{
    public DownloadPanel(DownloadViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}