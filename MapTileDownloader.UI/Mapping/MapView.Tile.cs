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
        var tileHelper=new TileHelper(tileDataSource);
        tileGridLayer.Features = tiles
            .Select(p=>tileHelper.GetTilePolygon(p))
            .Select(p=>new GeometryFeature(p))
            .ToList();
        Refresh();
    }
    
    
}