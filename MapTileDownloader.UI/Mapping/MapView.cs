using Avalonia;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Styles;
using Mapsui.UI.Avalonia;
using MapTileDownloader.Services;
using NetTopologySuite.Geometries;

namespace MapTileDownloader.UI.Mapping
{
    public partial class MapView : MapControl, IMapService
    {
        public MapView()
        {
            InitializeMap();
        }

        public void ZoomToGeometry(Geometry geometry, double growFactor = 0.1)
        {
            if (geometry == null || geometry.IsEmpty)
            {
                return;
            }

            var envelope = geometry.EnvelopeInternal;
            var extent = new MRect(envelope.MinX, envelope.MinY, envelope.MaxX, envelope.MaxY);
            var paddedExtent = growFactor <= 0 ? extent : extent.Grow(extent.Width * growFactor);
            Map.Navigator.ZoomToBox(paddedExtent);
            Refresh();
        }

        private void InitializeMap()
        {
            if (Application.Current?.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Dark)
            {
                Map.BackColor = Color.FromArgb(0xFF, 0x22, 0x22, 0x22);
            }
            else
            {
                Map.BackColor = Color.FromArgb(0xFF, 0xDD, 0xDD, 0xDD);
            }

            Map.Widgets.Clear();
            InitializeLayers();
            InitializeDrawing();
            InitializeTile();
        }
    }
}