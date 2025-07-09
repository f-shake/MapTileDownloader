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
            await mbtilesService. InitializeAsync(); // ȷ�����ݿ������Ѵ�
            int totalWidth = (maxX - minX + 1) * tileSize;
            int totalHeight = (maxY - minY + 1) * tileSize;

            // ʹ�� MagickImageCollection �ֿ鴦��
            using (var images = new MagickImageCollection())
            {
                // �ֿ鴦����Ƭ
                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        int offsetX = (x - minX) * tileSize;
                        int offsetY = (y - minY) * tileSize;
                        await AddTileToCollectionAsync(images, z, x, y, offsetX, offsetY, tileSize);
                    }
                }
                //����ռ�ô����ڴ�
                // �ϲ�������Ƭ������
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

            // ע�⣺����ʹ�� using���� MagickImageCollection ������������
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
                // ������ʧ�ܣ��ֶ��ͷ���Դ
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
            catch { /* ����������� */ }
        }
    }
}