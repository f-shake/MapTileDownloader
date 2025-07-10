namespace MapTileDownloader.UI.Messages;

public class TabEnableMessage(bool enable)
{
    public bool Enabled { get; set; } = enable;
}