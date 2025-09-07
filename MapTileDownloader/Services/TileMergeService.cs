using BruTile;
using BruTile.Predefined;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MapTileDownloader.Services
{
    public class TileMergeService(string mbtilesPath)
    {
        public string MbtilesPath { get; } = mbtilesPath;

        public (long totalPixels, long estimatedMemoryBytes) EstimateTileMergeMemory(
            int minX, int maxX, int minY, int maxY, int tileSize = 256)
        {
            int tileCountX = Math.Abs(maxX - minX) + 1;
            int tileCountY = Math.Abs(maxY - minY) + 1;
            long totalPixels = (long)tileCountX * tileCountY * tileSize * tileSize;
            long estimatedMemory = totalPixels * 4;

            return (totalPixels, estimatedMemory);
        }

        public async Task MergeTilesAsync(
            string outputPath,
            bool useTms,
            int z,
            int minX,
            int maxX,
            int minY,
            int maxY,
            int tileSize = 256,
            int quality = 80,
            CancellationToken ct = default)
        {
            quality = Math.Clamp(quality, 10, 100);
            if (tileSize is not (256 or 512))
            {
                throw new ArgumentException("瓦片尺寸应当为256或512像素", nameof(tileSize));
            }

            await using var mbtilesService = new MbtilesService(MbtilesPath, true);
            await mbtilesService.InitializeAsync();

            int totalWidth = (maxX - minX + 1) * tileSize;
            int totalHeight = (maxY - minY + 1) * tileSize;

            using var resultImage = new Image<Rgba32>(totalWidth, totalHeight);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    ct.ThrowIfCancellationRequested();
                    int offsetX = (x - minX) * tileSize;
                    int offsetY = useTms
                        ? (maxY - y) * tileSize // TMS 是 Y 轴向上，反着画
                        : (y - minY) * tileSize; // OSM 是 Y 轴向下，正着画

                    await AddTileToImageAsync(mbtilesService, resultImage, z, x, y, offsetX, offsetY, tileSize);
                }
            }

            if (outputPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                outputPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                await resultImage.SaveAsync(outputPath, new JpegEncoder
                {
                    Quality = quality
                }, cancellationToken: ct);
            }
            else if (outputPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            {
                await resultImage.SaveAsync(outputPath, new WebpEncoder
                {
                    Quality = quality
                }, cancellationToken: ct);
            }
            else
            {
                await resultImage.SaveAsync(outputPath, cancellationToken: ct);
            }

            await WriteWorldFileAsync(outputPath, useTms, z, tileSize, minX, minY, maxY);
            await WritePrjFileAsync(outputPath);
        }

        private async Task AddTileToImageAsync(
            MbtilesService mbtilesService,
            Image<Rgba32> canvas,
            int z, int x, int y,
            int offsetX, int offsetY,
            int tileSize)
        {
            byte[] tileData = await mbtilesService.GetTileAsync(x, y, z);
            if (tileData == null || tileData.Length == 0)
            {
                return;
            }

            using var tileImage = Image.Load<Rgba32>(tileData);
            if (tileImage.Width != tileSize || tileImage.Height != tileSize)
            {
                tileImage.Mutate(x => x.Resize(tileSize, tileSize));
            }

            canvas.Mutate(x => x.DrawImage(tileImage, new Point(offsetX, offsetY), 1f));
        }

        private async Task WritePrjFileAsync(string imagePath)
        {
            string basePath = Path.ChangeExtension(imagePath, null);
            string prjPath = basePath + ".prj";

            const string epsg3857Wkt =
                """PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Easting",EAST],AXIS["Northing",NORTH],EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +k=1 +units=m +nadgrids=@null +wktext +no_defs"],AUTHORITY["EPSG","3857"]] """;

            await File.WriteAllTextAsync(prjPath, epsg3857Wkt.Trim());
        }

        private async Task WriteWorldFileAsync(
            string imagePath,
            bool useTms,
            int z,
            int tileSize,
            int minX,
            int minY,
            int maxY)
        {
            var schema = new GlobalSphericalMercator(useTms ? YAxis.TMS : YAxis.OSM);
            double resolution = schema.Resolutions[z].UnitsPerPixel;
            double tileSizeMeters = tileSize * resolution;

            double minLon = schema.OriginX + minX * tileSizeMeters;
            double topLat = schema.OriginY + (useTms ? (maxY + 1) : -minY) * tileSizeMeters;

            double pixelSizeX = resolution;
            double pixelSizeY = resolution;

            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            string basePath = Path.ChangeExtension(imagePath, null);
            string worldFileExt = extension switch
            {
                ".jpg" or ".jpeg" => ".jgw",
                ".png" => ".pgw",
                ".webp" => ".wpw",
                _ => ".wld"
            };

            string worldFilePath = basePath + worldFileExt;

            await using var writer = new StreamWriter(worldFilePath);
            await writer.WriteLineAsync($"{pixelSizeX:0.############}"); // A
            await writer.WriteLineAsync("0"); // D
            await writer.WriteLineAsync("0"); // B
            await writer.WriteLineAsync($"{-pixelSizeY:0.############}"); // E
            await writer.WriteLineAsync($"{minLon + pixelSizeX / 2:0.############}"); // C
            await writer.WriteLineAsync($"{topLat - pixelSizeY / 2:0.############}"); // F
        }
    }
}