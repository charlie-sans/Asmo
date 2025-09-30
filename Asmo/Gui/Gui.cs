using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Gui
{
    public static class Gui
    {
        // Simple state for layout
        private static int cursorX = 10, cursorY = 10, spacingY = 22;

        public static void Begin(int x = 10, int y = 10)
        {
            cursorX = x;
            cursorY = y;
        }
        public static void DrawVerticalGradientRect(Asmo.Gfx.Surface surface, int x, int y, int w, int h, Asmo.Types.Color top, Asmo.Types.Color bottom)
    {
        for (int i = 0; i < h; i++)
        {
            float t = h > 1 ? (float)i / (h - 1) : 0f;
            byte r = (byte)(top.R + t * (bottom.R - top.R));
            byte g = (byte)(top.G + t * (bottom.G - top.G));
            byte b = (byte)(top.B + t * (bottom.B - top.B));
            byte a = (byte)(top.A + t * (bottom.A - top.A));
            var rowColor = new Asmo.Types.Color(r, g, b, a);
            for (int j = 0; j < w; j++)
                surface.SetPixel(x + j, y + i, rowColor);
        }
    }

        public static void Label(Surface surface, string text, Color color)
        {
            surface.DrawText(cursorX, cursorY, text, color);
            cursorY += spacingY;
        }

        public static bool Button(Surface surface, string text, Color color, int mouseX, int mouseY, bool mouseDown)
        {
            int w = text.Length * 8 + 20;
            int h = 28;
            int radius = 6;
            // Shadow
            Color shadow = new Color(0, 0, 0, 60);
            surface.DrawFilledCircle(cursorX + radius + 2, cursorY + radius + 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + w - radius - 2, cursorY + radius + 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + radius + 2, cursorY + h - radius - 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + w - radius - 2, cursorY + h - radius - 2, radius, shadow);
            surface.DrawRect(cursorX + 2, cursorY + radius + 2, w - 4, h - 2 * radius - 4, shadow);
            surface.DrawRect(cursorX + radius + 2, cursorY + 2, w - 2 * radius - 4, h - 4, shadow);

            // State colors
            bool hovered = mouseX >= cursorX && mouseX < cursorX + w && mouseY >= cursorY && mouseY < cursorY + h;
            bool clicked = hovered && mouseDown;
            Color bg = clicked ? new Color(60, 120, 220, 255) : hovered ? new Color(90, 150, 255, 255) : new Color(40, 60, 100, 255);
            Color fg = new Color(255, 255, 255, 255);
            Color border = hovered ? new Color(120, 180, 255, 255) : new Color(80, 100, 140, 255);

            // Filled rounded rect (simulate with circles + rects)
            surface.DrawFilledCircle(cursorX + radius, cursorY + radius, radius, bg);
            surface.DrawFilledCircle(cursorX + w - radius - 1, cursorY + radius, radius, bg);
            surface.DrawFilledCircle(cursorX + radius, cursorY + h - radius - 1, radius, bg);
            surface.DrawFilledCircle(cursorX + w - radius - 1, cursorY + h - radius - 1, radius, bg);
            surface.DrawRect(cursorX + radius, cursorY, w - 2 * radius, h, bg);
            surface.DrawRect(cursorX, cursorY + radius, w, h - 2 * radius, bg);

            // Border
            surface.DrawOutlinedRect(cursorX, cursorY, w, h, border);

            // Text
            surface.DrawText(cursorX + (w - text.Length * 7) / 2, cursorY + (h - 12) / 2, text, fg);
            cursorY += spacingY + 10;
            return clicked;
        }

        public static bool Checkbox(Surface surface, string text, ref bool value, Color color, int mouseX, int mouseY, bool mouseDown)
        {
            int boxSize = 14;
            surface.DrawOutlinedRect(cursorX, cursorY, boxSize, boxSize, color);
            if (value)
                surface.DrawFilledCircle(cursorX + boxSize / 2, cursorY + boxSize / 2, 5, color);
            surface.DrawText(cursorX + boxSize + 6, cursorY, text, color);
            bool hovered = mouseX >= cursorX && mouseX < cursorX + boxSize && mouseY >= cursorY && mouseY < cursorY + boxSize;
            bool clicked = hovered && mouseDown;
            if (clicked) value = !value;
            cursorY += spacingY;
            return clicked;
        }

        public static void NextLine(int pixels = 0)
        {
            cursorY += pixels > 0 ? pixels : spacingY;
        }
    }
}
