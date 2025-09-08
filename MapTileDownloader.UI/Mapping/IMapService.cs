using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Enums;
using MapTileDownloader.Models;
using MapTileDownloader.TileSources;
using MapTileDownloader.UI.Enums;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping;

public interface IMapService
{
    LayerInfo[] Layers { get; }

    void ClearTileGrids();

    void DisplayPolygon(Coordinate[] coordinates);

    void DisplayTileGrids(int level);

    Task<Coordinate[]> DrawAsync(CancellationToken cancellationToken);

    void LoadLocalTileMaps(MbtilesTileSource tileSource, MbtilesInfo mbtilesInfo);

    void LoadOnlineTileMaps(TileDataSource tileDataSource);

    Task LoadTileGridsAsync(TileDataSource tileDataSource, IEnumerable<IDownloadingLevel> levels);

    void RefreshBaseTileGrid();

    void SetEnable(PanelType type);

    void ZoomToGeometry(Geometry geometry, double growFactor);
}