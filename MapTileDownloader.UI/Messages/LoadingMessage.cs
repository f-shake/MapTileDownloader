namespace MapTileDownloader.UI.Messages;

public class LoadingMessage(bool isVisible)
{
    public bool IsVisible { get; set; } = isVisible;
}
