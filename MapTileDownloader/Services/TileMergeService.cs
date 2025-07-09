using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ImageMagick;

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
            await mbtilesService. InitializeAsync(); // 确保数据库连接已打开
            int totalWidth = (maxX - minX + 1) * tileSize;
            int totalHeight = (maxY - minY + 1) * tileSize;

            // 使用 MagickImageCollection 分块处理
            using (var images = new MagickImageCollection())
            {
                // 分块处理瓦片
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        int offsetX = (x - minX) * tileSize;
                        int offsetY = (y - minY) * tileSize;
                        await AddTileToCollectionAsync(images, z, x, y, offsetX, offsetY, tileSize);
                    }
                }
                //还是占用大量内存
                // 合并所有瓦片并保存
                using (var result = images.Mosaic())
                {
                    Environment.SetEnvironmentVariable("MAGICK_TEMPORARY_PATH", tempDirectory);
                    result.Write(outputPath);
                }
            }

            if (cleanupTempFiles)
                CleanupTempFiles();
        }

        private async Task AddTileToCollectionAsync(
            MagickImageCollection collection,
            int z, int x, int y,
            int offsetX, int offsetY,
            int tileSize)
        {
            byte[] tileData = await mbtilesService.GetTileAsync(x, y, z);
            if (tileData == null || tileData.Length == 0)
                return;

            string tempTilePath = Path.Combine(tempDirectory, $"tile_{x}_{y}_{z}.tmp");
            await File.WriteAllBytesAsync(tempTilePath, tileData);

            // 注意：不再使用 using，由 MagickImageCollection 管理生命周期
            var tileImage = new MagickImage(tempTilePath);
            try
            {
                if (tileImage.Width != tileSize || tileImage.Height != tileSize)
                    tileImage.Resize((uint)tileSize, (uint)tileSize);

                tileImage.Page = new MagickGeometry(offsetX, offsetY, (uint)tileSize, (uint)tileSize);
                collection.Add(tileImage);
            }
            catch
            {
                // 如果添加失败，手动释放资源
                tileImage?.Dispose();
                throw;
            }
            finally
            {
                File.Delete(tempTilePath);
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch { /* 忽略清理错误 */ }
        }
    }
}