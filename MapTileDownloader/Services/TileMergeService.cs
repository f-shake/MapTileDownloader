using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapTileDownloader.Services
{
    public class TileMergeService(string mbtilesPath) : MbtilesBasedService(mbtilesPath, true)
    {
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
            int tileSize = 256)
        {
            if (tileSize is not (256 or 512))
            {
                throw new ArgumentException("瓦片尺寸应当为256或512像素", nameof(tileSize));
            }

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

                    await AddTileToImageAsync(resultImage, z, x, y, offsetX, offsetY, tileSize);
                }
            }

            await resultImage.SaveAsync(outputPath);
        }

        private async Task AddTileToImageAsync(
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
    }
}