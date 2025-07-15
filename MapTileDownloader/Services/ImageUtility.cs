using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapTileDownloader.Services
{
    public class ImageUtility
    {
        static ImageUtility()
        {
            InitializeDefaultFont();
        }

        public static (string type, string mime) GetImageType(byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length < 4)
            {
                return (null, "application/octet-stream");
            }

            // JPEG
            if (fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF)
            {
                return ("jpg", "image/jpeg");
            }

            // PNG
            if (fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47)
            {
                return ("png", "image/png");
            }

            // GIF
            if (fileBytes[0] == 0x47 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x38)
            {
                return ("gif", "image/gif");
            }

            // WEBP - RIFF header followed by WEBP
            if (fileBytes[0] == 0x52 && fileBytes[1] == 0x49 && fileBytes[2] == 0x46 && fileBytes[3] == 0x46)
            {
                if (fileBytes.Length >= 12 && fileBytes[8] == 0x57 && fileBytes[9] == 0x45 && fileBytes[10] == 0x42 && fileBytes[11] == 0x50)
                {
                    return ("webp", "image/webp");
                }
            }

            // BMP
            if (fileBytes[0] == 0x42 && fileBytes[1] == 0x4D)
            {
                return ("bmp", "image/bmp");
            }

            // ICO
            if (fileBytes[0] == 0x00 && fileBytes[1] == 0x00 && fileBytes[2] == 0x01 && fileBytes[3] == 0x00)
            {
                return ("ico", "image/x-icon");
            }

            // HEIF (HEIC)
            if (fileBytes[4] == 0x66 && fileBytes[5] == 0x74 && fileBytes[6] == 0x79 && fileBytes[7] == 0x70 &&
                (fileBytes[8] == 0x68 && fileBytes[9] == 0x65 && fileBytes[10] == 0x69 && fileBytes[11] == 0x63 || // heic
                 fileBytes[8] == 0x6D && fileBytes[9] == 0x69 && fileBytes[10] == 0x66 && fileBytes[11] == 0x31))   // mif1
            {
                return ("heif", "image/heif");
            }

            for (int i = 0; i < fileBytes.Length - 4; i++)
            {
                if (fileBytes[i] == '<' && fileBytes[i + 1] == 's' && fileBytes[i + 2] == 'v' && fileBytes[i + 3] == 'g')
                {
                    return ("svg", "image/svg+xml");
                }
            }

            return (null, "application/octet-stream");
        }


        private static void InitializeDefaultFont()
        {
            foreach (var fontName in FallbackFontNames)
            {
                if (SystemFonts.TryGet(fontName, out var fontFamily))
                {
                    defaultFont = defaultFont = fontName;
                    return;
                }
            }

            defaultFont = SystemFonts.Families.First().Name;
        }

        private static readonly string[] FallbackFontNames =
        [
            "Microsoft YaHei",  // 中文(Windows)
            "Segoe UI",         // Windows
            "Helvetica Neue",   // macOS
            "Arial",            // 通用
            "PingFang SC",      // 中文(苹果)
            "Noto Sans",        // Linux/Android
            "sans-serif"        // 最后回退
        ];
        private static string defaultFont;
        private const int DefaultSize = 256;
        private const int DefaultBorderWidth = 4;
        private const int DefaultFontSize = 24;
        private static readonly Color DefaultBackgroundColor = Color.Transparent;
        private static readonly Color DefaultBorderColor = Color.Gray;
        private static readonly Color DefaultTextColor = Color.White;
        private static readonly Color DefaultHaloColor = Color.Black;
        private const float DefaultHaloWidth = 5;
        private const float LineSpacing = 1.2f;
        private const int TextPadding = 20;

        public static byte[] GetEmptyTileImage(int z, int x, int y,
            int size = DefaultSize, int borderWidth = DefaultBorderWidth, int fontSize = DefaultFontSize,
            Color? background = null, Color? border = null, Color? label = null,
            Color? halo = null, float haloWidth = DefaultHaloWidth)
        {
            try
            {
                var backgroundColor = background ?? DefaultBackgroundColor;
                var borderColor = border ?? DefaultBorderColor;
                var textColor = label ?? DefaultTextColor;
                var haloColor = halo ?? DefaultHaloColor;

                using var image = new Image<Rgba32>(size, size, backgroundColor);

                // 画边框
                image.Mutate(ctx =>
                {
                    ctx.Draw(borderColor, borderWidth, new RectangleF(0, 0, size, size));
                });

                // 文字内容
                string text = $"Z={z}\nX={x}\nY={y}";

                // 绘制文字（自动居中）
                image.Mutate(ctx =>
                {

                    ctx.SetGraphicsOptions(g => g.Antialias = true);

                    var textOptions = new RichTextOptions(SystemFonts.CreateFont(defaultFont, fontSize, FontStyle.Regular))
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(size / 2f, size / 2f),
                        WrappingLength = size - TextPadding,
                        LineSpacing = LineSpacing,
                    };

                    ctx.DrawText(textOptions, text, new SolidPen(haloColor, haloWidth));
                    ctx.DrawText(textOptions, text, textColor);
                });

                // 保存为 PNG 并返回字节数组
                using var ms = new MemoryStream(CalculateInitialCapacity(size));
                image.Save(ms, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                // 记录日志或处理异常
                throw new InvalidOperationException("生成图片失败", ex);
            }
        }

        // 预估初始内存流容量
        private static int CalculateInitialCapacity(int size)
        {
            // 简单估算：宽*高*4(ARGB) + 一些头部信息
            return size * size * 4 + 1024;
        }
    }
}