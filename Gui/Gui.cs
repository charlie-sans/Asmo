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

        public static void Label(Surface surface, string text, Color color)
        {
            surface.DrawText(cursorX, cursorY, text, color);
            cursorY += spacingY;
        }

        public static bool Button(Surface surface, string text, Color color, int mouseX, int mouseY, bool mouseDown)
        {
            int w = text.Length * 7 + 8;
            int h = 18;
            surface.DrawOutlinedRect(cursorX, cursorY, w, h, color);
            surface.DrawText(cursorX + 4, cursorY + 4, text, color);
            bool hovered = mouseX >= cursorX && mouseX < cursorX + w && mouseY >= cursorY && mouseY < cursorY + h;
            bool clicked = hovered && mouseDown;
            cursorY += spacingY;
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
