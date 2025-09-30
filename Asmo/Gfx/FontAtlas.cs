using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;

namespace Asmo.Gfx
{
    public static class FontAtlas
    {
        public static int TextureId { get; private set; }
        public static int GlyphWidth { get; private set; }
        public static int GlyphHeight { get; private set; }
        public static int Cols { get; private set; }
        public static int Rows { get; private set; }
        public static bool Loaded { get; private set; } = false;

        // Load from file path
        public static void Load(string path, int glyphWidth, int glyphHeight, int cols, int rows)
        {
            using var fs = File.OpenRead(path);
            LoadFromStream(fs, glyphWidth, glyphHeight, cols, rows);
        }

        // Load from embedded resource or any stream
        public static void LoadFromStream(Stream stream, int glyphWidth, int glyphHeight, int cols, int rows)
        {
            using var image = Image.Load<Rgba32>(stream);
            UploadImage(image, glyphWidth, glyphHeight, cols, rows);
        }

        // Load from embedded resource by name (helper)
        public static void LoadEmbedded(string resourceName, int glyphWidth, int glyphHeight, int cols, int rows)
        {
            var asm = typeof(FontAtlas).Assembly;
            using var stream = asm.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
            LoadFromStream(stream, glyphWidth, glyphHeight, cols, rows);
        }

        // Uploads the image to OpenGL and sets up atlas info
        private static void UploadImage(Image<Rgba32> image, int glyphWidth, int glyphHeight, int cols, int rows)
        {
            GlyphWidth = glyphWidth;
            GlyphHeight = glyphHeight;
            Cols = cols;
            Rows = rows;

            int width = image.Width;
            int height = image.Height;

            byte[] pixelBytes = new byte[width * height * 4];
            var frame = image.Frames.RootFrame;
            for (int y = 0; y < height; y++)
            {
                var rowSpan = frame.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    pixelBytes[i + 0] = rowSpan[x].R;
                    pixelBytes[i + 1] = rowSpan[x].G;
                    pixelBytes[i + 2] = rowSpan[x].B;
                    pixelBytes[i + 3] = rowSpan[x].A;
                }
            }
            
            TextureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, TextureId);
            System.Runtime.InteropServices.GCHandle handle = System.Runtime.InteropServices.GCHandle.Alloc(pixelBytes, System.Runtime.InteropServices.GCHandleType.Pinned);
            try
            {
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            Loaded = true;
        }

        // Check if a font is loaded
        public static bool IsLoaded() => Loaded;

        // Unload the font atlas (delete GL texture)
        public static void Unload()
        {
            if (Loaded && TextureId != 0)
            {
                GL.DeleteTexture(TextureId);
                TextureId = 0;
                Loaded = false;
            }
        }
    }
}
