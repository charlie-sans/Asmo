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
        // Draw a line using Bresenham's algorithm
        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy, e2;
            while (true)
            {
                SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        // Draw a circle (midpoint algorithm)
        public void DrawCircle(int cx, int cy, int radius, Color color)
        {
            int x = radius, y = 0, err = 0;
            while (x >= y)
            {
                SetPixel(cx + x, cy + y, color); SetPixel(cx + y, cy + x, color);
                SetPixel(cx - y, cy + x, color); SetPixel(cx - x, cy + y, color);
                SetPixel(cx - x, cy - y, color); SetPixel(cx - y, cy - x, color);
                SetPixel(cx + y, cy - x, color); SetPixel(cx + x, cy - y, color);
                y++;
                if (err <= 0) { err += 2 * y + 1; }
                if (err > 0) { x--; err -= 2 * x + 1; }
            }
        }

        // Draw a filled circle
        public void DrawFilledCircle(int cx, int cy, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        SetPixel(cx + x, cy + y, color);
        }

        // Draw an outlined rectangle
        public void DrawOutlinedRect(int x, int y, int w, int h, Color color)
        {
            DrawLine(x, y, x + w - 1, y, color);
            DrawLine(x, y, x, y + h - 1, color);
            DrawLine(x + w - 1, y, x + w - 1, y + h - 1, color);
            DrawLine(x, y + h - 1, x + w - 1, y + h - 1, color);
        }

        // Draw an ellipse (outline)
        public void DrawEllipse(int cx, int cy, int rx, int ry, Color color)
        {
            int x, y;
            int rx2 = rx * rx, ry2 = ry * ry;
            int tworx2 = 2 * rx2, twory2 = 2 * ry2;
            int px = 0, py = tworx2 * ry;
            // Region 1
            for (x = 0, y = ry, px = 0, py = tworx2 * ry; rx2 * y > ry2 * x; x++)
            {
                SetPixel(cx + x, cy + y, color); SetPixel(cx - x, cy + y, color);
                SetPixel(cx + x, cy - y, color); SetPixel(cx - x, cy - y, color);
                px += twory2;
                if (2 * px > py)
                {
                    y--;
                    py -= tworx2;
                }
            }
            // Region 2
            for (x = rx, y = 0, px = 0, py = twory2 * rx; ry2 * x > rx2 * y; y++)
            {
                SetPixel(cx + x, cy + y, color); SetPixel(cx - x, cy + y, color);
                SetPixel(cx + x, cy - y, color); SetPixel(cx - x, cy - y, color);
                py += tworx2;
                if (2 * py > px)
                {
                    x--;
                    px -= twory2;
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
