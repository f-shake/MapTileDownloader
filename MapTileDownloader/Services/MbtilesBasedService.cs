namespace MapTileDownloader.Services;

public abstract class MbtilesBasedService : IAsyncDisposable, IDisposable
{
    protected readonly MbtilesService mbtilesService;

    public MbtilesBasedService(string mbtilesPath, bool readOnly)
    {
        mbtilesService = new MbtilesService(mbtilesPath, false);
    }
    public virtual void Dispose()
    {
        mbtilesService?.Dispose();
    }

    public virtual async ValueTask DisposeAsync()
    {
        if (mbtilesService is not null)
        {
            await mbtilesService.DisposeAsync().ConfigureAwait(false);
        }
    }
}
