using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using BruTile;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MapTileDownloader.UI.ViewModels;

public partial class LevelTilesViewModel : ObservableObject
{
    public LevelTilesViewModel() : this(-1, [])
    {
    }

    public LevelTilesViewModel(int level, IEnumerable<TileIndex> tiles)
    {
        Level = level;
        Tiles = new ObservableCollection<TileIndex>(tiles);
        Tiles.CollectionChanged += (s, e) => { OnPropertyChanged(nameof(Count)); };
    }

    [ObservableProperty]
    private int downloadedCount;

    public int Level { get; set; }

    public int Count => Tiles.Count;

    public ObservableCollection<TileIndex> Tiles { get; }
}