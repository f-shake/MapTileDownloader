using Avalonia.Controls;
using Avalonia.Input;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.UI.Avalonia;
using Mapsui.Extensions;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using BruTile.Predefined;
using BruTile.Web;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Avalonia.Extensions;
using MapTileDownloader.UI.ViewModels;

namespace MapTileDownloader.UI.Views;

public partial class DataSourcePanel : UserControl
{
    public DataSourcePanel()
    {
        InitializeComponent();
    }
}