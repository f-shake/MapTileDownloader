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
using BruTile;
using MapTileDownloader.Helpers;
using MapTileDownloader.Models;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Geometry = NetTopologySuite.Geometries.Geometry;
using Pen = Mapsui.Styles.Pen;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    public void DisplayTiles(TileDataSource tileDataSource, IList<TileIndex> tiles)
    {
        if (tiles == null || !tiles.Any())
        {
            tileGridLayer.Features = []; // 清空图层
        }
        else
        {
            var tileHelper = new TileHelper(tileDataSource);

            // 生成瓦片几何图形 + 标注
            tileGridLayer.Features = tiles.Select(tileIndex =>
            {
                // 1. 获取瓦片多边形
                var polygon = tileHelper.GetTilePolygon(tileIndex);

                // 2. 创建要素并绑定标注属性
                var feature = new GeometryFeature(polygon)
                {
                    ["Col"] = tileIndex.Col, // 存储列号
                    ["Row"] = tileIndex.Row, // 存储行号
                    ["LabelText"] = $"X={tileIndex.Col}, Y={tileIndex.Row}" // 标注文本
                };

                // 3. 设置标注样式
                feature.Styles.Add(new LabelStyle
                {
                    Text = $"X={tileIndex.Col}\nY={tileIndex.Row}", // 动态获取标注
                    ForeColor = Color.Black,
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset { X = 0, Y = 0 },
                    CollisionDetection = true,
                    MaxVisible =  Math.Pow(2, 20-tileIndex.Level),
                });

                return feature;
            }).ToList();
        }

        Refresh(); // 刷新地图显示
    }
}