using BruTile;
using MapTileDownloader.Services;
using System;

namespace MapTileDownloader.TileSources;

public class TileGridSource(ITileSchema schema) : ILocalTileSource
{
    public ITileSchema Schema { get; } = schema;
    public string Name { get; }
    public Attribution Attribution { get; }

    public Task<byte[]> GetTileAsync(TileInfo tileInfo)
    {
        var index = tileInfo.Index;
        return Task.FromResult(ImageUtility.GetEmptyTileImage(index.Level, index.Col, index.Row));
    }
}
