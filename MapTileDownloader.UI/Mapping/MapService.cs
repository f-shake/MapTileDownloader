using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BruTile.Wmts.Generated;
using MapTileDownloader.Enums;
using MapTileDownloader.Models;
using MapTileDownloader.TileSources;
using MapTileDownloader.UI.Enums;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping;

public class MapService : IMapService
{
    private MapView mapView;

    public LayerInfo[] Layers
    {
        get
        {
            CheckIsAttached();
            return mapView.Layers;
        }
    }

    public void Attach(MapView mapView)
    {
        this.mapView = mapView;
    }

    public void ClearTileGrids()
    {
        CheckIsAttached();
        mapView.ClearTileGrids();
    }

    public void DisplayPolygon(Coordinate[] coordinates)
    {
        CheckIsAttached();
        mapView.DisplayPolygon(coordinates);
    }

    public void DisplayTileGrids(int level)
    {
        CheckIsAttached();
        mapView.DisplayTileGrids(level);
    }

    public Task<Coordinate[]> DrawAsync(CancellationToken cancellationToken)
    {
        CheckIsAttached();
        return mapView.DrawAsync(cancellationToken);
    }

    public void LoadLocalTileMaps(MbtilesTileSource tileSource)
    {
        CheckIsAttached();
        mapView.LoadLocalTileMaps(tileSource);
    }

    public void LoadOnlineTileMaps(TileDataSource tileDataSource)
    {
        CheckIsAttached();
        mapView.LoadOnlineTileMaps(tileDataSource);
    }

    public Task LoadTileGridsAsync(TileDataSource tileDataSource, IEnumerable<IDownloadingLevel> levels)
    {
        CheckIsAttached();
        return mapView.LoadTileGridsAsync(tileDataSource, levels);
    }

    public void RefreshBaseTileGrid()
    {
        CheckIsAttached();
        mapView.RefreshBaseTileGrid();
    }

    public void SetEnable(PanelType type)
    {
        CheckIsAttached();
        mapView.SetEnable(type);
    }

    public void ZoomToGeometry(Geometry geometry, double growFactor)
    {
        CheckIsAttached();
        mapView.ZoomToGeometry(geometry, growFactor);
    }

    private void CheckIsAttached()
    {
        if (mapView == null)
        {
            throw new InvalidOperationException("MapView还未附加");
        }
    }
}