using OpenTK.Windowing.Common;
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
        private const int FrameGraphSamples = 120;
        private float[] frameTimes = new float[FrameGraphSamples];
        private int frameTimeIndex = 0;

        public HomeScreenDisplay()
        {
            GameEnvironment.ShowFps = false;
            
        }

    public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer, Mouse mouse)
        {
            // --- Raw FPS display ---
            float lastMs = frameTimes[(frameTimeIndex - 1 + FrameGraphSamples) % FrameGraphSamples];
            float rawFps = lastMs > 0.01f ? 1000.0f / lastMs : 0f;
            string fpsText = $"FPS: {rawFps:F1}";
            int fpsTextX = (framebuffer.Width - 80) / 2;
            int fpsTextY = framebuffer.Height - (40 * 2) - 32; // above the graphs
            // Console.WriteLine(fpsText);
            // Use Mouse class for mouse state (passed in)
            int mouseX = (int)mouse.X;
            int mouseY = (int)mouse.Y;

            // --- Framegraph update ---
            // Store the latest frame time (in ms)
            frameTimes[frameTimeIndex] = (float)(e.Time * 1000.0); // ms
            frameTimeIndex = (frameTimeIndex + 1) % FrameGraphSamples;

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
            framebuffer.DrawRect(panelX, panelY, panelW, panelH, new Asmo.Types.Color(16, 16, 32, 220));

            int y = panelY + 18;
            framebuffer.DrawText(panelX + 20, y, $"ASMO GAME CONSOLE", Colors.Yellow); y += 20;
            framebuffer.DrawText(panelX + 20, y, $"Drop a game file (DLL or ZIP) to play!", Colors.White); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"(Or drag a folder with a game DLL)", Colors.Gray); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"Version: {version}", Colors.Cyan); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"{github}", Colors.Blue); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"GPU Drivers: {GL.GetString(StringName.Version)}", Colors.Blue); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"GPU Name: {GL.GetString(StringName.Vendor)}", Colors.Green); y += 16;

            // --- Framebuffer Resize Test Button ---
            int buttonX = panelX + 20;
            int buttonY = y + 8;
            Asmo.Gui.Gui.Begin(buttonX, buttonY);
            bool mouseDown = mouse != null && framebuffer.Window.IsMouseButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
            int altW = 320, altH = 180;
            if (Asmo.Gui.Gui.Button(framebuffer, "Framebuffer Resize Test", Colors.Magenta, mouseX, mouseY, mouseDown))
            {
                var window = framebuffer.Window;
                if (window is Asmo.Window.Window win2)
                {
                    if (!resizeToggled)
                        win2.ResizeFrameBuffer(altW, altH);
                    else
                        win2.ResizeFrameBuffer(GameEnvironment.ScreenWidth, GameEnvironment.ScreenHeight);
                    resizeToggled = !resizeToggled;
                }
            }

            y += 32;

            // Tips area (bottom of panel)
            int tipY = panelY + panelH - 24;
            framebuffer.DrawText(panelX + 20, tipY, tips[tipIndex], Colors.Green);
            if (GameEnvironment.ShowFps)
            {
            framebuffer.DrawText(fpsTextX, fpsTextY, fpsText, Colors.White);
            // --- Frame time graph (ms) ---
            int graphWidth = FrameGraphSamples;
            int graphHeight = 40;
            int graphX = (framebuffer.Width - graphWidth) / 2;
            int graphY = framebuffer.Height - (graphHeight * 2) - 16;
            float maxMs = 33.3f; // 30 FPS = 33.3ms, cap graph at this
            // Draw background
            framebuffer.DrawRect(graphX - 2, graphY - 2, graphWidth + 4, graphHeight + 4, new Asmo.Types.Color(0, 0, 0, 180));
            // Draw bars
            for (int i = 0; i < FrameGraphSamples; i++)
            {
                int idx = (frameTimeIndex + i) % FrameGraphSamples;
                float ms = frameTimes[idx];
                int barH = (int)Math.Min((ms / maxMs) * (graphHeight - 4), graphHeight - 4);
                int barY = graphY + (graphHeight - 4 - barH) + 2;
                int barX = graphX + i;
                var color = ms < 16.7f ? Colors.Green : (ms < 25f ? Colors.Yellow : Colors.Red);
                framebuffer.DrawRect(barX, barY, 1, barH, color);
            }
            // Draw axis line
            framebuffer.DrawRect(graphX, graphY + graphHeight - 2, graphWidth, 1, Colors.Gray);
            // Draw labels
            framebuffer.DrawText(graphX + 4, graphY + 4, $"Frame ms", Colors.White);
            framebuffer.DrawText(graphX + 4, graphY + 16, $"16.7ms (60fps)", Colors.Green);
            framebuffer.DrawText(graphX + 4, graphY + 28, $"33.3ms (30fps)", Colors.Red);

            // --- FPS graph ---
            int fpsGraphHeight = 40;
            int fpsGraphX = graphX;
            int fpsGraphY = graphY + graphHeight + 8;
            float maxFps = 120f;
            framebuffer.DrawRect(fpsGraphX - 2, fpsGraphY - 2, graphWidth + 4, fpsGraphHeight + 4, new Asmo.Types.Color(0, 0, 0, 180));
            for (int i = 0; i < FrameGraphSamples; i++)
            {
                int idx = (frameTimeIndex + i) % FrameGraphSamples;
                float ms = frameTimes[idx];
                float fps = ms > 0.01f ? 1000.0f / ms : maxFps;
                int barH = (int)Math.Min((fps / maxFps) * (fpsGraphHeight - 4), fpsGraphHeight - 4);
                int barY = fpsGraphY + (fpsGraphHeight - 4 - barH) + 2;
                int barX = fpsGraphX + i;
                var color = fps > 60f ? Colors.Green : (fps > 30f ? Colors.Yellow : Colors.Red);
                framebuffer.DrawRect(barX, barY, 1, barH, color);
            }
            framebuffer.DrawRect(fpsGraphX, fpsGraphY + fpsGraphHeight - 2, graphWidth, 1, Colors.Gray);
            framebuffer.DrawText(fpsGraphX + 4, fpsGraphY + 4, $"FPS", Colors.White);
            framebuffer.DrawText(fpsGraphX + 4, fpsGraphY + 16, $"60 FPS", Colors.Green);
            framebuffer.DrawText(fpsGraphX + 4, fpsGraphY + 28, $"30 FPS", Colors.Red);
            }
            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
            }
        }
    }
}
