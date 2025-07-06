using BruTile;
using Mapsui;
using Mapsui.Nts;
using Mapsui.Styles;
using MapTileDownloader.Helpers;
using MapTileDownloader.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Color = Mapsui.Styles.Color;

namespace MapTileDownloader.UI.Mapping;

public partial class MapView
{
    private int currentDisplayingLevel = -1;

    public async Task DisplayTilesAsync(TileDataSource tileDataSource, IList<TileIndex> tiles)
    {
        if (tiles == null || !tiles.Any())
        {
            currentDisplayingLevel = -1;
            tileGridLayer.Features = []; // 清空图层
            Refresh();
            return;
        }

        currentDisplayingLevel = tiles[0].Level;
        var tileHelper = new TileHelper(tileDataSource);

        // 生成瓦片几何图形 + 标注
        var features = new List<GeometryFeature>();

        await Task.Run(() =>
        {
            foreach (var tileIndex in tiles)
            {
                if (tileIndex.Level != currentDisplayingLevel)
                {
                    throw new ArgumentException("瓦片的级别不统一", nameof(tiles));
                }

                var polygon = tileHelper.GetTilePolygon(tileIndex);
                var feature = new GeometryFeature(polygon);

                feature.Styles.Add(new LabelStyle
                {
                    Text = $"X={tileIndex.Col}\nY={tileIndex.Row}",
                    ForeColor = Color.Black,
                    HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                    VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
                    Offset = new Offset { X = 0, Y = 0 },
                    CollisionDetection = false,
                    MaxVisible = 0.5 * GetDisplayThreshold(),
                });

                features.Add(feature);
            }
        });
        tileGridLayer.Features = features;

        tileGridLayer.MaxVisible = GetDisplayThreshold();
        Refresh();
    }

    private double GetDisplayThreshold()
    {
        return Math.Pow(2, 20 - currentDisplayingLevel);
    }

    private void InitializeTile()
    {
        Map.Navigator.ViewportChanged += NavigatorOnViewportChanged;
    }

    private void NavigatorOnViewportChanged(object sender, PropertyChangedEventArgs e)
    {
    }
}