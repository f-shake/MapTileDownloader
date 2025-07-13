using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                .WithWebApi("/", m => m.WithController(() => new TileController(mbtilesService, options.ReturnEmptyPngWhenNotFound)));

            await server.RunAsync(cancellationToken);
        }

        public class TileServerOptions
        {
            public bool LocalhostOnly { get; set; } = true;
            public string MbtilesPath { get; set; }
            public int Port { get; set; } = 8888;
            public bool ReturnEmptyPngWhenNotFound { get; set; } = true;
        }
        private class TileController : WebApiController
        {
            private readonly bool returnEmptyPngWhenNotFound;

            public TileController(MbtilesService mbtilesService, bool returnEmptyPngWhenNotFound)
            {
                MbtilesService = mbtilesService;
                this.returnEmptyPngWhenNotFound = returnEmptyPngWhenNotFound;
            }

            public MbtilesService MbtilesService { get; }

            [Route(HttpVerbs.Get, "/{z}/{x}/{y}")]
            public async Task GetTileAsync(int z, int x, int y)
            {
                var data = await MbtilesService.GetTileAsync(x, y, z).ConfigureAwait(false);
                if (data == null)
                {
                    if (returnEmptyPngWhenNotFound)
                    {
                        await HttpContext.Response.OutputStream.WriteAsync(ImageUtility.GetEmptyTileImage(x, y, z));
                    }
                    else
                    {
                        Response.StatusCode = 404;
                        await Response.OutputStream.WriteAsync("Tile not found"u8.ToArray());
                    }
                    return;
                }

                HttpContext.Response.ContentType = GetImageMimeType(data);
                await HttpContext.Response.OutputStream.WriteAsync(data);
            }

            private static string GetImageMimeType(byte[] fileBytes)
            {
                if (fileBytes.Length < 4)
                {
                    return "application/octet-stream";
                }

                if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
                {
                    return "image/jpeg";
                }

                if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
                {
                    return "image/png";
                }

                if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x38)
                {
                    return "image/gif";
                }

                if (fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46)
                {
                    if (fileBytes.Length >= 12 && fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && fileBytes[10] == 0x42 && fileBytes[11] == 0x50)
                    {
                        return "image/webp";
                    }
                }

                if (fileBytes[0] == 0x42 && fileBytes[1] == 0x4D)
                {
                    return "image/bmp";
                }

                if (fileBytes[0] == 0x3C && fileBytes[1] == 0x3F && fileBytes[2] == 0x78 && fileBytes[3] == 0x6D)
                {
                    return "image/svg+xml";
                }

                if (fileBytes[0] == 0x00 && fileBytes[1] == 0x00 && fileBytes[2] == 0x01 && fileBytes[3] == 0x00)
                {
                    return "image/x-icon";
                }

                return "application/octet-stream";
            }
        }
    }
}