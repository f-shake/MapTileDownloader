using BruTile;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MapTileDownloader.UI.ViewModels;

public partial class LevelTilesViewModel : ObservableObject
{
    [ObservableProperty]
    private int downloadedCount;

    public LevelTilesViewModel() : this(-1, [])
    {
    }

    public LevelTilesViewModel(int level, IEnumerable<TileIndex> tiles)
    {
        Level = level;
        Tiles = new ObservableCollection<TileIndex>(tiles);
        Tiles.CollectionChanged += (s, e) => { OnPropertyChanged(nameof(Count)); };
    }
    public int Count => Tiles.Count;
    public int Level { get; set; }
    public ObservableCollection<TileIndex> Tiles { get; }
}