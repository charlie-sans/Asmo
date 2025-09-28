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
namespace Asmo.Window.HomeScreen
{
    public class HomeScreenDisplay
    {
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

        public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer)
        {
            var (mouseX, mouseY, mousePressed) = Asmo.GuiSetup.GetMouseState(framebuffer);
            var window = framebuffer.Window;
            // Minimal version: no config or feature summary
            string qualityText = "Quality: Retro";
            string featuresText = "Basic";

            // Cycle tips every 5 seconds
            tipTimer += e.Time;
            if (tipTimer > 5.0)
            {
                tipIndex = (tipIndex + 1) % tips.Length;
                tipTimer = 0;
            }

            // Main Info Panel
            int panelX = 32, panelY = 32, panelW = 320, panelH = 180;
            framebuffer.DrawOutlinedRect(panelX - 4, panelY - 4, panelW + 8, panelH + 8, Colors.Cyan);
            framebuffer.DrawRect(panelX, panelY, panelW, panelH, new Asmo.Types.Color(16, 16, 32, 220));

            // Draw a red border around the entire framebuffer for scaling debug
            framebuffer.DrawOutlinedRect(0, 0, framebuffer.Width, framebuffer.Height, Colors.Red);

            int y = panelY + 12;
            framebuffer.DrawText(panelX + 12, y, $"ASMO GAME CONSOLE {version}", Colors.Yellow); y += 16;
            framebuffer.DrawText(panelX + 12, y, description, Colors.White); y += 16;
            framebuffer.DrawText(panelX + 12, y, $"Author: {author}", Colors.Cyan); y += 14;
            framebuffer.DrawText(panelX + 12, y, $"GitHub: {github}", Colors.Magenta); y += 14;
            // framebuffer.DrawText(panelX + 12, y, $"Window: {window?.Settings.Width}x{window?.Settings.Height}", Colors.White); y += 14;
            // framebuffer.DrawText(panelX + 12, y, $"Framebuffer: {window?.Settings.FramebufferWidth}x{window?.Settings.FramebufferHeight}", Colors.White); y += 14;
            framebuffer.DrawText(panelX + 12, y, qualityText, Colors.Green); y += 14;
            framebuffer.DrawText(panelX + 12, y, $"Features: {featuresText}", Colors.Cyan); y += 14;
            framebuffer.DrawText(panelX + 12, y, tips[tipIndex], Colors.Gray);

            // Close Window Button
            if (Gui.Gui.Button(framebuffer, "Close Window", Colors.Red, mouseX, mouseY, mousePressed))
            {
                window?.Close();
            }

            // Settings Panel Button
            if (Gui.Gui.Button(framebuffer, showSettings ? "Hide Settings" : "Show Settings", Colors.Green, mouseX, mouseY, mousePressed))
            {
                showSettings = !showSettings;
            }

            // About Panel Button
            if (Gui.Gui.Button(framebuffer, showAbout ? "Hide About" : "About", Colors.Blue, mouseX, mouseY, mousePressed))
            {
                showAbout = !showAbout;
            }

            // Settings Panel
            if (showSettings && framebuffer.Window?.Manager != null)
            {
                // Place the settings panel below the main info panel
                int settingsX = 32;
                int settingsY = 32 + 180 + 24;
                Asmo.Gui.GuiSetup.SettingsPanel.Render(framebuffer, framebuffer.Window.Manager, settingsX, settingsY);
            }

            // About Panel
            if (showAbout)
            {
                int aboutX = panelX + panelW + 24, aboutY = panelY;
                framebuffer.DrawOutlinedRect(aboutX - 4, aboutY - 4, 220, 120, Colors.Magenta);
                framebuffer.DrawRect(aboutX, aboutY, 212, 112, new Asmo.Types.Color(32, 16, 32, 220));
                int ay = aboutY + 12;
                framebuffer.DrawText(aboutX + 12, ay, "ASMO is a .NET game console framework.", Colors.White); ay += 14;
                framebuffer.DrawText(aboutX + 12, ay, "Supports retro and modern modes.", Colors.White); ay += 14;
                framebuffer.DrawText(aboutX + 12, ay, "- Pixel-perfect scaling", Colors.Cyan); ay += 14;
                framebuffer.DrawText(aboutX + 12, ay, "- Enhanced graphics", Colors.Cyan); ay += 14;
                framebuffer.DrawText(aboutX + 12, ay, "- Easy modding & loading", Colors.Cyan); ay += 14;
                framebuffer.DrawText(aboutX + 12, ay, "- Open source!", Colors.Yellow);
            }

            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
            }
        }
    }
}
