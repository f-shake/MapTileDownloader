using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Extensions;
using NetTopologySuite.Geometries;
using Mapsui.UI.Avalonia.Extensions;
using Avalonia.Interactivity;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;
using SkiaSharp;
using System.Threading;
using System.Diagnostics;
using Avalonia.Media;
using MapTileDownloader.Models;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Geometry = NetTopologySuite.Geometries.Geometry;
using Pen = Mapsui.Styles.Pen;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    private MemoryLayer drawingLayer;
    private MemoryLayer mousePositionLayer;
    private MemoryLayer tileGridLayer;
    
    private void InitializeLayers()
    {
        AddPlaceholderBaseLayer();
        AddTileGridLayer();
        AddDrawingLayer();
        AddMousePositionLayer();
    }

    private void AddPlaceholderBaseLayer()
    {
        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, 20),
            "http://localhost/{x}/{y}/{z}"
        );
        Map.Layers.Add(new TileLayer(s));
    }

    private void AddDrawingLayer()
    {
        drawingLayer = new MemoryLayer
        {
            Name = nameof(drawingLayer),
            Style = new VectorStyle // 直接设置默认样式
            {
                Fill = new Brush(Color.FromArgb(32, 255, 255, 255)),
                Outline = new Pen(Color.Red, 2),
                Line = new Pen(Color.Red, 2),
            }
        };
        Map.Layers.Add(drawingLayer);
    }

    private void AddMousePositionLayer()
    {
        mousePositionLayer = new MemoryLayer
        {
            Name = nameof(mousePositionLayer),
            Style = new SymbolStyle // 直接设置默认样式
            {
                SymbolType = SymbolType.Rectangle,
                Fill = new Brush(Color.White),
                Outline = new Pen(Color.Red, 4),
                SymbolScale = 0.2,
            },
        };
        Map.Layers.Add(mousePositionLayer);
    }
    private void AddTileGridLayer()
    {
        tileGridLayer = new MemoryLayer
        {
            Name = nameof(tileGridLayer),
            Style = new VectorStyle // 直接设置默认样式
            {
                Fill = new Brush(Color.Transparent),
                Outline = new Pen(Color.Gray, 2),
                Line = new Pen(Color.Gray, 2),
            },
        };
        Map.Layers.Add(tileGridLayer);
    }


    public void LoadTileMaps(TileDataSource tileDataSource)
    {
        if (Map.Layers.Count > 0)
        {
            Map.Layers.Remove(Map.Layers[0]);
        }

        if (tileDataSource == null || string.IsNullOrEmpty(tileDataSource.Url))
        {
            Map.Layers.Insert(0, new MemoryLayer());
            return;
        }

        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, 20),
            tileDataSource.Url,
            userAgent: string.IsNullOrWhiteSpace(tileDataSource.UserAgent) ? null : tileDataSource.UserAgent
        );
        if (!string.IsNullOrWhiteSpace(tileDataSource.Host))
        {
            s.AddHeader("Host", tileDataSource.Host);
        }

        if (!string.IsNullOrWhiteSpace(tileDataSource.Origin))
        {
            s.AddHeader("Origin", tileDataSource.Origin);
        }

        if (!string.IsNullOrWhiteSpace(tileDataSource.Referer))
        {
            s.AddHeader("Referer", tileDataSource.Referer);
        }

        Map.Layers.Insert(0, new TileLayer(s));
    }
}