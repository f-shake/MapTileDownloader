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

            if (!pattern.Contains("{z}") || !pattern.Contains("{x}") || !pattern.Contains("{y}"))
            {
                throw new ArgumentException($"“{nameof(pattern)}”必须包含{{z}}、{{x}}和{{y}}占位符。", nameof(pattern));
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
                foreach (var dir in dirs)
                {
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                    {
                        cancellation.ThrowIfCancellationRequested();
                        if (IsMatchPattern(pattern, Path.GetRelativePath(dir, file), out int z, out int x, out int y))
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
            await serviece.InitializeMetadataAsync(name, format, "local files", minZ, maxZ);

            var existingTiles =await serviece.GetExistingTilesAsync();

            int index = 0;
            foreach (var item in files)
            {
                cancellation.ThrowIfCancellationRequested();
                index++;
                progress?.Report((double)index / files.Count);
                var tileIndex=new TileIndex(item.X,item.Y, item.Z);
                if (existingTiles.Contains(tileIndex))
                {
                    continue;
                }
                await serviece.WriteTileAsync(item.X, item.Y, item.Z,
                    await File.ReadAllBytesAsync(item.File, cancellation).ConfigureAwait(false));
            }
        }
    //    public async Task ConvertToFilesAsync(string mbtilesPath, string outputDir, string pattern,
    //bool overwrite = false,
    //IProgress<double> progress = null,
    //CancellationToken cancellation = default)
    //    {
    //        if (string.IsNullOrEmpty(mbtilesPath))
    //            throw new ArgumentException($"“{nameof(mbtilesPath)}”不能为 null 或空。", nameof(mbtilesPath));

    //        if (string.IsNullOrEmpty(outputDir))
    //            throw new ArgumentException($"“{nameof(outputDir)}”不能为 null 或空。", nameof(outputDir));

    //        if (string.IsNullOrEmpty(pattern) || !pattern.Contains("{z}") || !pattern.Contains("{x}") || !pattern.Contains("{y}"))
    //            throw new ArgumentException($"“{nameof(pattern)}”必须包含{{z}}、{{x}}和{{y}}占位符。", nameof(pattern));

    //        if (!Directory.Exists(outputDir))
    //            Directory.CreateDirectory(outputDir);

    //        await using var service = new MbtilesService(mbtilesPath, true);
    //        await service.InitializeAsync();

    //        var tiles = await service.GetExistingTilesAsync();

    //        int index = 0;
    //        int total = tiles.Count;

    //        foreach (var tile in tiles)
    //        {
    //            cancellation.ThrowIfCancellationRequested();

    //            index++;
    //            progress?.Report((double)index / total);

    //            // tile 格式是 "z/x/y"
    //            var parts = tile.Split('/');
    //            if (parts.Length != 3)
    //                continue;

    //            if (!int.TryParse(parts[0], out int z) ||
    //                !int.TryParse(parts[1], out int x) ||
    //                !int.TryParse(parts[2], out int y))
    //                continue;

    //            var tileData = await service.GetTileAsync(x, y, z);
    //            if (tileData == null)
    //                continue;

    //            // 根据 pattern 替换为路径
    //            string relativePath = pattern.Replace("{z}", z.ToString())
    //                                         .Replace("{x}", x.ToString())
    //                                         .Replace("{y}", y.ToString());

    //            string outputPath = Path.Combine(outputDir, relativePath.Replace('/', Path.DirectorySeparatorChar));

    //            string outputDirPath = Path.GetDirectoryName(outputPath);
    //            if (!Directory.Exists(outputDirPath))
    //                Directory.CreateDirectory(outputDirPath);

    //            if (!overwrite && File.Exists(outputPath))
    //                continue;

    //            await File.WriteAllBytesAsync(outputPath, tileData, cancellation);
    //        }
    //    }

        private bool IsMatchPattern(string pattern, string relativePath, out int z, out int x, out int y)
        {
            relativePath = relativePath.Replace('\\', '/');

            // 使用具名捕获组替换 pattern 中的 {x}、{y}、{z}
            pattern = pattern.Replace("\\", "/")
                .Replace("{x}", "(?<x>\\d+)")
                .Replace("{y}", "(?<y>\\d+)")
                .Replace("{z}", "(?<z>\\d{1,2})")
                .Replace(" * ", ".+");

            var regex = new Regex(pattern);
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