using System;
using Asmo.Gfx;
using Asmo.Types;
namespace Asmo.Window.HomeScreen
{
    public class HomeScreenDisplay
    {
        // For framebuffer resize test button
        private bool resizeToggled = false;
        private bool showSettings = false;
        private bool showAbout = false;
        private string sampleImagePath = "Assets/sample.png";
        private string version = "v1.0.0";
        private string author = "Charlie Sans";
        private string github = "https://github.com/charlie-sans/Asmo";
        private string description = "A modern C#/.NET retro/modern game console framework.";
    private Surface? _surface;
        private string[] tips = new[]
        {
            "Tip: Use the Settings Panel to change quality modes!",
            "Tip: Drop a DLL or ZIP to load a game.",
            "Tip: Resize the window to see scaling in action.",
            "Tip: Try Enhanced/HD modes for more features!"
        };
        private int tipIndex = 0;
        private double tipTimer = 0;

    // Framegraph state
    private Asmo.Debug.FrameGraph homeScreenGraph = new Asmo.Debug.FrameGraph(120) { Label = "HomeScreen ms", GraphColor = Asmo.Gfx.Colors.Magenta };

        // Rolling perf stats (simple running window of last 240 frames)
        private const int PerfWindow = 240;
        private readonly float[] perfSamples = new float[PerfWindow];
        private int perfIndex = 0;
        private int perfCount = 0;
        private float perfMinMs = float.MaxValue;
        private float perfMaxMs = 0f;
        private float perfSumMs = 0f;

        private void AccumulatePerf(float frameMs)
        {
            // Remove old sample from sum when buffer full
            if (perfCount == PerfWindow)
            {
                float old = perfSamples[perfIndex];
                perfSumMs -= old;
                // If old was min or max we'll recompute lazily below
                if (old == perfMinMs || old == perfMaxMs)
                {
                    // Recompute min/max across buffer
                    float nMin = float.MaxValue, nMax = 0f;
                    for (int i = 0; i < perfCount; i++)
                    {
                        float v = perfSamples[i];
                        if (v < nMin) nMin = v;
                        if (v > nMax) nMax = v;
                    }
                    perfMinMs = nMin;
                    perfMaxMs = nMax;
                }
            }
            else
            {
                perfCount++;
            }
            perfSamples[perfIndex] = frameMs;
            perfIndex = (perfIndex + 1) % PerfWindow;
            perfSumMs += frameMs;
            if (frameMs < perfMinMs) perfMinMs = frameMs;
            if (frameMs > perfMaxMs) perfMaxMs = frameMs;
        }

        private float PerfAvgMs => perfCount > 0 ? perfSumMs / perfCount : 0f;

        private void ResetPerf()
        {
            for (int i = 0; i < perfSamples.Length; i++) perfSamples[i] = 0f;
            perfIndex = 0; perfCount = 0; perfMinMs = float.MaxValue; perfMaxMs = 0f; perfSumMs = 0f;
        }

        // Simple local UI button (lightweight to avoid deeper Gui layout mixing here)
        private bool DrawSimpleButton(Surface surface, string text, int x, int y, int mouseX, int mouseY, bool mouseDown, out int w)
        {
            w = text.Length * 7 + 24; int h = 20;
            bool hover = mouseX >= x && mouseX < x + w && mouseY >= y && mouseY < y + h;
            bool pressEdge = hover && mouseDown;
            var bg = hover ? Colors.Blue : new Asmo.Types.Color(20, 20, 40, 200);
            // fill
            for (int py = 0; py < h; py++)
                for (int px = 0; px < w; px++)
                    surface.SetPixel(x + px, y + py, bg);
            surface.DrawOutlinedRect(x, y, w, h, Colors.Cyan);
            surface.DrawText(x + (w - text.Length * 7) / 2, y + 4, text, Colors.White);
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            return pressEdge;
        }


        // Audio fields for boot chime
        private static bool bootChimePlayed = false;
    // TODO: reintegrate AudioEngine boot chime via Raylib (removed OpenTK/XNA remnants)

        public HomeScreenDisplay()
        {
            // TODO: remember to change this between true/false for debugging
            GameEnvironment.ShowFps = false;

            // Ensure DebugOverlay exists
            if (Asmo.DebugOverlay.Current != null)
            {
                Asmo.DebugOverlay.Current.RegisterFrameGraph(homeScreenGraph);
                // Asmo.DebugOverlay.Current?.Show();
            }
            // Play boot chime only once per app run
            if (!bootChimePlayed) bootChimePlayed = true; // boot chime temporarily disabled
            // Do not toggle—visibility controlled centrally
        }

