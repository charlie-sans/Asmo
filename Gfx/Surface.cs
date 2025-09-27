using Asmo.Types;
using OpenTK.Windowing.Desktop;

namespace Asmo.Gfx
{
    public class Surface
    {
        public int Width { get; }
        public int Height { get; }
    // left null for now
    public GLFWGraphicsContext Context { get; set; }
    /// <summary>
    /// Reference to the owning window, if any.
    /// </summary>
    public Asmo.Window.Window Window { get; set; }
        public Color[][] Pixels { get; }

        public Surface(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color[width][];
            for (int x = 0; x < width; x++)
            {
                Pixels[x] = new Color[height];
                for (int y = 0; y < height; y++)
                    Pixels[x][y] = new Color(0, 0, 0, 255);
            }
        }

        public void DrawText(int x, int y, string text, Color color)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int px = x + i * 6;
                char c = text[i];
                if (!Font.Font5x7Map.TryGetValue(c, out var glyph))
                    continue; // skip unknown chars
                for (int fx = 0; fx < 5; fx++)
                    for (int fy = 0; fy < 7; fy++)
                    {
                        if (glyph[fx, fy])
                            SetPixel(px + fx, y + fy, color);
                    }
            }
        }   
        public void SetPixel(int x, int y, Color color)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
                Pixels[x][y] = color;
        }

        public void Clear(Color color)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Pixels[x][y] = color;
        }

        public void DrawRect(int x, int y, int w, int h, Color color)
        {
            for (int ix = x; ix < x + w; ix++)
                for (int iy = y; iy < y + h; iy++)
                    SetPixel(ix, iy, color);
        }

        public void DrawSprite(Sprite sprite, int x, int y)
        {
            for (int sx = 0; sx < sprite.Width; sx++)
                for (int sy = 0; sy < sprite.Height; sy++)
                {
                    if (sx < sprite.Pixels.Length && sy < sprite.Pixels[sx].Length)
                    {
                        var c = sprite.Pixels[sx][sy];
                        if (c.A > 0)
                            SetPixel(x + sx, y + sy, c);
                    }
                    else
                    {
                        //// Log or break here to see what's wrong
                        //Console.WriteLine($"Out of bounds: sx={sx}, sy={sy}");
                    }
                }
        }
    }

    public class Sprite
    {
        public int Width { get; }
        public int Height { get; }
        public Color[][] Pixels { get; }

        public Sprite(int width, int height, Color[][] pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }
    }
}
