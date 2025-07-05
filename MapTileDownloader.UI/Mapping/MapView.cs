using Avalonia.Interactivity;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui;
using Mapsui.UI.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using MapTileDownloader.Models;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping
{
    public partial class MapView : MapControl
    {
        public MapView()
        {
            InitializeMap();
        }

        private void InitializeMap()
        {
            Map.BackColor = Color.Gray;
            InitializeLayers();
            InitializeDrawing();
        }

        public void ZoomToGeometry(Geometry geometry, double growFactor = 0.1)
        {
            if (geometry == null || geometry.IsEmpty)
                return;

            var envelope = geometry.EnvelopeInternal;
            var extent = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
            var paddedExtent = growFactor <= 0 ? extent : extent.Grow(extent.Width * growFactor);
            Map.Navigator.ZoomToBox(paddedExtent);
            Refresh();
        }
    }
}