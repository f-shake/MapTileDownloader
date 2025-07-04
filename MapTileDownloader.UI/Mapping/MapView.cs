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
            // Map.CRS = "EPSG:3857";
            //Map.Layers.Add(new MemoryLayer());
            var s = new HttpTileSource(
                new GlobalSphericalMercator(0, 20),
               "http://localhost/{x}/{y}/{z}"
            );
            Map.Layers.Add(new TileLayer(s));
            Map.Navigator.ZoomToBox(new MRect(-20037508.34, -20037508.34, 20037508.34, 20037508.34));
            InitializeDrawing();
        }

        public void LoadTileMaps(TileSource tileSource)
        {
            if (Map.Layers.Count > 0)
            {
                Map.Layers.Remove(Map.Layers[0]);
            }

            if (tileSource == null || string.IsNullOrEmpty(tileSource.Url))
            {
                Map.Layers.Insert(0, new MemoryLayer());
                return;
            }

            var s = new HttpTileSource(
                new GlobalSphericalMercator(0, 20),
                tileSource.Url,
                userAgent: string.IsNullOrWhiteSpace(tileSource.UserAgent) ? null : tileSource.UserAgent
            );
            if (!string.IsNullOrWhiteSpace(tileSource.Host))
            {
                s.AddHeader("Host", tileSource.Host);
            }

            if (!string.IsNullOrWhiteSpace(tileSource.Origin))
            {
                s.AddHeader("Origin", tileSource.Origin);
            }

            if (!string.IsNullOrWhiteSpace(tileSource.Referer))
            {
                s.AddHeader("Referer", tileSource.Referer);
            }

            Map.Layers.Insert(0, new TileLayer(s));
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