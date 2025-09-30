using Asmo.Gfx;
using AstrOS;
namespace AstrOS;
public class App
{
    public string Name;
    public Action Launch;
    public string? IconPath;
    public App(string name, Action launch, string? iconPath = null)
    {
        Name = name;
        Launch = launch;
        IconPath = iconPath;
    }
}

public static class AppRegistry
{
    public static List<App> Apps = new();
    public static void Register(App app) => Apps.Add(app);
}



public class StartMenu
{
    public bool IsOpen = false;
    public int X = 0, Y = 0, Width = 180, Height = 180;
    public List<(string Text, Action OnClick)> Items = new();
    public List<App> AppItems = new();

    public StartMenu()
    {
        Items.Add(("About", () => { /* To be set by WindowManager */ }));
    }

    public void Draw(Surface surface)
    {
        if (!IsOpen) return;
        int totalItems = AppRegistry.Apps.Count + Items.Count;
        int menuHeight = 16 + totalItems * 32;
        surface.FillRect(X, Y, Width, menuHeight, surface, Colors.DarkGray);
        surface.DrawRect(X, Y, Width, menuHeight, Colors.Black);
        int y = Y + 8;
        // Draw apps
        for (int i = 0; i < AppRegistry.Apps.Count; i++)
        {
            surface.DrawText(X + 12, y + i * 32, AppRegistry.Apps[i].Name, Colors.White);
        }
        // Draw separator
        y += AppRegistry.Apps.Count * 32;
        surface.DrawRect(X + 8, y, Width - 16, 1, Colors.Gray);
        y += 8;
        // Draw static items
        for (int i = 0; i < Items.Count; i++)
        {
            surface.DrawText(X + 12, y + i * 32, Items[i].Text, Colors.White);
        }
    }
    public int HitTest(int mx, int my)
    {
        if (!IsOpen) return -1;
        int totalItems = AppRegistry.Apps.Count + Items.Count;
        int menuHeight = 16 + totalItems * 32;
        if (mx < X || mx > X + Width || my < Y || my > Y + menuHeight) return -1;
        int idx = (my - Y - 8) / 32;
        return (idx >= 0 && idx < totalItems) ? idx : -1;
    }
}

