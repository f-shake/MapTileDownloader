namespace MapTileDownloader.Models;

public class TileSource
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string UserAgent { get; set; }
    public string Host { get; set; }
    public string Referer { get; set; }
    public string Origin { get; set; }
    public string Format { get; set; }
}