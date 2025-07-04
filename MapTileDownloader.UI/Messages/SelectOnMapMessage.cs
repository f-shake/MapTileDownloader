using NetTopologySuite.Geometries;
using System.Threading;
using System.Threading.Tasks;

namespace MapTileDownloader.UI.Messages;

public class SelectOnMapMessage
{
    public SelectOnMapMessage(CancellationToken cancellationToken)
    {
        CancellationToken = cancellationToken;
    }

    public Task<Coordinate[]> Task { get; set; }
    public CancellationToken CancellationToken { get; }
}