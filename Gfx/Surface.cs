using Asmo.Types;

namespace Asmo.Gfx
{
    public class Surface
    {
        public int Width { get; }
        public int Height { get; }
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
                int px = x + i * 6; // Assuming 5x7 font with 1 pixel spacing
                for (int fx = 0; fx < 5; fx++)
                    for (int fy = 0; fy < 7; fy++)
                    {
                        // Flip fy so font is right-side up
                        if (Font.Font5x7[(int)text[i], fx, fy])
                            SetPixel(px + fx, y + (6 - fy), color);
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
                    var c = sprite.Pixels[sx][sy];
                    if (c.A > 0) // Only draw non-transparent
                        SetPixel(x + sx, y + sy, c);
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
