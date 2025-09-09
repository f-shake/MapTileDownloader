using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO.Actions;

namespace MapTileDownloader.Services
{
    public class TileServerService : IAsyncDisposable
    {
        private readonly TileServerOptions options;

        private MbtilesService mbtilesService;

        private WebServer server;

        private TileServerService(TileServerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!File.Exists(options.MbtilesPath))
            {
                throw new FileNotFoundException("MBTiles文件不存在", options.MbtilesPath);
            }

            this.options = options;
            mbtilesService = new MbtilesService(options.MbtilesPath, true);
        }

        public static async Task RunAsync(TileServerOptions options, CancellationToken cancellationToken)
        {
            await using TileServerService serverService = new TileServerService(options);
            await serverService.RunAsync(cancellationToken).ConfigureAwait(false);
        }


        public ValueTask DisposeAsync()
        {
            server?.Dispose();
            return mbtilesService.DisposeAsync();
        }


        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await mbtilesService.InitializeAsync().ConfigureAwait(false);
            var host = options.LocalhostOnly ? "localhost" : "*";
            var url = $"http://{host}:{options.Port}/";

            server = new WebServer(o => o
                    .WithUrlPrefix(url)
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithModule(new ActionModule("/", HttpVerbs.Get, HandleTileRequestAsync));

            await server.RunAsync(cancellationToken);
        }

        private async Task HandleTileRequestAsync(IHttpContext ctx)
        {
            var path = ctx.RequestedPath.Trim('/');
            var parts = path.Split('/');
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var z) &&
                int.TryParse(parts[1], out var x) &&
                int.TryParse(parts[2], out var y))
            {
                var data = await mbtilesService.GetTileAsync(x, y, z).ConfigureAwait(false);
                if (data == null)
                {
                    if (options.ReturnEmptyPngWhenNotFound)
                    {
                        ctx.Response.ContentType = "image/png";
                        await ctx.Response.OutputStream.WriteAsync(ImageUtility.GetEmptyTileImage(x, y, z));
                    }
                    else
                    {
                        ctx.Response.StatusCode = 404;
                        await ctx.Response.OutputStream.WriteAsync("Tile not found"u8.ToArray());
                    }
                }
                else
                {
                    ctx.Response.ContentType = ImageUtility.GetImageType(data).mime;
                    await ctx.Response.OutputStream.WriteAsync(data);
                }
            }
            else
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.OutputStream.WriteAsync("Invalid path"u8.ToArray());
            }
        }


        public class TileServerOptions
        {
            public bool LocalhostOnly { get; set; } = true;
            public string MbtilesPath { get; set; }
            public int Port { get; set; } = 8888;
            public bool ReturnEmptyPngWhenNotFound { get; set; } = true;
        }
    }
}