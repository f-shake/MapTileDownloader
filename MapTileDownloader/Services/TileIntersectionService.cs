using BruTile;
using BruTile.Predefined;
using MapTileDownloader.Extensions;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using ProjNet.CoordinateSystems.Transformations;
using ProjNet.CoordinateSystems;
using System.Xml.Linq;

namespace MapTileDownloader.Services;

public class TileIntersectionService(bool useTms)
{
    public GlobalSphericalMercator TileSchema { get; } = new GlobalSphericalMercator(useTms ? YAxis.TMS : YAxis.OSM);

    public bool UseTms { get; } = useTms;

    public long EstimateIntersectingTileCount(Coordinate[] polygonCoordinates3857, int zoomLevel)
    {
        var geometryFactory = new GeometryFactory();
        var polygon3857 = geometryFactory.CreatePolygon(polygonCoordinates3857.ToClosed().ToArray());
        var envelope = polygon3857.EnvelopeInternal;

        double resolution = TileSchema.Resolutions[zoomLevel].UnitsPerPixel;
        long tileSize = TileSchema.GetTileWidth(zoomLevel); // 通常是 256

        // 计算外包矩形覆盖的瓦片行列范围（使用 long 类型）
        long minCol = (long)Math.Floor((envelope.MinX - TileSchema.OriginX) / (tileSize * resolution));
        long maxCol = (long)Math.Floor((envelope.MaxX - TileSchema.OriginX) / (tileSize * resolution));
        int minRow =
            (int)Math.Floor((TileSchema.OriginY - envelope.MaxY) / (tileSize * resolution));
        int maxRow = (int)Math.Floor((TileSchema.OriginY - envelope.MinY) / (tileSize * resolution));

        // 边界检查：确保行列号非负（假设瓦片坐标系从0开始）
        minCol = Math.Max(minCol, 0);
        minRow = Math.Max(minRow, 0);

        checked
        {
            try
            {
                return (maxCol - minCol + 1) * (maxRow - minRow + 1);
            }
            catch (OverflowException)
            {
                // 处理极端情况（如全球范围+高缩放级别）
                return long.MaxValue; // 或抛出异常/返回保守估计值
            }
        }
    }

    public List<TileIndex> GetIntersectingTiles(Coordinate[] polygonCoordinates3857, int zoomLevel)
    {
        var geometryFactory = new GeometryFactory();
        var polygon3857 = geometryFactory.CreatePolygon(polygonCoordinates3857.ToClosed().ToArray());
        double resolution = TileSchema.Resolutions[zoomLevel].UnitsPerPixel;
        int tileSize = TileSchema.GetTileWidth(zoomLevel);

        (int minRow, int maxRow, int minCol, int maxCol) = GetTileRange(polygon3857, zoomLevel);

        var preparedPolygon = PreparedGeometryFactory.Prepare(polygon3857);
        var tiles = new List<TileIndex>();

        for (int col = minCol; col <= maxCol; col++)
        {
            for (int row = minRow; row <= maxRow; row++)
            {
                double minX = TileSchema.OriginX + col * tileSize * resolution;
                double maxX = TileSchema.OriginX + (col + 1) * tileSize * resolution;
                double minY = TileSchema.OriginY - row * tileSize * resolution;
                double maxY = TileSchema.OriginY - (row + 1) * tileSize * resolution;

                var tilePolygon = new Polygon(new LinearRing([
                    new Coordinate(minX, minY),
                    new Coordinate(minX, maxY),
                    new Coordinate(maxX, maxY),
                    new Coordinate(maxX, minY),
                    new Coordinate(minX, minY)
                ]));

                if (preparedPolygon.Intersects(tilePolygon))
                {
                    tiles.Add(new TileIndex(col, row, zoomLevel));
                }
            }
        }

        return tiles;
    }

    public Polygon GetTilePolygon(TileIndex tile)
    {
        var resolution = TileSchema.Resolutions[tile.Level].UnitsPerPixel;
        int tileSize = TileSchema.GetTileWidth(tile.Level);

        double minX = TileSchema.OriginX + tile.Col * tileSize * resolution;
        double maxX = minX + tileSize * resolution;
        double maxY = TileSchema.OriginY - tile.Row * tileSize * resolution;
        double minY = maxY - tileSize * resolution;

        var coordinates = new[]
        {
            new Coordinate(minX, minY),
            new Coordinate(minX, maxY),
            new Coordinate(maxX, maxY),
            new Coordinate(maxX, minY),
            new Coordinate(minX, minY)
        };

        var geometryFactory = new GeometryFactory();
        return geometryFactory.CreatePolygon(coordinates);
    }

    public (int minRow, int maxRow, int minColumn, int maxColumn) GetTileRange(Coordinate[] coordinates, int zoomLevel)
    {
        var geometryFactory = new GeometryFactory();
        var polygon3857 = geometryFactory.CreatePolygon(coordinates.ToClosed().ToArray());

        return GetTileRange(polygon3857, zoomLevel);
    }

    public Extent GetWorldExtent(int zoomLevel, int minX, int minY, int maxX, int maxY)
    {
        var extent3857 = TileTransform.TileToWorld(new TileRange(minX, minY, maxX - minX + 1, maxY - minY + 1),
            zoomLevel, TileSchema);
        // 创建Web墨卡托坐标系
        var webMercator = ProjectedCoordinateSystem.WebMercator;

        // 创建WGS84地理坐标系
        var wgs84 = GeographicCoordinateSystem.WGS84;

        // 创建坐标转换
        var transform = new CoordinateTransformationFactory().CreateFromCoordinateSystems(webMercator, wgs84);
        var westSouth = transform.MathTransform.Transform(extent3857.MinX, extent3857.MinY);
        var eastNorth = transform.MathTransform.Transform(extent3857.MaxX, extent3857.MaxY);
        return new Extent(westSouth.x, westSouth.y, eastNorth.x, eastNorth.y);
    }

    private (int minRow, int maxRow, int minColumn, int maxColumn) GetTileRange(Polygon polygon3857, int zoomLevel)
    {
        var envelope = polygon3857.EnvelopeInternal;

        double resolution = TileSchema.Resolutions[zoomLevel].UnitsPerPixel;
        int tileSize = TileSchema.GetTileWidth(zoomLevel); // 通常是 256

        int minCol = (int)Math.Floor((envelope.MinX - TileSchema.OriginX) / (tileSize * resolution));
        int maxCol = (int)Math.Floor((envelope.MaxX - TileSchema.OriginX) / (tileSize * resolution));

        int minRow = 0;
        int maxRow = 0;

        if (UseTms)
        {
            minRow = Math.Abs((int)Math.Floor((envelope.MinY - TileSchema.OriginY) / (tileSize * resolution)));
            maxRow = Math.Abs((int)Math.Floor((envelope.MaxY - TileSchema.OriginY) / (tileSize * resolution)));
        }
        else
        {
            maxRow = Math.Abs((int)Math.Floor((TileSchema.OriginY - envelope.MinY) / (tileSize * resolution)));
            minRow = Math.Abs((int)Math.Floor((TileSchema.OriginY - envelope.MaxY) / (tileSize * resolution)));
        }

        return (minRow, maxRow, minCol, maxCol);
    }
}