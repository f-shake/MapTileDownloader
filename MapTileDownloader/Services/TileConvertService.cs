using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            IProgress<double> progress = null,
            CancellationToken cancellation = default)
        {
            if (dirs == null || dirs.Count == 0)
            {
                throw new ArgumentException("至少需要指定一个目录", nameof(dirs));
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
            if (newFile)
            {
                await serviece.InitializeMBTilesAsync(name, format, "local files", minZ, maxZ);
            }

            int index = 0;
            foreach (var item in files)
            {
                cancellation.ThrowIfCancellationRequested();
                index++;
                await serviece.WriteTileAsync(item.X, item.Y, item.Z,
                    await File.ReadAllBytesAsync(item.File, cancellation).ConfigureAwait(false));
                progress?.Report((double)index / files.Count);
            }
        }

        private bool IsMatchPattern(string pattern, string relativePath, out int z, out int x, out int y)
        {
            relativePath = relativePath.Replace('\\', '/');
            pattern = pattern.Replace('\\', '/').Replace("{x}", "(\\d+)").Replace("{y}", "(\\d+)")
                .Replace("{z}", "(\\d{1,2})").Replace(" * ", ".+ ");
            var regex = new Regex(pattern);
            var match = regex.Match(relativePath);
            if (match.Success && match.Groups.Count == 4)
            {
                z = int.Parse(match.Groups[1].Value);
                x = int.Parse(match.Groups[2].Value);
                y = int.Parse(match.Groups[3].Value);
                return true;
            }

            z = x = y = 0;
            return false;
        }
    }
}