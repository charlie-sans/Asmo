using Asmo.Types;
using OpenTK.Windowing.Desktop;

namespace Asmo.Gfx
{
    // Sprite class for pixel art and UI graphics
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
        /// <summary>
        /// Measures the width and height of a string in the default font (5x7, 6px per char, 7px height).
        /// </summary>
        public (int Width, int Height) MeasureText(string text)
        {
            if (string.IsNullOrEmpty(text)) return (0, 0);
            var lines = text.Split('\n');
            int maxLine = 0;
            foreach (var line in lines)
                if (line.Length > maxLine) maxLine = line.Length;
            int width = maxLine * 6;
            int height = lines.Length * 18; // 18px line spacing as in DrawText
            return (width, height);
        }
        /// <summary>
        /// Create a sprite from a width, height, and an array of 32-bit ARGB or RGBA values.
        /// </summary>
        public static Sprite FromArgbArray(int width, int height, int[] pixels, bool isRgba = true)
        {
			var arr = new Color[width][];
			for (int x = 0; x < width; x++)
			{
				arr[x] = new Color[height];
				for (int y = 0; y < height; y++)
				{
					int idx = y * width + x;
					int val = pixels[idx];
					byte a, r, g, b;
					if (isRgba)
					{
						r = (byte)((val >> 24) & 0xFF);
						g = (byte)((val >> 16) & 0xFF);
						b = (byte)((val >> 8) & 0xFF);
						a = (byte)(val & 0xFF);
					}
					else // ARGB
					{
						a = (byte)((val >> 24) & 0xFF);
						r = (byte)((val >> 16) & 0xFF);
						g = (byte)((val >> 8) & 0xFF);
						b = (byte)(val & 0xFF);
					}
					arr[x][y] = new Color(r, g, b, a);
				}
			}
			return new Sprite(width, height, arr);
		}

        /// <summary>
        /// Create a sprite from a width, height, and a byte array (RGBA or ARGB, 4 bytes per pixel)
        /// </summary>
        public static Sprite FromByteArray(int width, int height, byte[] data, bool isRgba = true)
        {
            var arr = new Color[width][];
            for (int x = 0; x < width; x++)
            {
                arr[x] = new Color[height];
                for (int y = 0; y < height; y++)
                {
                    int idx = (y * width + x) * 4;
                    byte r, g, b, a;
                    if (isRgba)
                    {
                        r = data[idx];
                        g = data[idx + 1];
                        b = data[idx + 2];
                        a = data[idx + 3];
                    }
                    else // ARGB
                    {
                        a = data[idx];
                        r = data[idx + 1];
                        g = data[idx + 2];
                        b = data[idx + 3];
                    }
                    arr[x][y] = new Color(r, g, b, a);
                }
            }
            return new Sprite(width, height, arr);
        }
    }

    // End of Sprite class
}

namespace Asmo.Gfx
{

    public class Surface
    {
        /// <summary>
        /// Draw a rectangle filled with a vertical gradient from colorTop to colorBottom.
        /// </summary>
        public void DrawVerticalGradientRect(int x, int y, int width, int height, Asmo.Types.Color colorTop, Asmo.Types.Color colorBottom)
        {
            for (int iy = 0; iy < height; iy++)
            {
                float t = height > 1 ? iy / (float)(height - 1) : 0f;
                byte r = (byte)(colorTop.R + t * (colorBottom.R - colorTop.R));
                byte g = (byte)(colorTop.G + t * (colorBottom.G - colorTop.G));
                byte b = (byte)(colorTop.B + t * (colorBottom.B - colorTop.B));
                byte a = (byte)(colorTop.A + t * (colorBottom.A - colorTop.A));
                var rowColor = new Asmo.Types.Color(r, g, b, a);
                for (int ix = 0; ix < width; ix++)
                {
                    SetPixel(x + ix, y + iy, rowColor);
                }
            }
        }

        // Dirty rectangle tracking for minimal updates
        private int dirtyX0 = int.MaxValue, dirtyY0 = int.MaxValue, dirtyX1 = int.MinValue, dirtyY1 = int.MinValue;
        public bool IsDirty => dirtyX0 <= dirtyX1 && dirtyY0 <= dirtyY1;

        /// <summary>
        /// Get the current dirty rectangle. Returns (x, y, w, h). If not dirty, returns (0,0,0,0).
        /// </summary>
        public (int x, int y, int w, int h) GetDirtyRect()
        {
            if (!IsDirty) return (0, 0, 0, 0);
            return (dirtyX0, dirtyY0, dirtyX1 - dirtyX0 + 1, dirtyY1 - dirtyY0 + 1);
        }

        /// <summary>
        /// Reset the dirty rectangle after uploading changes.
        /// </summary>
        public void ClearDirty()
        {
            dirtyX0 = int.MaxValue; dirtyY0 = int.MaxValue; dirtyX1 = int.MinValue; dirtyY1 = int.MinValue;
        }

        private void MarkDirty(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            if (x < dirtyX0) dirtyX0 = x;
            if (x > dirtyX1) dirtyX1 = x;
            if (y < dirtyY0) dirtyY0 = y;
            if (y > dirtyY1) dirtyY1 = y;
        }

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

        public void FillRect(int x, int y, int width, int height, Surface surface, Color color)
        {
            surface.DrawRect(x, y, width, height, color);
        }

        public void DrawText(int x, int y, string text, Color color)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
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
            {
                Pixels[x][y] = color;
                MarkDirty(x, y);
            }
        }

        public void Clear(Color color)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Pixels[x][y] = color;
            // Mark the whole surface as dirty
            dirtyX0 = 0; dirtyY0 = 0; dirtyX1 = Width - 1; dirtyY1 = Height - 1;
        }

        public void DrawRect(int x, int y, int w, int h, Color color)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            for (int ix = x; ix < x + w; ix++)
                for (int iy = y; iy < y + h; iy++)
                    SetPixel(ix, iy, color);
            // Mark the affected region as dirty
            MarkDirty(x, y);
            MarkDirty(x + w - 1, y + h - 1);
        }

        public void DrawSprite(Sprite sprite, int x, int y)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
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

        /// <summary>
        /// Draw a subregion of a sprite at the given position.
        /// </summary>
        public void DrawSpriteRegion(Sprite sprite, int x, int y, int srcX, int srcY, int srcW, int srcH)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            for (int sx = 0; sx < srcW; sx++)
                for (int sy = 0; sy < srcH; sy++)
                {
                    int px = srcX + sx;
                    int py = srcY + sy;
                    if (px < sprite.Width && py < sprite.Height && px >= 0 && py >= 0)
                    {
                        var c = sprite.Pixels[px][py];
                        if (c.A > 0)
                            SetPixel(x + sx, y + sy, c);
                    }
                }
        }

        // Draw a line using Bresenham's algorithm
        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
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
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
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
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                    if (x * x + y * y <= radius * radius)
                        SetPixel(cx + x, cy + y, color);
        }

        // Draw an outlined rectangle
        public void DrawOutlinedRect(int x, int y, int w, int h, Color color)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            DrawLine(x, y, x + w - 1, y, color);
            DrawLine(x, y, x, y + h - 1, color);
            DrawLine(x + w - 1, y, x + w - 1, y + h - 1, color);
            DrawLine(x, y + h - 1, x + w - 1, y + h - 1, color);
        }

        // Draw an ellipse (outline)
        public void DrawEllipse(int cx, int cy, int rx, int ry, Color color)
        {
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
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
}
