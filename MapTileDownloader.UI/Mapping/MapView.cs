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
            Map.Layers.Add(new MemoryLayer());
            InitializeDrawing();
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
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
                tileSource.Url
            );
            Map.Layers.Insert(0, new TileLayer(s));
        }
    }
}