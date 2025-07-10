using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapTileDownloader.Services
{
    public class TileMergeService : MbtilesBasedService
    {
        private readonly bool cleanupTempFiles;
        private readonly string tempDirectory;

        public TileMergeService(string mbtilesPath, bool cleanupTempFiles = true)
            : base(mbtilesPath, true)
        {
            this.cleanupTempFiles = cleanupTempFiles;
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "TileMergeTemp");
            Directory.CreateDirectory(this.tempDirectory);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (cleanupTempFiles)
                CleanupTempFiles();
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

            // 自动根据扩展名决定格式
            await resultImage.SaveAsync(outputPath);

            if (cleanupTempFiles)
                CleanupTempFiles();
        }

        private async Task AddTileToImageAsync(
            Image<Rgba32> canvas,
            int z, int x, int y,
            int offsetX, int offsetY,
            int tileSize)
        {
            byte[] tileData = await mbtilesService.GetTileAsync(x, y, z);
            if (tileData == null || tileData.Length == 0)
                return;

            using var tileImage = Image.Load<Rgba32>(tileData);
            if (tileImage.Width != tileSize || tileImage.Height != tileSize)
            {
                tileImage.Mutate(x => x.Resize(tileSize, tileSize));
            }

            canvas.Mutate(x => x.DrawImage(tileImage, new Point(offsetX, offsetY), 1f));
        }

        private void CleanupTempFiles()
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch { /* 忽略异常 */ }
        }
    }
}
