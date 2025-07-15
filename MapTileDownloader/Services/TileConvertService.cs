using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BruTile;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapTileDownloader.Services
{
    public class TileConvertService
    {
        private void Check(string mbtilesPath, string pattern)
        {
            if (string.IsNullOrEmpty(mbtilesPath))
            {
                throw new ArgumentException($"“{nameof(mbtilesPath)}”不能为 null 或空。", nameof(mbtilesPath));
            }

            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException($"“{nameof(pattern)}”不能为 null 或空。", nameof(pattern));
            }

            if (!pattern.Contains("{z}") || !pattern.Contains("{x}") || !pattern.Contains("{y}") || !pattern.Contains("{ext}"))
            {
                throw new ArgumentException($"“{nameof(pattern)}”必须包含{{z}}、{{x}}、{{y}}和{{ext}}占位符。", nameof(pattern));
            }
        }


        public async Task ConvertToMbtilesAsync(string mbtilesPath, IList<string> dirs, string pattern,
            bool skipExisted,
            IProgress<double> progress = null,
            CancellationToken cancellation = default)
        {
            if (dirs == null || dirs.Count == 0)
            {
                throw new ArgumentException("至少需要指定一个目录", nameof(dirs));
            }
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    throw new ArgumentException($"目录{dir}不存在", nameof(dirs));
                }
            }

            Check(mbtilesPath, pattern);

            var files = new List<(string File, int Z, int X, int Y)>();
            await Task.Run(() =>
            {
                pattern = pattern.Replace("\\", "/")
                .Replace("{x}", "(?<x>\\d+)")
                .Replace("{y}", "(?<y>\\d+)")
                .Replace("{z}", "(?<z>[12]?\\d)")
                .Replace(".", "\\.")
                .Replace("{ext}", ".+");
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                foreach (var dir in dirs)
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (IsMatchPattern(regex, Path.GetRelativePath(dir, file), out int z, out int x, out int y))
                        {
                            files.Add((file, z, x, y));
                        }
                    }
                }
            }, cancellation);
            if (files.Count == 0)
            {
                throw new Exception("没有找到符合条件的瓦片图像");
            }

            var minZ = files.Min(f => f.Z);
            var maxZ = files.Max(f => f.Z);
            var name = Path.GetFileName(dirs[0]);
            var format = Path.GetExtension(files[0].File).Trim('.');

            bool newFile = !File.Exists(mbtilesPath);
            await using var serviece = new MbtilesService(mbtilesPath, false);
            await serviece.InitializeAsync();

            var existingTiles = await serviece.GetExistingTilesAsync();

            int index = 0;
            foreach (var item in files)
            {
                cancellation.ThrowIfCancellationRequested();
                index++;
                progress?.Report((double)index / files.Count);
                var tileIndex = new TileIndex(item.X, item.Y, item.Z);
                if (existingTiles.Contains(tileIndex))
                {
                    continue;
                }
                await serviece.WriteTileAsync(item.X, item.Y, item.Z,
                    await File.ReadAllBytesAsync(item.File, cancellation).ConfigureAwait(false));
            }

            await serviece.UpdateMetadataAsync(name, "local files", Configs.Instance.MbtilesUseTms);
        }
        public async Task ConvertToFilesAsync(string mbtilesPath, string outputDir, string pattern,
             bool skipExisted,
             IProgress<double> progress = null,
             CancellationToken cancellation = default)
        {
            Check(mbtilesPath, pattern);
            if (string.IsNullOrEmpty(outputDir))
            {
                throw new ArgumentException($"“{nameof(outputDir)}”不能为 null 或空。", nameof(outputDir));
            }

            Directory.CreateDirectory(outputDir);

            await using var service = new MbtilesService(mbtilesPath, true);
            await service.InitializeAsync();

            var tiles = await service.GetExistingTilesAsync();
            var metadata = await service.GetMbtilesInfoAsync(false);
            if (metadata.Format == MbtilesService.UnknownFormat)
            {
                throw new Exception("无法识别的瓦片图像格式，请检查mbtiles文件");
            }

            int index = 0;
            int total = tiles.Count;

            foreach (var tile in tiles)
            {
                cancellation.ThrowIfCancellationRequested();

                index++;
                progress?.Report((double)index / total);

                string relativePath = pattern
                 .Replace("{z}", tile.Level.ToString())
                 .Replace("{x}", tile.Col.ToString())
                 .Replace("{y}", tile.Row.ToString())
                 .Replace("{ext}", metadata.Format);

                string outputPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

                string outputDirPath = Path.GetDirectoryName(outputPath);
                Directory.CreateDirectory(outputDirPath);

                if (skipExisted && File.Exists(outputPath))
                {
                    continue;
                }

                var tileData = await service.GetTileAsync(tile.Col, tile.Row, tile.Level);
                if (tileData == null)
                {
                    continue;
                }            

                await File.WriteAllBytesAsync(outputPath, tileData, cancellation);
            }
        }

        private bool IsMatchPattern(Regex regex, string relativePath, out int z, out int x, out int y)
        {
            relativePath = relativePath.Replace('\\', '/');
            var match = regex.Match(relativePath);

            if (match.Success &&
                match.Groups["x"].Success &&
                match.Groups["y"].Success &&
                match.Groups["z"].Success)
            {
                x = int.Parse(match.Groups["x"].Value);
                y = int.Parse(match.Groups["y"].Value);
                z = int.Parse(match.Groups["z"].Value);
                return true;
            }

            x = y = z = 0;
            return false;
        }

    }
}