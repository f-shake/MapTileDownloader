using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Messages;

public class DisplayPolygonOnMapMessage(Coordinate[] coordinates)
{
    public Coordinate[] Coordinates { get; } = coordinates;
}