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
        public HomeScreenDisplay()
        {
            GameEnvironment.ShowFps = true;
        }
        
        public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer)
        {
            // Use Mouse class for mouse state
            var window = framebuffer.Window;
            int mouseX = -1, mouseY = -1;
            if (window is Asmo.Window.Window win && win is not null)
            {
                var mouseField = typeof(Asmo.Window.Window).GetField("mouse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var mouseObj = mouseField?.GetValue(win) as Asmo.Window.input.Mouse;
                if (mouseObj != null)
                {
                    mouseX = mouseObj.X;
                    mouseY = mouseObj.Y;
                }
            }

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
            int panelW = 340, panelH = 160;
            int panelX = (framebuffer.Width - panelW) / 2;
            int panelY = (framebuffer.Height - panelH) / 2;
            framebuffer.DrawOutlinedRect(panelX - 4, panelY - 4, panelW + 8, panelH + 8, Colors.Cyan);
            framebuffer.DrawRect(panelX, panelY, panelW, panelH, new Asmo.Types.Color(16, 16, 32, 220));

            int y = panelY + 18;
            framebuffer.DrawText(panelX + 20, y, $"ASMO GAME CONSOLE", Colors.Yellow); y += 20;
            framebuffer.DrawText(panelX + 20, y, $"Drop a game file (DLL or ZIP) to play!", Colors.White); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"(Or drag a folder with a game DLL)", Colors.Gray); y += 18;
            framebuffer.DrawText(panelX + 20, y, $"Version: {version}", Colors.Cyan); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"by {author}", Colors.Magenta); y += 16;
            framebuffer.DrawText(panelX + 20, y, $"{github}", Colors.Blue); y += 16;

            // Tips area (bottom of panel)
            int tipY = panelY + panelH - 24;
            framebuffer.DrawText(panelX + 20, tipY, tips[tipIndex], Colors.Green);

            // Draw mouse cursor
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Colors.Magenta);
            }
        }
    }
}