    // Legacy OpenTK RenderHomeScreen removed in aggressive cleanup.
    /* public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer, Mouse mouse)
        {
            if (_surface == null || _surface != framebuffer)
            {
                _surface = framebuffer;
                Console.WriteLine($"[HomeScreen] Surface assigned in RenderHomeScreen. Size: {_surface.Width}x{_surface.Height}");
            }
            // Only enforce canonical title if still at home screen and not already changed by a loaded game.
            if (!GameEnvironment.WindowTitle.Equals(Asmo.Window.Window.CanonicalTitle, StringComparison.Ordinal))
            {
                // Respect externally set title (e.g., a game) – do not overwrite.
            }
            else if (framebuffer.Window != null && framebuffer.Window.GetWindowName() != Asmo.Window.Window.CanonicalTitle)
            {
                System.Diagnostics.Debug.WriteLine($"[HomeScreen] Restoring canonical title '{Asmo.Window.Window.CanonicalTitle}' (was '{framebuffer.Window.GetWindowName()}')");
                framebuffer.Window.SetWindowTitle(Asmo.Window.Window.CanonicalTitle);
            }
            // --- Raw FPS display ---
            float frameMs = (float)(e.Time * 1000.0);
            homeScreenGraph.AddSample(frameMs);
            float rawFps = frameMs > 0.01f ? 1000.0f / frameMs : 0f;
            string fpsText = $"FPS: {rawFps:F1}";
            int fpsTextX = (framebuffer.Width - 80) / 2;
            int fpsTextY = framebuffer.Height - (40 * 2) - 32; // above the graphs
            int mouseX = (int)mouse.X;
            int mouseY = (int)mouse.Y;

            // Accumulate perf for Performance tab
            AccumulatePerf(frameMs);

            // Cycle tips every 5 seconds
            tipTimer += e.Time;
            if (tipTimer > 5.0)
            {
                tipIndex = (tipIndex + 1) % tips.Length;
                tipTimer = 0;
            }

            // // Draw a border around the framebuffer for scaling debug
            // framebuffer.DrawOutlinedRect(0, 0, framebuffer.Width, framebuffer.Height, Colors.Red);

            // Main Panel with Tabs
            int panelW = 480, panelH = 260;
            int panelX = (framebuffer.Width - panelW) / 2;
            int panelY = (framebuffer.Height - panelH) / 2;
            framebuffer.DrawOutlinedRect(panelX - 4, panelY - 4, panelW + 8, panelH + 8, Colors.Cyan);
            Asmo.DebugOverlay.Current?.RegisterDrawCall();
            framebuffer.DrawVerticalGradientRect(
                panelX, panelY, panelW, panelH,
                new Asmo.Types.Color(16, 16, 48, 220),
                new Asmo.Types.Color(32, 32, 64, 220)
            );
            Asmo.DebugOverlay.Current?.RegisterDrawCall();

            // GUI begin inside panel
            bool mouseDown = mouse != null && framebuffer.Window != null && framebuffer.Window.IsMouseButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
            Asmo.Gui.Gui.Begin(panelX + 16, panelY + 16);

            // Tabs across top
            string[] tabs = new [] { "Home", "System", "Performance", "About" };
            int selected = Asmo.Gui.Gui.Tabs(framebuffer, "home_screen_tabs", tabs, mouseX, mouseY, mouseDown);

            // Content area origin after tabs
            switch (selected)
            {
                case 0: // Home
                    framebuffer.DrawText(panelX + 24, Asmo.Gui.Gui.Theme.ItemSpacingY + panelY + 36 - 8, "ASMO GAME CONSOLE", Colors.Yellow); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 64, "Drop a game file (DLL or ZIP) to play!", Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 82, "(Or drag a folder with a game DLL)", Colors.Gray); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 104, $"Tip: {tips[tipIndex]}", Colors.Green); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    break;
                case 1: // System
                    framebuffer.DrawText(panelX + 24, panelY + 48, $"Version: {version}", Colors.Cyan); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 66, $"GPU Driver: {GL.GetString(StringName.Version)}", Colors.Blue); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 84, $"GPU Vendor: {GL.GetString(StringName.Vendor)}", Colors.Green); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 102, $"Resolution: {framebuffer.Width}x{framebuffer.Height}", Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    break;
                case 2: // Performance
                    framebuffer.DrawText(panelX + 24, panelY + 48, $"Frame: {frameMs:F2} ms ({rawFps:F1} fps)", Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    // Potentially draw graphs inline later
                    break;
                case 3: // About
                    framebuffer.DrawText(panelX + 24, panelY + 48, $"Author: {author}", Colors.Cyan); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 66, $"GitHub: {github}", Colors.Blue); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    framebuffer.DrawText(panelX + 24, panelY + 84, description, Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                    break;
            }

            // Performance tab content augmentation
            if (selected == 2)
            {
                // Inline graphs (reuse DebugOverlay drawing helpers if available)
                int graphX = panelX + 24;
                int graphY = panelY + 72;
                int graphW = 180;
                int graphH = 60;
                Asmo.DebugOverlay.Current?.DrawFrameTimeGraph(framebuffer, graphX, graphY, graphW, graphH);
                Asmo.DebugOverlay.Current?.RegisterDrawCall();
                Asmo.DebugOverlay.Current?.DrawFpsGraph(framebuffer, graphX, graphY + graphH + 8, graphW, graphH);
                Asmo.DebugOverlay.Current?.RegisterDrawCall();
                // Stats summary
                framebuffer.DrawText(panelX + 220, panelY + 48, $"Min: {perfMinMs:F2} ms", Colors.Green); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                framebuffer.DrawText(panelX + 220, panelY + 64, $"Avg: {PerfAvgMs:F2} ms", Colors.Cyan); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                framebuffer.DrawText(panelX + 220, panelY + 80, $"Max: {perfMaxMs:F2} ms", Colors.Red); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                // Action buttons
                int actY = panelY + 152;
                int actX = panelX + 24;
                int bw;
                if (DrawSimpleButton(framebuffer, "Clear Stats", actX, actY, mouseX, mouseY, mouseDown, out bw))
                    ResetPerf();
                actX += bw + 12;
                if (DrawSimpleButton(framebuffer, "Toggle Overlay", actX, actY, mouseX, mouseY, mouseDown, out bw))
                    Asmo.DebugOverlay.Current?.Toggle();
                if (GameEnvironment.ShowFps)
                {
                    string badge = $"{rawFps:F0} FPS";
                    int bx = panelX + panelW - (badge.Length * 7) - 24;
                    int by = panelY + 20;
                    framebuffer.DrawText(bx, by, badge, Colors.White);
                    Asmo.DebugOverlay.Current?.RegisterDrawCall();
                }
            }

            // Enhanced Home tab features
            if (selected == 0)
            {
                int actionsY = panelY + 140;
                int actionsX = panelX + 24;
                int bw;
                if (DrawSimpleButton(framebuffer, "Toggle Overlay", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw))
                {
                    Asmo.DebugOverlay.Current?.Toggle();
                }
                actionsX += bw + 12;
                if (DrawSimpleButton(framebuffer, "Settings", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw))
                {
                    showSettings = !showSettings; // placeholder
                }
                actionsX += bw + 12;
                if (DrawSimpleButton(framebuffer, "Exit", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw))
                {
                    framebuffer.Window?.Close();
                }
                // Recent games list
                var entries = Asmo.Window.GameHistory.Entries;
                framebuffer.DrawText(panelX + 24, panelY + 168, "Recent Games:", Colors.Cyan); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                if (entries.Count == 0)
                {
                    framebuffer.DrawText(panelX + 24, panelY + 186, "(none yet)", Colors.Gray); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                }
                else
                {
                    int ry = panelY + 186;
                    int shown = 0;
                    foreach (var g in entries)
                    {
                        if (shown >= 5) break; // show top 5
                        string line = $"{g.DisplayName}  [{g.LastPlayed.ToLocalTime():HH:mm}]";
                        framebuffer.DrawText(panelX + 24, ry, line, Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                        ry += 16; shown++;
                    }
                }
            }

            // End of GUI frame (layout resets next call)

            // (Global FPS badge removed from non-performance tabs per request to move perf items)
            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
                Asmo.DebugOverlay.Current?.RegisterDrawCall();
            }
            if (GameEnvironment.ShowFps)
            {
                Asmo.DebugOverlay.Current?.Render(framebuffer);
                // Asmo.DebugOverlay.Current?.Show();
            }
    } */

