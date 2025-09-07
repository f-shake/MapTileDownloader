using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapTileDownloader.Enums;
using MapTileDownloader.Models;
using MapTileDownloader.TileSources;

using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping;

public class MapService : IMapService
{
    private MapView mapView;

    public void Attach(MapView mapView)
    {
        this.mapView = mapView;
    }

    private void CheckIsAttached()
    {
        if (mapView == null)
        {
            throw new InvalidOperationException("MapView还未附加");
        }
    }

    public void ZoomToGeometry(Geometry geometry, double growFactor)
    {
        CheckIsAttached();
        mapView.ZoomToGeometry(geometry, growFactor);
    }

    public void DisplayPolygon(Coordinate[] coordinates)
    {
        CheckIsAttached();
        mapView.DisplayPolygon(coordinates);
    }

    public Task<Coordinate[]> DrawAsync(CancellationToken cancellationToken)
    {
        CheckIsAttached();
        return mapView.DrawAsync(cancellationToken);
    }

    public Task LoadTileGridsAsync(TileDataSource tileDataSource, IEnumerable<IDownloadingLevel> levels)
    {
        CheckIsAttached();
        return mapView.LoadTileGridsAsync(tileDataSource, levels);
    }

    public void DisplayTileGrids(int level)
    {
        CheckIsAttached();
        mapView.DisplayTileGrids(level);
    }

    public void LoadTileMaps(TileDataSource tileDataSource)
    {
        CheckIsAttached();
        mapView.LoadTileMaps(tileDataSource);
    }

    public void LoadLocalTileMaps(MbtilesTileSource tileSource)
    {
        CheckIsAttached();
        mapView.LoadLocalTileMaps(tileSource);
    }

    public void SetEnable(AppLayer layer)
    {
        CheckIsAttached();
        mapView.SetEnable(layer);
    }

    public void ClearTileGrids()
    {
        CheckIsAttached();
        mapView.ClearTileGrids();
    }

    public void RefreshBaseTileGrid()
    {
        CheckIsAttached();
        mapView.RefreshBaseTileGrid();
    }
}