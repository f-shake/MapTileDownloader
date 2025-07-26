using Avalonia.Controls;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class LocalToolsPanel : UserControl
{
    public LocalToolsPanel(LocalToolsViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}