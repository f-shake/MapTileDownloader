﻿using BruTile;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Styles;
using MapTileDownloader.Services;
using MapTileDownloader.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Color = Mapsui.Styles.Color;
using System.Data;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    private Dictionary<int, List<GeometryFeature>> featuresPerLevel;
    private bool refreshPending = false;

    private void RequestRefresh()
    {
        if (refreshPending)
        {
            return;
        }

        refreshPending = true;

        _ = Task.Delay(1000).ContinueWith(_ =>
        {
            refreshPending = false;
            Dispatcher.UIThread.Post(() => { Refresh(); });
        });
    }

    public void ClearTileGrids()
    {
        featuresPerLevel = null;
        overlayTileGridLayer.Features = [];
    }

    public void DisplayTileGrids(int level)
    {
        if (featuresPerLevel == null)
        {
            throw new Exception("还未初始化瓦片网格");
        }

        if (!featuresPerLevel.TryGetValue(level, out var features))
        {
            throw new ArgumentException($"已加载的瓦片中不包含级别{level}", nameof(level));
        }

        overlayTileGridLayer.Features = features;

        overlayTileGridLayer.MaxVisible = 5 * GetDisplayThreshold(level);
        Refresh();
    }

    public async Task LoadTileGridsAsync(TileDataSource tileDataSource, IEnumerable<IDownloadingLevel> levels)
    {
        if (levels == null || !levels.Any())
        {
            throw new ArgumentException("提供的levels为空", nameof(levels));
        }

        featuresPerLevel = new Dictionary<int, List<GeometryFeature>>();
        var tileHelper = new TileIntersectionService(false);

        // 生成瓦片几何图形 + 标注

        await Task.Run(() =>
        {
            foreach (var level in levels)
            {
                var tiles = level.Tiles;

                var features = new List<GeometryFeature>();
                foreach (var tile in tiles)
                {
                    var polygon = tileHelper.GetTilePolygon(tile.TileIndex);
                    var feature = new GeometryFeature(polygon);

                    feature.Styles.Add(new VectorStyle
                    {
                        Fill = new Brush(Color.Transparent),
                        Outline = new Pen(Color.Gray, 2),
                        Line = new Pen(Color.Gray, 2),
                        Opacity = 0.33f
                    });
                    feature.Styles.Add(new LabelStyle
                    {
                        Text = $"X={tile.TileIndex.Col}\nY={tile.TileIndex.Row}",
                        ForeColor = Color.Black,
                        HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                        VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                        Offset = new Offset { X = 0, Y = 0 },
                        CollisionDetection = false,
                        MaxVisible = 0.5 * GetDisplayThreshold(level.Level),
                    });

                    void SetBackground(Color color, bool requestRefresh)
                    {
                        VectorStyle style = feature.Styles.OfType<VectorStyle>().First();
                        style.Fill = new Brush(color);
                        if (requestRefresh)
                        {
                            RequestRefresh();
                        }
                    }

                    void UpdateColor(DownloadStatus status, bool requestRefresh)
                    {
                        switch (status)
                        {
                            case DownloadStatus.Ready:
                                SetBackground(Color.Transparent, requestRefresh);
                                break;
                            case DownloadStatus.Downloading:
                                SetBackground(Color.Orange, requestRefresh);
                                break;
                            case DownloadStatus.Success:
                            case DownloadStatus.Skip:
                                SetBackground(Color.Green, requestRefresh);
                                break;
                            case DownloadStatus.Failed:
                                SetBackground(Color.Red, requestRefresh);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    tile.DownloadStatusChanged += (s, e) =>
                    {
                        UpdateColor(e.NewStatus, true);
                    };

                    UpdateColor(tile.Status, false);
                    features.Add(feature);
                }

                featuresPerLevel.Add(level.Level, features);
            }
        });
    }

    private double GetDisplayThreshold(int level)
    {
        return Math.Pow(2, 20 - level);
    }

    private void InitializeTile()
    {
        Map.Navigator.ViewportChanged += NavigatorOnViewportChanged;
    }

    private void NavigatorOnViewportChanged(object sender, PropertyChangedEventArgs e)
    {
    }
}