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


        // Audio fields for boot chime
        private static bool bootChimePlayed = false;
        private static AudioEngine? audioEngine = null;

        public HomeScreenDisplay()
        {
            GameEnvironment.ShowFps = false;

            // Ensure DebugOverlay exists
            if (Asmo.DebugOverlay.Current == null)
                new Asmo.DebugOverlay();
            Asmo.DebugOverlay.Current?.Show();
            // Play boot chime only once per app run
            if (!bootChimePlayed)
            {
                try
                {
                    if (audioEngine == null)
                        audioEngine = new AudioEngine();

                    // Use default stereo channels for compatibility
                    int sampleRate = AudioClip.DefaultSampleRate;
                    int channels = AudioClip.DefaultChannels;
                    // audioEngine.PlayClip(AudioClip.CreateSquare(523.25, 0.10, 0.25f, sampleRate, channels), "master"); // C5
                    // // Thread.Sleep(150);
                    // audioEngine.PlayClip(AudioClip.CreateSquare(659.25, 0.10, 0.20f, sampleRate, channels), "master"); // E5
                    // // Thread.Sleep(150);
                    // audioEngine.PlayClip(AudioClip.CreateSquare(783.99, 0.18, 0.18f, sampleRate, channels), "master"); // G5
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Boot chime failed: {ex.Message}");
                }
                bootChimePlayed = true;
            }
            Asmo.DebugOverlay.Current?.Toggle();
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
            // Gradient panel background
            framebuffer.DrawVerticalGradientRect(
                panelX, panelY, panelW, panelH,
                new Asmo.Types.Color(16, 16, 48, 220), // top color
                new Asmo.Types.Color(32, 32, 64, 220)  // bottom color
            );

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
            // if (Asmo.Gui.Gui.Button(framebuffer, "Toggle Debug Overlay", Colors.Magenta, mouseX, mouseY, mouseDown))
            // {
            //     Asmo.DebugOverlay.Current?.Toggle();
            // }

            y += 32;

            // Tips area (bottom of panel)
            int tipY = panelY + panelH - 24;
            framebuffer.DrawText(panelX + 20, tipY, tips[tipIndex], Colors.Green);
            if (GameEnvironment.ShowFps)
            {
                framebuffer.DrawText(fpsTextX, fpsTextY, fpsText, Colors.White);
                // Removed manual frame time and FPS graph drawing. DebugOverlay handles this now.
            }
            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
            }
            Asmo.DebugOverlay.Current?.Render(framebuffer);
        }
    }
}
