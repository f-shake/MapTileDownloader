using BruTile.Predefined;
using BruTile.Web;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using MapTileDownloader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using BruTile;
using MapTileDownloader.Enums;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Pen = Mapsui.Styles.Pen;
using BruTile.Wms;
using Mapsui.Nts;
using MapTileDownloader.Services;
using MapTileDownloader.TileSources;
using MapTileDownloader.UI.Enums;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    /// <summary>
    /// 切片网格
    /// </summary>
    private LayerInfo overlayTileGridLayer;

    /// <summary>
    /// 绘制和范围图层
    /// </summary>
    private LayerInfo drawingLayer;

    /// <summary>
    /// 本地底图
    /// </summary>
    private LayerInfo localBaseLayer;

    /// <summary>
    /// 绘制时鼠标位置图层
    /// </summary>
    private LayerInfo mousePositionLayer;

    /// <summary>
    /// 在线底图
    /// </summary>
    private LayerInfo onlineBaseLayer;

    /// <summary>
    /// 瓦片网格图层
    /// </summary>
    private LayerInfo downloadingTileGridLayer;

    /// <summary>
    ///  本地瓦片图层的范围指示
    /// </summary>
    private LayerInfo localExtentLayer;

    public LayerInfo[] Layers { get; private set; }

    private TileLayer GetEmptyTileLayer()
    {
        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, 20),
            "http://localhost/{x}/{y}/{z}"
        );
   return new TileLayer(s);
    }
    
    public void LoadLocalTileMaps(MbtilesTileSource tileSource, MbtilesInfo mbtilesInfo)
    {
        if (tileSource == null)
        {
            localBaseLayer.Replace(GetEmptyTileLayer());
            localExtentLayer.Features = [];
            return;
        }
        var layer = new TileLayer(tileSource)
        {
            Name = nameof(localBaseLayer),
        };
        localBaseLayer.Replace(layer);

        if (mbtilesInfo == null)
        {
            localExtentLayer.Features = [];
            return;
        }

        var (x1, x2, y1, y2) =
            (mbtilesInfo.MinLongitude, mbtilesInfo.MaxLongitude, mbtilesInfo.MinLatitude, mbtilesInfo.MaxLatitude);

        if (x2 - x1 <= 0 || y2 - y1 <= 0)
        {
            localExtentLayer.Features = [];
            return;
        }

        (x1, y1) = CoordinateSystemUtility.Wgs84ToWebMercator.MathTransform.Transform(x1, y1);
        (x2, y2) = CoordinateSystemUtility.Wgs84ToWebMercator.MathTransform.Transform(x2, y2);

        var rect = new Polygon(new LinearRing([
            new Coordinate(x1, y1),
            new Coordinate(x1, y2),
            new Coordinate(x2, y2),
            new Coordinate(x2, y1),
            new Coordinate(x1, y1)
        ]));
        var feature = new GeometryFeature(rect);
        localExtentLayer.Features = [feature];
        Refresh();
    }

    public void LoadOnlineTileMaps(TileDataSource tileDataSource)
    {
        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, tileDataSource.MaxLevel),
            tileDataSource.Url,
            configureHttpRequestMessage: s =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(tileDataSource.UserAgent))
                    {
                        s.Headers.UserAgent.ParseAdd(tileDataSource.UserAgent);
                    }

                    if (!string.IsNullOrWhiteSpace(tileDataSource.Host))
                    {
                        s.Headers.Host = tileDataSource.Host;
                    }

                    if (!string.IsNullOrWhiteSpace(tileDataSource.Origin))
                    {
                        s.Headers.Add("Origin", tileDataSource.Origin);
                    }

                    if (!string.IsNullOrWhiteSpace(tileDataSource.Referer))
                    {
                        s.Headers.Referrer = new Uri(tileDataSource.Referer);
                    }
                }
                catch
                {
                }
            }
        );


        var layer = new TileLayer(s) { Name = nameof(BaseLayer) };
        onlineBaseLayer.Replace(layer);
    }

    public void RefreshBaseTileGrid()
    {
        LayerInfo currentLayer = null;
        if (onlineBaseLayer.IsVisible)
        {
            currentLayer = onlineBaseLayer;
        }
        else if (localBaseLayer.IsVisible)
        {
            currentLayer = localBaseLayer;
        }
        else
        {
            overlayTileGridLayer.IsVisible = false;
            return;
        }


        var s = new TileGridSource((currentLayer.Layer as TileLayer).TileSource.Schema);
        overlayTileGridLayer.Replace(new TileLayer(s));
    }

    public void SetVisible(PanelType type)
    {
        onlineBaseLayer.IsVisible = type == PanelType.Online;
        localBaseLayer.IsVisible = type == PanelType.Local;
        downloadingTileGridLayer.IsVisible = type == PanelType.Online;
        localExtentLayer.IsVisible = type == PanelType.Local;

        RefreshBaseTileGrid();
        Refresh();
    }

    private TileLayer GetBaseTileGridLayer()
    {
        var s = new TileGridSource(new GlobalSphericalMercator(YAxis.OSM));
        return new TileLayer(s) { Name = nameof(overlayTileGridLayer) };
    }

    private MemoryLayer GetDrawingLayer()
    {
        return new MemoryLayer
        {
            Name = nameof(drawingLayer),
            Style = new VectorStyle
            {
                Fill = new Brush(Color.FromArgb(32, 255, 255, 255)),
                Outline = new Pen(Color.Red, 2),
                Line = new Pen(Color.Red, 2),
            }
        };
    }

    private MemoryLayer GetMousePositionLayer()
    {
        return new MemoryLayer
        {
            Name = nameof(mousePositionLayer),
            Style = new SymbolStyle
            {
                SymbolType = SymbolType.Rectangle,
                Fill = new Brush(Color.White),
                Outline = new Pen(Color.Red, 4),
                SymbolScale = 0.2,
            },
        };
    }

    private MemoryLayer GetLocalExtentLayer()
    {
        return new MemoryLayer
        {
            Name = nameof(localExtentLayer),
            Style = new VectorStyle
            {
                Fill = new Brush(Color.Transparent),
                Outline = new Pen(Color.Yellow, 2),
            }
        };
    }

    private MemoryLayer GetOverlayTileGridLayer()
    {
        return new MemoryLayer
        {
            Name = nameof(downloadingTileGridLayer),
            Style = new VectorStyle
            {
                Fill = new Brush(Color.Transparent),
                Outline = new Pen(Color.Gray, 2),
                Line = new Pen(Color.Gray, 2),
                Opacity = 0.33f
            },
        };
    }


    private void InitializeLayers()
    {
        onlineBaseLayer = LayerInfo.CreateAndInsert("在线底图", Map.Layers,GetEmptyTileLayer());
        localBaseLayer = LayerInfo.CreateAndInsert("本地底图", Map.Layers,GetEmptyTileLayer());
        overlayTileGridLayer = LayerInfo.CreateAndInsert("切片网格（显示）", Map.Layers, GetBaseTileGridLayer());
        downloadingTileGridLayer = LayerInfo.CreateAndInsert("切片网格（下载）", Map.Layers, GetOverlayTileGridLayer());
        localExtentLayer = LayerInfo.CreateAndInsert("本地底图范围", Map.Layers, GetLocalExtentLayer());
        drawingLayer = LayerInfo.CreateAndInsert("绘制范围", Map.Layers, GetDrawingLayer());
        mousePositionLayer = LayerInfo.CreateAndInsert("鼠标位置", Map.Layers, GetMousePositionLayer());

        Layers =
        [
            localExtentLayer,
            downloadingTileGridLayer,
            overlayTileGridLayer,
            localBaseLayer,
            onlineBaseLayer
        ];
    }
}