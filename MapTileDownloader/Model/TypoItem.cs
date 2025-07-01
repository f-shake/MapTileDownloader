namespace MapTileDownloader.Models;
public class TypoItem : ICheckItem
{
    public string Context { get; set; }
    public string Original { get; set; }
    public string Corrected { get; set; }
    public string FixedSegment { get; set; }
    public string Explanation { get; set; }
}
