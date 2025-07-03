using CommunityToolkit.Mvvm.ComponentModel;

namespace MapTileDownloader.Models;

// 改为分部类 + 继承 ObservableObject
public partial class TileSource : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string url;

    [ObservableProperty]
    private string userAgent;

    [ObservableProperty]
    private string host;

    [ObservableProperty]
    private string referer;

    [ObservableProperty]
    private string origin;

    [ObservableProperty]
    private string format = "JPG";
}