public class Taskbar
    {
    public int Height = 36;
    public int Y = 0;
    public List<AstrOS.BasicWindow> MinimizedWindows = new();
    public StartMenu StartMenu = new();
    public int StartButtonWidth = 80;
    public void Draw(Surface surface, int screenWidth)
    {
        Y = surface.Height - Height;
        // Draw background
        surface.FillRect(0, Y, screenWidth, Height, surface, Colors.DarkGray);
        surface.DrawRect(0, Y, screenWidth, Height, Colors.Black);
        // Draw Start button
        surface.FillRect(0, Y, StartButtonWidth, Height, surface, Colors.Gray);
        surface.DrawRect(0, Y, StartButtonWidth, Height, Colors.White);
        surface.DrawText(16, Y + 10, "Start", Colors.White);
        // Draw window buttons
        int x = StartButtonWidth + 8;
        foreach (var win in MinimizedWindows)
        {
            int btnW = 120, btnH = 28;
            surface.FillRect(x, Y + 4, btnW, btnH, surface, Colors.Gray);
            surface.DrawRect(x, Y + 4, btnW, btnH, Colors.White);
            surface.DrawText(x + 8, Y + 10, win.Title, Colors.White);
            x += btnW + 8;
        }
        StartMenu.Draw(surface);
    }
    public AstrOS.BasicWindow? HitTest(int mx, int my)
    {
        int x = StartButtonWidth + 8;
        foreach (var win in MinimizedWindows)
        {
            int btnW = 120, btnH = 28;
            if (mx >= x && mx < x + btnW && my >= Y + 4 && my < Y + 4 + btnH)
                return win;
            x += btnW + 8;
        }
        return null;
    }
    public bool IsOnStartButton(int mx, int my)
    {
        return mx >= 0 && mx < StartButtonWidth && my >= Y && my < Y + Height;
    }
    }


    public class ContextMenu
    {
        public class MenuItem
        {
            public string Text;
            public Action? OnClick;
            public MenuItem(string text, Action? onClick)
            {
                Text = text;
                OnClick = onClick;
            }
        }
        public List<MenuItem> Items = new();
        public int X, Y;
        public bool Visible = false;
        public BasicWindow? TargetWindow;

        public void Show(int x, int y, BasicWindow win)
        {
            X = x; Y = y; TargetWindow = win; Visible = true;
        }
        public void Hide() => Visible = false;

        public void Draw(Surface surface)
        {
            if (!Visible || Items.Count == 0) return;
            int w = 120, h = 28 * Items.Count;
            surface.FillRect(X, Y, w, h, surface, Colors.DarkGray);
            surface.DrawRect(X, Y, w, h, Colors.Black);
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                surface.DrawText(X + 8, Y + 6 + i * 28, item.Text, Colors.White);
            }
        }
        public int HitTest(int mx, int my)
        {
            if (!Visible) return -1;
            int w = 120, h = 28 * Items.Count;
            if (mx < X || mx > X + w || my < Y || my > Y + h) return -1;
            int idx = (my - Y) / 28;
            return (idx >= 0 && idx < Items.Count) ? idx : -1;
        }
    }

    public class BasicWindow
    {
        public int X, Y, Width, Height;
        public string Title;
        public bool IsFocused;
        public Action<Surface, BasicWindow>? DrawContent;
        public bool IsDragging = false;
        public int DragOffsetX, DragOffsetY;
        public bool IsClosed = false;
        public bool IsMinimized = false;

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

        private ContextMenu contextMenu = new();
        public bool IsContextMenuVisible => contextMenu.Visible;
        public Taskbar Taskbar = new();

        public WindowManager()
        {
            contextMenu.Items.Add(new ContextMenu.MenuItem("Bring to Front", () =>
            {
                if (contextMenu.TargetWindow != null)
                    BringToFront(contextMenu.TargetWindow);
                contextMenu.Hide();
            }));
            contextMenu.Items.Add(new ContextMenu.MenuItem("Minimize", () =>
            {
                if (contextMenu.TargetWindow != null)
                {
                    MinimizeWindow(contextMenu.TargetWindow);
                }
                contextMenu.Hide();
            }));
            contextMenu.Items.Add(new ContextMenu.MenuItem("Close", () =>
            {
                if (contextMenu.TargetWindow != null)
                    CloseWindow(contextMenu.TargetWindow);
                contextMenu.Hide();
            }));
        }
        public void MinimizeWindow(BasicWindow win)
        {
            win.IsMinimized = true;
            Taskbar.MinimizedWindows.Add(win);
        }
        public void RestoreWindow(BasicWindow win)
        {
            win.IsMinimized = false;
            Taskbar.MinimizedWindows.Remove(win);
            BringToFront(win);
        }

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
            // Remove closed windows from taskbar
            Taskbar.MinimizedWindows.RemoveAll(w => w.IsClosed);
        }
        public void DrawAll(Surface surface)
        {
            foreach (var win in Windows)
                if (!win.IsMinimized)
                    win.Draw(surface);
            contextMenu.Draw(surface);
            Taskbar.Draw(surface, surface.Width);
        }
        public void BringToFront(BasicWindow win)
        {
            if (Windows.Remove(win))
                Windows.Add(win);
        }

        // Call this from your main Update method, passing mouse state
        // rightDown should be 'just pressed', not 'held'.
        public void HandleMouseInput(int mx, int my, bool leftDown, bool rightJustPressed, bool leftReleased, bool rightReleased)
        {
            // Start button click
            if (leftReleased && Taskbar.IsOnStartButton(mx, my))
            {
                Taskbar.StartMenu.IsOpen = !Taskbar.StartMenu.IsOpen;
                if (Taskbar.StartMenu.IsOpen)
                {
                    Taskbar.StartMenu.X = 0;
                    Taskbar.StartMenu.Y = Taskbar.Y - Taskbar.StartMenu.Height;
                }
                return;
            }
            // Start menu item click
            if (Taskbar.StartMenu.IsOpen && leftReleased)
            {
                int idx = Taskbar.StartMenu.HitTest(mx, my);
                if (idx >= 0)
                {
                    if (idx < AppRegistry.Apps.Count)
                    {
                        AppRegistry.Apps[idx].Launch?.Invoke();
                        Taskbar.StartMenu.IsOpen = false;
                        return;
                    }
                    else
                    {
                        int staticIdx = idx - AppRegistry.Apps.Count;
                        if (staticIdx >= 0 && staticIdx < Taskbar.StartMenu.Items.Count)
                        {
                            Taskbar.StartMenu.Items[staticIdx].OnClick?.Invoke();
                            Taskbar.StartMenu.IsOpen = false;
                            return;
                        }
                    }
                }
                // Click outside closes menu
                int totalItems = AppRegistry.Apps.Count + Taskbar.StartMenu.Items.Count;
                int menuHeight = 16 + totalItems * 32;
                if (!(mx >= Taskbar.StartMenu.X && mx < Taskbar.StartMenu.X + Taskbar.StartMenu.Width && my >= Taskbar.StartMenu.Y && my < Taskbar.StartMenu.Y + menuHeight))
                {
                    Taskbar.StartMenu.IsOpen = false;
                }
            }
            // Restore window from taskbar
            if (leftReleased)
            {
                var win = Taskbar.HitTest(mx, my);
                if (win != null)
                {
                    RestoreWindow(win);
                    return;
                }
            }
            // Right click: open context menu for topmost window under mouse (only on just pressed)
            if (rightJustPressed && !contextMenu.Visible)
            {
                for (int i = Windows.Count - 1; i >= 0; i--)
                {
                    var win = Windows[i];
                    if (win.IsPointInside(mx, my))
                    {
                        contextMenu.Show(mx, my, win);
                        break;
                    }
                }
            }
            // Left click: select menu item if menu is open
            if (contextMenu.Visible && leftReleased)
            {
                int idx = contextMenu.HitTest(mx, my);
                if (idx >= 0)
                {
                    contextMenu.Items[idx].OnClick?.Invoke();
                }
                else
                {
                    contextMenu.Hide();
                }
            }
            // Click elsewhere closes menu
            if (contextMenu.Visible && rightReleased)
            {
                contextMenu.Hide();
            }
        }
    }

