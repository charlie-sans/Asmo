using OpenTK.Windowing.Common;
using System.Threading;
using Asmo.Sound;
using Asmo.Audio;
using System;
using System.IO;
using Asmo.Window.input;
using Asmo.Gui;
using Asmo.Gfx;
using Asmo.Types;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Microsoft.Xna.Framework;
using CSCore.Tags.ID3.Frames;
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
        private Surface _surface;
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


        // Audio fields for boot chime
        private static bool bootChimePlayed = false;
        private static AudioEngine? audioEngine = null;

        public HomeScreenDisplay()
        {
            GameEnvironment.ShowFps = false;

            // Ensure DebugOverlay exists
            if (Asmo.DebugOverlay.Current == null)
                DebugOverlay.Current = new Asmo.DebugOverlay();
                Console.WriteLine("[HomeScreen] Created DebugOverlay instance.");
                Asmo.DebugOverlay.Current.Show();
            
            // Register custom framegraph
            Asmo.DebugOverlay.Current.RegisterFrameGraph(homeScreenGraph);
            // Play boot chime only once per app run
            if (!bootChimePlayed)
            {
                try
                {
                    if (audioEngine == null)
                        audioEngine = new AudioEngine();
           
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Boot chime failed: {ex.Message}");
                }
                bootChimePlayed = true;
            }
            Asmo.DebugOverlay.Current.Toggle();
        }

        public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer, Mouse mouse)
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
            else if (framebuffer.Window.GetWindowName() != Asmo.Window.Window.CanonicalTitle)
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

            // Cycle tips every 5 seconds
            tipTimer += e.Time;
            if (tipTimer > 5.0)
            {
                tipIndex = (tipIndex + 1) % tips.Length;
                tipTimer = 0;
            }

            // // Draw a border around the framebuffer for scaling debug
            // framebuffer.DrawOutlinedRect(0, 0, framebuffer.Width, framebuffer.Height, Colors.Red);

            // Main Home Panel
            int panelW = 340, panelH = 180;
            int panelX = (framebuffer.Width - panelW) / 2;
            int panelY = (framebuffer.Height - panelH) / 2;
            framebuffer.DrawOutlinedRect(panelX - 4, panelY - 4, panelW + 8, panelH + 8, Colors.Cyan);
            Asmo.DebugOverlay.Current.RegisterDrawCall();
            // Gradient panel background
            framebuffer.DrawVerticalGradientRect(
                panelX, panelY, panelW, panelH,
                new Asmo.Types.Color(16, 16, 48, 220), // top color
                new Asmo.Types.Color(32, 32, 64, 220)  // bottom color
            );
            Asmo.DebugOverlay.Current.RegisterDrawCall();
            // Asmo.DebugOverlay.Current.DrawFpsGraph(framebuffer, 8, framebuffer.Height - 40 - 8, 120, 40);
            // Asmo.DebugOverlay.Current.DrawFrameTimeGraph(framebuffer, 8, framebuffer.Height - 80 - 16, 120, 40);

            int y = panelY + 18;
            framebuffer.DrawText(panelX + 20, y, $"ASMO GAME CONSOLE", Colors.Yellow); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 20;
            framebuffer.DrawText(panelX + 20, y, $"Drop a game file (DLL or ZIP) to play!", Colors.White); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"(Or drag a folder with a game DLL)", Colors.Gray); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"Version: {version}", Colors.Cyan); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"{github}", Colors.Blue); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"GPU Drivers: {GL.GetString(StringName.Version)}", Colors.Blue); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"GPU Name: {GL.GetString(StringName.Vendor)}", Colors.Green); Asmo.DebugOverlay.Current.RegisterDrawCall(); y += 16;

            // --- Framebuffer Resize Test Button ---
            int buttonX = panelX + 20;
            int buttonY = y + 8;
            Asmo.Gui.Gui.Begin(buttonX, buttonY);
            bool mouseDown = mouse != null && framebuffer.Window.IsMouseButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
            // if (Asmo.Gui.Gui.Button(framebuffer, "Toggle Debug Overlay", Colors.Magenta, mouseX, mouseY, mouseDown))
            // {
            //     Asmo.DebugOverlay.Current?.Toggle();
            // }

            y += 32;

            // Tips area (bottom of panel)
            int tipY = panelY + panelH - 24;
            framebuffer.DrawText(panelX + 20, tipY, tips[tipIndex], Colors.Green); Asmo.DebugOverlay.Current?.RegisterDrawCall();
            if (GameEnvironment.ShowFps)
            {
                framebuffer.DrawText(fpsTextX, fpsTextY, fpsText, Colors.White); Asmo.DebugOverlay.Current?.RegisterDrawCall();
                // Removed manual frame time and FPS graph drawing. DebugOverlay handles this now.
            }
            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
                Asmo.DebugOverlay.Current.RegisterDrawCall();
            }
            if (GameEnvironment.ShowFps)
            {
                Asmo.DebugOverlay.Current.Render(framebuffer);
                Asmo.DebugOverlay.Current.Show();
            }
        }
    }
}
