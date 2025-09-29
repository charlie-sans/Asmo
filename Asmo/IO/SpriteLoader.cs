using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Asmo.Types;
using Asmo.Gfx;


namespace Asmo.IO
{
    public static class SpriteLoader
    {
        public static Sprite LoadFromFile(string path)
        {
            using (var image = Image.Load<Rgba32>(path))
            {
                int w = image.Width, h = image.Height;
                var pixels = new Asmo.Types.Color[w][];
                for (int x = 0; x < w; x++)
                {
                    pixels[x] = new Asmo.Types.Color[h];
                    for (int y = 0; y < h; y++)
                    {
                        var c = image[x, y];
                        pixels[x][y] = new Asmo.Types.Color(c.R, c.G, c.B, c.A);
                    }
                }
                return new Sprite(w, h, pixels);
            }
        }

        public static void SaveSurfaceAsPng(Surface surface, string path)
        {
            using (var image = new Image<Rgba32>(surface.Width, surface.Height))
            {
                for (int x = 0; x < surface.Width; x++)
                    for (int y = 0; y < surface.Height; y++)
                    {
                        var c = surface.Pixels[x][y];
                        image[x, y] = new Rgba32((byte)c.R, (byte)c.G, (byte)c.B, (byte)c.A);
                    }
                image.SaveAsPng(path);
            }
        }
    }
}
