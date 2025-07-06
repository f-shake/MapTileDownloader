using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping;

public interface IMapService
{
    void ZoomToGeometry(Geometry geometry, double growFactor);
    void DisplayPolygon(Coordinate[] coordinates);
    Task<Coordinate[]> DrawAsync(CancellationToken cancellationToken);
    Task LoadTileGridsAsync(TileDataSource tileDataSource, IEnumerable<IDownloadingLevel> levels);
    void DisplayTileGrids(int level);
}