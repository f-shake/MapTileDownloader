namespace MapTileDownloader.Models;

public class TileDataSource : SimpleNotifyPropertyChangedBase
{
    private string name;

    public string Format { get; set; } = "JPG";

    public string Host { get; set; }

    public int MaxLevel { get; set; } = 20;

    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }

    public string Origin { get; set; }

    public string Referer { get; set; }

    public string Url { get; set; }

    public string UserAgent { get; set; }
}