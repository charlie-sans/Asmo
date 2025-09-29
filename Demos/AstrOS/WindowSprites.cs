using Asmo.Gfx;
using Asmo.IO;
using System;

namespace AstrOS
{
    public static class WindowSprites
    {
        public static Sprite? TitleBarSprite;
        public static Sprite? FrameSprite;

        public static void LoadSprites()
        {
            // Load title bar sprite from file if available
            string path = "Assets/Titlebar.png";
            if (System.IO.File.Exists(path))
                TitleBarSprite = SpriteLoader.LoadFromFile(path);


            int w = 134, h = 24;
            int[] frameData = new int[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = (x < 8 || x >= w - 8 || y < 8 || y >= h - 8);
                    int color = border ? unchecked((int)0xFFAAAAAA) : unchecked((int)0xFF333333); // ARGB: light gray border, dark center
                    frameData[y * w + x] = color;
                }
            }
            FrameSprite = Sprite.FromArgbArray(w, h, frameData, false); // false = ARGB
        }
    }
}
