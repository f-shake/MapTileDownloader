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

namespace MapTileDownloader.UI.Mapping
{
    public partial class MapView : MapControl
    {

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            InitializeMap();
        }
        private void InitializeMap()
        {           
            // 加载地图瓦片（示例：自定义瓦片源）
            var tileSource = new HttpTileSource(
                new GlobalSphericalMercator(0, 18),
                "https://s.fshake.com/map/google/{x}/{y}/{z}"
            );
            Map.Layers.Add(new TileLayer(tileSource));

            InitializeDrawing();

            StartDrawing();
        }

    }
}
