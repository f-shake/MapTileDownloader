﻿using BruTile.Predefined;
using BruTile.Web;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using MapTileDownloader.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using BruTile;
using MapTileDownloader.Enums;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Pen = Mapsui.Styles.Pen;
using BruTile.Wms;
using MapTileDownloader.Services;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    /// <summary>
    /// XYZ底图
    /// </summary>
    private TileLayer baseLayer;

    /// <summary>
    /// 绘制和范围图层
    /// </summary>
    private MemoryLayer drawingLayer;

    /// <summary>
    /// 本地服务底图
    /// </summary>
    private TileLayer localBaseLayer;

    /// <summary>
    /// 绘制时鼠标位置图层
    /// </summary>
    private MemoryLayer mousePositionLayer;

    /// <summary>
    /// 瓦片网格图层
    /// </summary>
    private MemoryLayer tileGridLayer;

    public void LoadLocalTileMaps(MbtilesTileSource tileSource)
    {
        bool isEnabled = true;
        if (Map.Layers.Count > (int)AppLayer.LocalBaseLayer)
        {
            isEnabled = Map.Layers.Get((int)AppLayer.LocalBaseLayer).Enabled;
            Map.Layers.Remove(Map.Layers.Get((int)AppLayer.LocalBaseLayer));
        }

        if (tileSource == null)
        {
            Map.Layers.Insert((int)AppLayer.LocalBaseLayer, new MemoryLayer());
            return;
        }

        localBaseLayer = new TileLayer(tileSource) { Enabled = isEnabled };
        Map.Layers.Insert((int)AppLayer.LocalBaseLayer, localBaseLayer);
    }

    public void LoadTileMaps(TileDataSource tileDataSource)
    {
        if (Map.Layers.Count > 0)
        {
            Map.Layers.Remove(Map.Layers.Get((int)AppLayer.BaseLayer));
        }

        if (tileDataSource == null || string.IsNullOrEmpty(tileDataSource.Url))
        {
            Map.Layers.Insert((int)AppLayer.BaseLayer, new MemoryLayer());
            return;
        }

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


        baseLayer = new TileLayer(s);
        Map.Layers.Insert(0, baseLayer);
    }

    public void SetEnable(AppLayer index, bool enable)
    {
        if (index < 0 || (int)index >= Map.Layers.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围");
        }

        var layer = Map.Layers.Get((int)index);
        layer.Enabled = enable;
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

    private void AddPlaceholderBaseLayer()
    {
        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, 20),
            "http://localhost/{x}/{y}/{z}"
        );
        baseLayer = new TileLayer(s);
        baseLayer.Enabled = false;
        Map.Layers.Add(baseLayer);
    }

    private void AddPlaceholderLocalBaseLayer()
    {
        var s = new HttpTileSource(
            new GlobalSphericalMercator(0, 20),
            "http://localhost/{x}/{y}/{z}"
        );
        localBaseLayer = new TileLayer(s);
        localBaseLayer.Enabled = false;
        Map.Layers.Add(localBaseLayer);
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
                Opacity = 0.33f
            },
        };
        Map.Layers.Add(tileGridLayer);
    }

    private void InitializeLayers()
    {
        AddPlaceholderBaseLayer();
        AddPlaceholderLocalBaseLayer();
        AddTileGridLayer();
        AddDrawingLayer();
        AddMousePositionLayer();
    }
}