        // Lightweight overload for Raylib path (no OpenTK FrameEventArgs or Mouse class available)
        public void RenderHomeScreenRaylib(double deltaSeconds, Gfx.Surface framebuffer, int mouseX, int mouseY, bool mouseDown)
        {
            // Raylib path uses simplified minimal renderer (no OpenTK types).
            var adapter = new RaylibMouseAdapter(mouseX, mouseY, mouseDown);
            RenderHomeScreenMinimal(deltaSeconds, framebuffer, adapter);
        }

        private record struct RaylibMouseAdapter(int X, int Y, bool Down);

        // Extracted reduced version of RenderHomeScreen that uses adapter instead of full Mouse.
    private void RenderHomeScreenMinimal(double deltaSeconds, Gfx.Surface framebuffer, RaylibMouseAdapter mouse)
        {
            framebuffer.Clear(Colors.Black);
            if (_surface == null || _surface != framebuffer)
                _surface = framebuffer;
            // Timing / perf
            float frameMs = (float)(deltaSeconds * 1000.0);
            homeScreenGraph.AddSample(frameMs);
            float rawFps = frameMs > 0.01f ? 1000.0f / frameMs : 0f;
            AccumulatePerf(frameMs);
            tipTimer += deltaSeconds; if (tipTimer > 5.0) { tipIndex = (tipIndex + 1) % tips.Length; tipTimer = 0; }
            int panelW = 480, panelH = 260;
            int panelX = (framebuffer.Width - panelW) / 2;
            int panelY = (framebuffer.Height - panelH) / 2;
            framebuffer.DrawOutlinedRect(panelX - 4, panelY - 4, panelW + 8, panelH + 8, Colors.Cyan);
            framebuffer.DrawVerticalGradientRect(panelX, panelY, panelW, panelH, new Asmo.Types.Color(16,16,48,220), new Asmo.Types.Color(32,32,64,220));
            bool mouseDown = mouse.Down;
            int mouseX = mouse.X; int mouseY = mouse.Y;
            Asmo.Gui.Gui.Begin(panelX + 16, panelY + 16);
            string[] tabs = new [] { "Home", "System", "Performance", "About" };
            int selected = Asmo.Gui.Gui.Tabs(framebuffer, "home_screen_tabs", tabs, mouseX, mouseY, mouseDown);
            switch(selected)
            {
                case 0:
                    framebuffer.DrawText(panelX + 24, panelY + 28, "ASMO GAME CONSOLE", Colors.Yellow);
                    framebuffer.DrawText(panelX + 24, panelY + 52, "Drop a game file (DLL or ZIP) to play!", Colors.White);
                    framebuffer.DrawText(panelX + 24, panelY + 68, "(Or drag a folder with a game DLL)", Colors.Gray);
                    framebuffer.DrawText(panelX + 24, panelY + 88, $"Tip: {tips[tipIndex]}", Colors.Green);
                    break;
                case 2:
                    framebuffer.DrawText(panelX + 24, panelY + 48, $"Frame: {frameMs:F2} ms ({rawFps:F1} fps)", Colors.White);
                    framebuffer.DrawText(panelX + 220, panelY + 48, $"Min: {perfMinMs:F2} ms", Colors.Green);
                    framebuffer.DrawText(panelX + 220, panelY + 64, $"Avg: {PerfAvgMs:F2} ms", Colors.Cyan);
                    framebuffer.DrawText(panelX + 220, panelY + 80, $"Max: {perfMaxMs:F2} ms", Colors.Red);
                    break;
                case 3:
                    framebuffer.DrawText(panelX + 24, panelY + 48, $"Author: {author}", Colors.Cyan);
                    framebuffer.DrawText(panelX + 24, panelY + 64, $"GitHub: {github}", Colors.Blue);
                    framebuffer.DrawText(panelX + 24, panelY + 80, description, Colors.White);
                    break;
            }
            // Home tab buttons (subset)
            if (selected == 0)
            {
                int actionsY = panelY + 120; int actionsX = panelX + 24; int bw;
                if (DrawSimpleButton(framebuffer, "Toggle Overlay", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw)) Asmo.DebugOverlay.Current?.Toggle();
                actionsX += bw + 12;
                if (DrawSimpleButton(framebuffer, "Settings", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw)) showSettings = !showSettings;
                actionsX += bw + 12;
                if (DrawSimpleButton(framebuffer, "Exit", actionsX, actionsY, mouseX, mouseY, mouseDown, out bw)) { /* exit handled externally */ }
            }
            // Mouse cursor box
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
        }
    }
}
