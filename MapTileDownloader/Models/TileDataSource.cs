namespace MapTileDownloader.Models;

public partial class TileDataSource : SimpleNotifyPropertyChangedBase
{
    private string name;

    public string Name
    {
        get => name;
        set => SetField(ref name, value);
    }

    public string Url { get; set; }

    public string UserAgent { get; set; }

    public string Host { get; set; }

    public string Referer { get; set; }

    public string Origin { get; set; }

    public string Format { get; set; } = "JPG";

    public bool InverseYAxis { get; set; }

    public int MaxLevel { get; set; } = 20;
}