namespace Asmo.Gfx
{
    public static class Font
    {
        // Each byte[] is 7 rows, 5 bits per row (leftmost is highest bit)
        private static readonly byte[][] Font5x7Data =
        {
            // ... fill in all ASCII chars ...
            new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 }, // 65 'A'
            new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10001, 0b10001, 0b11110 }, // 66 'B'
           
        };

        public static readonly bool[,,] Font5x7 = new bool[128, 5, 7];

        static Font()
        {
            for (int ch = 0; ch < Font5x7Data.Length; ch++)
            {
                var charData = Font5x7Data[ch];
                if (charData == null) continue;
                for (int y = 0; y < 7; y++)
                {
                    byte row = charData[y];
                    for (int x = 0; x < 5; x++)
                    {
                        Font5x7[ch, x, y] = ((row >> (4 - x)) & 1) != 0;
                    }
                }
            }
        }
    }
}
