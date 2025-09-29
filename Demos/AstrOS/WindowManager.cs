
using Asmo.Gfx;
using Asmo.Types;
using System;
using System.Collections.Generic;

namespace AstrOS
{
    public class BasicWindow
    {
        public int X, Y, Width, Height;
        public string Title;
        public bool IsFocused;
        public Action<Surface, BasicWindow>? DrawContent;
        public bool IsDragging = false;
        public int DragOffsetX, DragOffsetY;
        public bool IsClosed = false;

        public BasicWindow(int x, int y, int w, int h, string title, Action<Surface, BasicWindow>? drawContent = null)
        {
            X = x; Y = y; Width = w; Height = h; Title = title; DrawContent = drawContent;
        }
       
        public void Draw(Surface surface)
        {
            // Draw window background and frame using 9-slice if available
            var frame = AstrOS.WindowSprites.FrameSprite;
            int border = 8; // border thickness in pixels
            if (frame != null && frame.Width >= 24 && frame.Height >= 24)
            {
                // 9-slice: corners 8x8, edges stretch, center fill
                // Top-left
                surface.DrawSpriteRegion(frame, X, Y, 0, 0, border, border);
                // Top-right
                surface.DrawSpriteRegion(frame, X + Width - border, Y, frame.Width - border, 0, border, border);
                // Bottom-left
                surface.DrawSpriteRegion(frame, X, Y + Height - border, 0, frame.Height - border, border, border);
                // Bottom-right
                surface.DrawSpriteRegion(frame, X + Width - border, Y + Height - border, frame.Width - border, frame.Height - border, border, border);

                // Top edge
                for (int xBar = X + border; xBar < X + Width - border; xBar += border)
                    surface.DrawSpriteRegion(frame, xBar, Y, border, 0, border, border);
                // Bottom edge
                for (int xBar = X + border; xBar < X + Width - border; xBar += border)
                    surface.DrawSpriteRegion(frame, xBar, Y + Height - border, border, frame.Height - border, border, border);
                // Left edge
                for (int yBar = Y + border; yBar < Y + Height - border; yBar += border)
                    surface.DrawSpriteRegion(frame, X, yBar, 0, border, border, border);
                // Right edge
                for (int yBar = Y + border; yBar < Y + Height - border; yBar += border)
                    surface.DrawSpriteRegion(frame, X + Width - border, yBar, frame.Width - border, border, border, border);

                // Center fill
                for (int xBar = X + border; xBar < X + Width - border; xBar += border)
                    for (int yBar = Y + border; yBar < Y + Height - border; yBar += border)
                        surface.DrawSpriteRegion(frame, xBar, yBar, border, border, border, border);
            }
            else
            {
                // Fallback: fill window with color and draw border
                surface.FillRect(X, Y, Width, Height, surface, Colors.DarkGray);
                surface.DrawRect(X, Y, Width, Height, IsFocused ? Colors.Yellow : Colors.White);
            }

            // Draw title bar (sprite or color) after frame, before text/buttons
            if (AstrOS.WindowSprites.TitleBarSprite != null)
            {
                var sprite = AstrOS.WindowSprites.TitleBarSprite;
                int spriteW = sprite.Width;
                int yBar = Y;
                for (int xBar = X; xBar < X + Width; xBar += spriteW)
                {
                    int drawW = Math.Min(spriteW, X + Width - xBar);
                    if (drawW == spriteW)
                        surface.DrawSprite(sprite, xBar, yBar);
                }
            }
            else
            {
                surface.DrawRect(X, Y, Width, 22, Colors.Gray);
            }

            // Draw title text
            surface.DrawText(X + 4, Y + 2, Title, Colors.White);
            // Draw close button (top right)
            surface.FillRect(X + Width - 18, Y + 2, 16, 16, surface, Colors.Red);
            surface.DrawText(X + Width - 15, Y + 2, "X", Colors.White);
            // Draw content area
            DrawContent?.Invoke(surface, this);
        }

        public bool IsPointInside(int px, int py)
        {
            return px >= X && px < X + Width && py >= Y && py < Y + Height;
        }
        public bool IsOnTitleBar(int px, int py)
        {
            return px >= X && px < X + Width && py >= Y && py < Y + 22;
        }
        public bool IsOnCloseButton(int px, int py)
        {
            return px >= X + Width - 18 && px < X + Width - 2 && py >= Y + 2 && py < Y + 18;
        }
    }

    public class WindowManager
    {
        public List<BasicWindow> Windows = new();
        public BasicWindow? FocusedWindow => Windows.Count > 0 ? Windows[Windows.Count - 1] : null;

        public void AddWindow(BasicWindow win)
        {
            Windows.Add(win);
        }
        public void CloseWindow(BasicWindow win)
        {
            win.IsClosed = true;
        }
        public void Update()
        {
            Windows.RemoveAll(w => w.IsClosed);
        }
        public void DrawAll(Surface surface)
        {
            foreach (var win in Windows)
                win.Draw(surface);
        }
        public void BringToFront(BasicWindow win)
        {
            if (Windows.Remove(win))
                Windows.Add(win);
        }
    }
}
