using Avalonia.Controls;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class MbtilesPicker : UserControl
{
    public MbtilesPicker(MbtilesPickerViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}