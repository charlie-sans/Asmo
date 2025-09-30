using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Asmo.Gfx
{
    public class RuntimeFontAtlas
    {
        public int TextureId { get; private set; }
        public int AtlasWidth { get; private set; }
        public int AtlasHeight { get; private set; }
        public int GlyphHeight { get; private set; }
        public int GlyphAscent { get; private set; }
        public Dictionary<char, (int x, int y, int w, int h, int xOffset, int yOffset, int xAdvance)> Glyphs { get; private set; }

        public RuntimeFontAtlas(string fontPath, int fontSize, string charset = null)
        {
            charset ??= " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            var fontCollection = new FontCollection();
            var font = fontCollection.Add(fontPath);
            var typeface = font.CreateFont(fontSize, FontStyle.Regular);
            var glyphs = new List<(char c, float width, float height, float xOffset, float yOffset, float xAdvance)>();
            int maxGlyphWidth = 0, maxGlyphHeight = 0, ascent = 0;
            var fontMetrics = typeface.FontMetrics;
            foreach (char c in charset)
            {
                var text = c.ToString();
                var options = new TextOptions(typeface)
                {
                    Dpi = 96,
                    KerningMode = KerningMode.None,
                    Origin = new PointF(0, 0)
                };
                // Measure advance width for the glyph
                float advance = TextMeasurer.MeasureAdvance(text, options).Width;
                // Use CapHeight for ascent (since Ascender is not available)
                int asc = (int)MathF.Ceiling(fontMetrics.UnitsPerEm);
                // Use bounding box for width/height
                var bounds = TextMeasurer.MeasureBounds(text, options);
                float width = bounds.Width;
                float height = bounds.Height;
                glyphs.Add((c, width, height, bounds.Left, bounds.Top, advance));
                maxGlyphWidth = Math.Max(maxGlyphWidth, (int)MathF.Ceiling(width));
                maxGlyphHeight = Math.Max(maxGlyphHeight, (int)MathF.Ceiling(height));
                ascent = Math.Max(ascent, asc);
            }
            int cols = (int)Math.Ceiling(Math.Sqrt(glyphs.Count));
            int rows = (int)Math.Ceiling(glyphs.Count / (float)cols);
            int atlasWidth = cols * maxGlyphWidth;
            int atlasHeight = rows * maxGlyphHeight;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
            GlyphHeight = maxGlyphHeight;
            GlyphAscent = ascent;
            var image = new Image<Rgba32>(atlasWidth, atlasHeight);
            Glyphs = new Dictionary<char, (int x, int y, int w, int h, int xOffset, int yOffset, int xAdvance)>();
            int gx = 0, gy = 0, col = 0, row = 0;
            foreach (var (c, width, height, xOffset, yOffset, xAdvance) in glyphs)
            {
                var text = c.ToString();
                var options = new RichTextOptions(typeface)
                {
                    Dpi = 96,
                    KerningMode = KerningMode.None,
                    Origin = new PointF(gx, gy + ascent)
                };
                image.Mutate(ctx => ctx.DrawText(options, text, Color.White));
                Glyphs[c] = (gx, gy, (int)MathF.Ceiling(width), (int)MathF.Ceiling(height), (int)xOffset, (int)yOffset, (int)xAdvance);
                col++;
                gx += maxGlyphWidth;
                if (col >= cols) { col = 0; gx = 0; row++; gy += maxGlyphHeight; }
            }
            // Upload image to OpenGL and set TextureId
            try
            {
                var pixels = new byte[atlasWidth * atlasHeight * 4];
                image.CopyPixelDataTo(pixels);
                int tex = OpenTK.Graphics.OpenGL4.GL.GenTexture();
                OpenTK.Graphics.OpenGL4.GL.BindTexture(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, tex);
                OpenTK.Graphics.OpenGL4.GL.TexImage2D(
                    OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D,
                    0,
                    OpenTK.Graphics.OpenGL4.PixelInternalFormat.Rgba,
                    atlasWidth,
                    atlasHeight,
                    0,
                    OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                    OpenTK.Graphics.OpenGL4.PixelType.UnsignedByte,
                    pixels
                );
                OpenTK.Graphics.OpenGL4.GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMinFilter, (int)OpenTK.Graphics.OpenGL4.TextureMinFilter.Linear);
                OpenTK.Graphics.OpenGL4.GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureMagFilter, (int)OpenTK.Graphics.OpenGL4.TextureMagFilter.Linear);
                OpenTK.Graphics.OpenGL4.GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapS, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToEdge);
                OpenTK.Graphics.OpenGL4.GL.TexParameter(OpenTK.Graphics.OpenGL4.TextureTarget.Texture2D, OpenTK.Graphics.OpenGL4.TextureParameterName.TextureWrapT, (int)OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToEdge);
                TextureId = tex;
                var glErr = OpenTK.Graphics.OpenGL4.GL.GetError();
                if (glErr != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
                {
                    System.Console.WriteLine($"[RuntimeFontAtlas] OpenGL Error after texture upload: {glErr}");
                }
                else
                {
                    System.Console.WriteLine($"[RuntimeFontAtlas] Font atlas texture created: {TextureId} ({atlasWidth}x{atlasHeight})");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[RuntimeFontAtlas] Exception during texture upload: {ex}");
            }
        }
    }
}
