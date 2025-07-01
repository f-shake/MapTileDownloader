using Mapsui;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapTileDownloader.UI.Mapping;

public static class MapsuiExtension
{
    public static LinearRing ToClosedLinearRing(this IList<MPoint> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count <= 2)
        {
            throw new ArgumentException("提供的点数量应不少于3个", nameof(points));
        }
        return new LinearRing([.. points.Select(ToCoordinate).Append(points[0].ToCoordinate())]);
    }

    public static Coordinate ToCoordinate(this MPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);
        return new Coordinate(point.X, point.Y);
    }

    public static Point ToPoint(this MPoint point)
    {
        return new Point(point.X, point.Y);
    }
}
