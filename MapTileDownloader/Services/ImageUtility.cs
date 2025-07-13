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