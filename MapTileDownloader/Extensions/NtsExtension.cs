using NetTopologySuite.Geometries;

namespace MapTileDownloader.Extensions;

public static class NtsExtension
{
    public static IEnumerable<Coordinate> ToClosed(this IList<Coordinate> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count <= 2)
        {
            throw new ArgumentException("提供的点数量应不少于3个", nameof(points));
        }

        return points[0].Equals(points[^1]) ? points : points.Append(new Coordinate(points[0].X, points[0].Y));
    }
}