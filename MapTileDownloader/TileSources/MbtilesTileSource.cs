using BruTile;
using System;
using BruTile.Predefined;
using MapTileDownloader.Services;

namespace MapTileDownloader.TileSources;

public class MbtilesTileSource : ILocalTileSource, IDisposable, IAsyncDisposable
{
    private readonly MbtilesService mbtilesService;
    private bool disposed = false;
    private bool initialized;

    public MbtilesTileSource(string mbtilesFile, bool useTms)
    {
        mbtilesService = new MbtilesService(mbtilesFile, true);
        Schema = new GlobalSphericalMercator(useTms ? YAxis.TMS : YAxis.OSM);
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
        return await mbtilesService.GetTileAsync(index.Col, index.Row, index.Level)
               ?? throw new Exception("Tile not found"); //如果返回null，会导致缩放范围之外的瓦片无法显示，所以这里抛出异常
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