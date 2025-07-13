using BruTile;
using System;
using BruTile.Predefined;

namespace MapTileDownloader.Services;

public class MbtilesTileSource : ILocalTileSource, IDisposable, IAsyncDisposable
{
    private readonly MbtilesService mbtilesService;
    private bool disposed = false;
    private bool initialized;
    public MbtilesTileSource(string mbtilesFile)
    {
        mbtilesService = new MbtilesService(mbtilesFile, true);
        Schema = new GlobalSphericalMercator(YAxis.OSM);
    }

    public ITileSchema Schema { get; }

    public string Name { get; } = "Mbtiles Tile Source";

    public Attribution Attribution { get; }

    public async Task<byte[]> GetTileAsync(TileInfo tileInfo)
    {
        if (!initialized)
        {
            throw new InvalidOperationException("还未进行初始化");
        }

        if (tileInfo == null)
        {
            throw new ArgumentNullException(nameof(tileInfo));
        }

        if (disposed)
        {
            throw new ObjectDisposedException(nameof(MbtilesTileSource));
        }

        var index = tileInfo.Index;
        return await mbtilesService.GetTileAsync(index.Col, index.Row, index.Level) ?? 
            ImageUtility.GetEmptyTileImage(index.Col, index.Row, index.Level);
    }

    public ValueTask InitializeAsync()
    {
        initialized = true;
        return mbtilesService.InitializeAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            mbtilesService.Dispose();
        }

        disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!disposed)
        {
            await mbtilesService.DisposeAsync().ConfigureAwait(false);
        }
    }

    ~MbtilesTileSource()
    {
        Dispose(false);
    }
}