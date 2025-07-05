using BruTile;
using BruTile.Predefined;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace MapTileDownloader.Helpers;

public class TileHelper(TileDataSource tileDataSource)
{
    public TileDataSource TileDataSource { get; } = tileDataSource;

    public GlobalSphericalMercator TileSchema { get; } =
        new GlobalSphericalMercator(tileDataSource.InverseYAxis ? YAxis.TMS : YAxis.OSM, 0, tileDataSource.MaxLevel);


    /// <summary>
    /// 获取指定坐标构成的多边形在指定瓦片级别下相交的瓦片索引集合（坐标为 EPSG:3857）。
    /// </summary>
    /// <param name="polygonCoordinates3857">构成多边形的坐标点（首尾应相连）</param>
    /// <param name="zoomLevel">瓦片级别</param>
    /// <returns>相交的 TileIndex 列表</returns>
    public List<TileIndex> GetIntersectingTiles(Coordinate[] polygonCoordinates3857, int zoomLevel)
    {
        var geometryFactory = new GeometryFactory();
        var polygon3857 = geometryFactory.CreatePolygon(polygonCoordinates3857);

        var envelope = polygon3857.EnvelopeInternal;

        double resolution = TileSchema.Resolutions[zoomLevel].UnitsPerPixel;
        int tileSize = TileSchema.GetTileWidth(zoomLevel); // 通常是 256

        int minCol = (int)Math.Floor((envelope.MinX - TileSchema.OriginX) / (tileSize * resolution));
        int maxCol = (int)Math.Floor((envelope.MaxX - TileSchema.OriginX) / (tileSize * resolution));
        int minRow = (int)Math.Floor((envelope.MinY - TileSchema.OriginY) / (tileSize * resolution));
        int maxRow = (int)Math.Floor((envelope.MaxY - TileSchema.OriginY) / (tileSize * resolution));

        var preparedPolygon = PreparedGeometryFactory.Prepare(polygon3857);
        var tiles = new List<TileIndex>();

        for (int col = minCol; col <= maxCol; col++)
        {
            for (int row = minRow; row <= maxRow; row++)
            {
                double minX = TileSchema.OriginX + col * tileSize * resolution;
                double maxX = TileSchema.OriginX + (col + 1) * tileSize * resolution;
                double minY = TileSchema.OriginY + row * tileSize * resolution;
                double maxY = TileSchema.OriginY + (row + 1) * tileSize * resolution;

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
    
    public  Polygon GetTilePolygon(TileIndex tile)
    {
        var resolution = TileSchema.Resolutions[tile.Level].UnitsPerPixel;
        int tileSize = TileSchema.GetTileWidth(tile.Level);

        double minX = TileSchema.OriginX + tile.Col * tileSize * resolution;
        double maxX = minX + tileSize * resolution;
        double minY = TileSchema.OriginY + tile.Row * tileSize * resolution;
        double maxY = minY + tileSize * resolution;

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
}