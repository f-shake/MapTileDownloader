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
            int tileCountX = maxX - minX + 1;
            int tileCountY = maxY - minY + 1;
            long totalPixels = (long)tileCountX * tileCountY * tileSize * tileSize;
            long estimatedMemory = totalPixels * 4;

            return (totalPixels, estimatedMemory);
        }

        public async Task MergeTilesAsync(
            string outputPath,
            int z,
            int minX,
            int maxX,
            int minY,
            int maxY,
            int tileSize = 256,
            int quality = 80)
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
                    int offsetX = (x - minX) * tileSize;
                    int offsetY = (y - minY) * tileSize;

                    await AddTileToImageAsync(mbtilesService, resultImage, z, x, y, offsetX, offsetY, tileSize);
                }
            }
            if (outputPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
               outputPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                await resultImage.SaveAsync(outputPath, new JpegEncoder
                {
                    Quality = quality
                });
            }
            else if (outputPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            {
                await resultImage.SaveAsync(outputPath, new WebpEncoder
                {
                    Quality = quality
                });
            }
            else
            {
                await resultImage.SaveAsync(outputPath);
            }

            WriteWorldFile(outputPath, z, tileSize, minX, minY);
            WritePrjFile(outputPath);
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

        private void WritePrjFile(string imagePath)
        {
            string basePath = Path.ChangeExtension(imagePath, null);
            string prjPath = basePath + ".prj";

            const string epsg3857Wkt = """PROJCS["WGS 84 / Pseudo-Mercator",GEOGCS["WGS 84",DATUM["WGS_1984",SPHEROID["WGS 84",6378137,298.257223563,AUTHORITY["EPSG","7030"]],AUTHORITY["EPSG","6326"]],PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]],UNIT["degree",0.0174532925199433,AUTHORITY["EPSG","9122"]],AUTHORITY["EPSG","4326"]],PROJECTION["Mercator_1SP"],PARAMETER["central_meridian",0],PARAMETER["scale_factor",1],PARAMETER["false_easting",0],PARAMETER["false_northing",0],UNIT["metre",1,AUTHORITY["EPSG","9001"]],AXIS["Easting",EAST],AXIS["Northing",NORTH],EXTENSION["PROJ4","+proj=merc +a=6378137 +b=6378137 +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +k=1 +units=m +nadgrids=@null +wktext +no_defs"],AUTHORITY["EPSG","3857"]] """;

            File.WriteAllText(prjPath, epsg3857Wkt.Trim());
        }

        private void WriteWorldFile(
            string imagePath,
            int z,
            int tileSize,
            int minX,
            int minY)
        {
            var schema = new GlobalSphericalMercator(YAxis.OSM);
            double resolution = schema.Resolutions[z].UnitsPerPixel;
            double tileSizeMeters = tileSize * resolution;

            // 计算图像左上角的像素中心坐标
            double minLon = schema.OriginX + minX * tileSizeMeters;
            double maxLat = -schema.OriginX - minY * tileSizeMeters;

            double pixelSizeX = resolution;
            double pixelSizeY = resolution;

            // 生成 world file 文件扩展名
            string extension = Path.GetExtension(imagePath).ToLowerInvariant();
            string basePath = Path.ChangeExtension(imagePath, null);
            string worldFileExt = extension switch
            {
                ".jpg" or ".jpeg" => ".jgw",
                ".png" => ".pgw",
                ".webp" => ".wpw", // 非标准但可用
                _ => ".wld"
            };

            string worldFilePath = basePath + worldFileExt;

            using var writer = new StreamWriter(worldFilePath);
            writer.WriteLine($"{pixelSizeX:0.############}"); // A: 每像素宽度
            writer.WriteLine("0");                            // D: 行旋转
            writer.WriteLine("0");                            // B: 列旋转
            writer.WriteLine($"{-pixelSizeY:0.############}");// E: 每像素高度（负）
            writer.WriteLine($"{minLon + pixelSizeX / 2:0.############}"); // C: 左上角像素中心X
            writer.WriteLine($"{maxLat - pixelSizeY / 2:0.############}"); // F: 左上角像素中心Y
        }
    }
}