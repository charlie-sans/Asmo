using OpenTK.Windowing.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asmo.Window.input;
using Asmo.Gui;
using Asmo.Gfx; // Add this to access SurfaceExtensions

namespace Asmo.Window.HomeScreen
{
    public class HomeScreenDisplay
    {
        bool demoCheckbox = false;
        bool buttonClicked = false;
        bool showSettings = false;
        bool showImageDemo = false;
        private string sampleImagePath = "Assets/sample.png"; // Example path

        public void RenderHomeScreen(FrameEventArgs e, Gfx.Surface framebuffer)
        {
            // Use the new helper method for mouse handling
            var (mouseX, mouseY, mousePressed) = Asmo.GuiSetup.GetMouseState(framebuffer);
            
            // Demo high-quality features if available
            framebuffer.WithEnhanced(enhanced => 
            {
     
                if (File.Exists(sampleImagePath))
                {
                    enhanced.LoadImage("sample", sampleImagePath);
                    enhanced.DrawImage("sample", 10, framebuffer.Height - 60, 100, 50);
                }
                
                // Example: Load and use a bitmap font (if you have a bitmap font file)
                string fontPath = "Assets/pixel_font.png";
                if (File.Exists(fontPath))
                {
                    enhanced.LoadBitmapFont("pixel", fontPath, 8, 8);
                    enhanced.DrawBitmapText("pixel", 10, framebuffer.Height - 20, "High Quality Mode!", Asmo.Gfx.Colors.Yellow);
                }
                
                // Demonstrate alpha blending
                if (enhanced.Config.EnableBlending)
                {
                    var semiTransparent = new Asmo.Types.Color(255, 0, 0, 128);
                    enhanced.DrawPixelBlended(50, 50, semiTransparent);
                    enhanced.DrawPixelBlended(51, 50, semiTransparent);
                    enhanced.DrawPixelBlended(52, 50, semiTransparent);
                }
            });
            
            // Show current rendering quality in UI
            var config = framebuffer.GetGraphicsConfig();
            string qualityText = config != null ? $"Quality: {config.Quality}" : "Quality: Retro";
            string featuresText = framebuffer.GetFeatureSummary();
            
            // Main UI Panel
            var (guiX, guiY) = GuiPlacement.Right.Top(framebuffer, offsetFromRight: 245, offsetFromTop: 50);
            Gui.Gui.Begin(guiX, guiY);
            
            Gui.Gui.Label(framebuffer, "=== ASMO DEMO ===", Asmo.Gfx.Colors.Yellow);
            Gui.Gui.Label(framebuffer, qualityText, Asmo.Gfx.Colors.Cyan);
            
            if (Gui.Gui.Checkbox(framebuffer, "Demo Checkbox", ref demoCheckbox, Asmo.Gfx.Colors.Cyan, mouseX, mouseY, mousePressed))
            {
                // Checkbox toggled
            }
            if (Gui.Gui.Button(framebuffer, "Demo Button", Asmo.Gfx.Colors.Yellow, mouseX, mouseY, mousePressed))
            {
                buttonClicked = !buttonClicked;
            }
            
            // Settings panel toggle
            if (Gui.Gui.Button(framebuffer, "Settings Panel", Asmo.Gfx.Colors.Green, mouseX, mouseY, mousePressed))
            {
                showSettings = !showSettings;
            }
            
            // Image demo toggle (only if enhanced features available)
            if (framebuffer.IsEnhanced())
            {
                if (Gui.Gui.Button(framebuffer, "Image Demo", Asmo.Gfx.Colors.Magenta, mouseX, mouseY, mousePressed))
                {
                    showImageDemo = !showImageDemo;
                }
            }
            
            // Show enhanced features availability
            if (framebuffer.IsEnhanced())
            {
                var enhancedConfig = framebuffer.GetGraphicsConfig()!;
                Gui.Gui.Label(framebuffer, "Enhanced Features:", Asmo.Gfx.Colors.Green);
                Gui.Gui.Label(framebuffer, $"Images: {(enhancedConfig.EnableImageLoading ? "ON" : "OFF")}", Asmo.Gfx.Colors.White);
                Gui.Gui.Label(framebuffer, $"Fonts: {(enhancedConfig.EnableBitmapFonts ? "ON" : "OFF")}", Asmo.Gfx.Colors.White);
                Gui.Gui.Label(framebuffer, $"Blending: {(enhancedConfig.EnableBlending ? "ON" : "OFF")}", Asmo.Gfx.Colors.White);
                Gui.Gui.Label(framebuffer, $"Filtering: {(enhancedConfig.EnableFilteredScaling ? "ON" : "OFF")}", Asmo.Gfx.Colors.White);
            }
            else
            {
                Gui.Gui.Label(framebuffer, "Basic Surface Mode", Asmo.Gfx.Colors.Gray);
                Gui.Gui.Label(framebuffer, "For enhanced features,", Asmo.Gfx.Colors.Gray);
                Gui.Gui.Label(framebuffer, "use Settings Panel!", Asmo.Gfx.Colors.Gray);
            }
            
            Gui.Gui.Label(framebuffer, $"Checkbox: {(demoCheckbox ? "Checked" : "Unchecked")}", Asmo.Gfx.Colors.White);
            Gui.Gui.Label(framebuffer, $"Button: {(buttonClicked ? "Clicked!" : "Not clicked")}", Asmo.Gfx.Colors.White);
            
            // Settings Panel
            if (showSettings && framebuffer.Window?.Manager != null)
            {
                var (settingsX, settingsY) = GuiPlacement.Left.Top(framebuffer, offsetFromLeft: 10, offsetFromTop: 50);
                Asmo.Gui.GuiSetup.SettingsPanel.Render(framebuffer, framebuffer.Window.Manager, settingsX, settingsY);
            }
            
            // Image Demo Panel
            if (showImageDemo)
            {
                RenderImageDemo(framebuffer, mouseX, mouseY, mousePressed);
            }
            
            // Draw a debug box around the mouse cursor only if inside framebuffer
            if (mouseX >= 0 && mouseY >= 0 && mouseX < framebuffer.Width && mouseY < framebuffer.Height)
            {
                framebuffer.DrawOutlinedRect(mouseX - 4, mouseY - 4, 9, 9, Asmo.Gfx.Colors.Magenta);
            }
            // Debug output for mouse position
            Console.WriteLine($"HomeScreen: mouseX={mouseX}, mouseY={mouseY}, fb={framebuffer.Width}x{framebuffer.Height}");
            
            // Show instructions at bottom
            var instructionsY = 20;
            framebuffer.DrawText(10, instructionsY, "Enhanced Graphics Demo - Use Settings Panel to change quality modes", Asmo.Gfx.Colors.White);
            framebuffer.DrawText(10, instructionsY + 10, $"Current: {featuresText}", Asmo.Gfx.Colors.Cyan);
        }

        private void RenderImageDemo(Gfx.Surface framebuffer, int mouseX, int mouseY, bool mousePressed)
        {
            framebuffer.WithEnhanced(enhanced =>
            {
                var (demoX, demoY) = GuiPlacement.Center.Middle(framebuffer, -100, -50);
                
                Gui.Gui.Begin(demoX, demoY);
                Gui.Gui.Label(framebuffer, "=== IMAGE DEMO ===", Asmo.Gfx.Colors.Yellow);
                
                // Try to load and display sample images if they exist
                string[] sampleImages = { 
                    "Assets/logo.png", 
                    "Assets/sample.jpg", 
                    "Assets/test.bmp",
                    "Assets/demo.gif"
                };
                
                bool foundImage = false;
                foreach (string imagePath in sampleImages)
                {
                    if (File.Exists(imagePath))
                    {
                        string imageName = Path.GetFileNameWithoutExtension(imagePath);
                        if (enhanced.LoadImage(imageName, imagePath))
                        {
                            var imageInfo = enhanced.GetImageInfo(imageName);
                            if (imageInfo.HasValue)
                            {
                                Gui.Gui.Label(framebuffer, $"Loaded: {imageName}", Asmo.Gfx.Colors.Green);
                                Gui.Gui.Label(framebuffer, $"Size: {imageInfo.Value.width}x{imageInfo.Value.height}", Asmo.Gfx.Colors.White);
                                
                                // Draw the image scaled down
                                enhanced.DrawImage(imageName, demoX + 150, demoY - 100, 64, 64);
                                foundImage = true;
                                break; // Just show one image for demo
                            }
                        }
                    }
                }
                
                if (!foundImage)
                {
                    Gui.Gui.Label(framebuffer, "No sample images found", Asmo.Gfx.Colors.Red);
                    Gui.Gui.Label(framebuffer, "Try placing images in:", Asmo.Gfx.Colors.Gray);
                    Gui.Gui.Label(framebuffer, "Assets/logo.png", Asmo.Gfx.Colors.Gray);
                    Gui.Gui.Label(framebuffer, "Assets/sample.jpg", Asmo.Gfx.Colors.Gray);
                    
                    // Draw a colored rectangle as fallback demo
                    for (int i = 0; i < 64; i++)
                    {
                        for (int j = 0; j < 64; j++)
                        {
                            var color = new Asmo.Types.Color((i * 4) % 256, (j * 4) % 256, 128, 255);
                            enhanced.SetPixel(demoX + 150 + i, demoY - 100 + j, color);
                        }
                    }
                    Gui.Gui.Label(framebuffer, "Procedural demo ^", Asmo.Gfx.Colors.Cyan);
                }
                
                if (Gui.Gui.Button(framebuffer, "Close Demo", Asmo.Gfx.Colors.Red, mouseX, mouseY, mousePressed))
                {
                    showImageDemo = false;
                }
            });
            
            // If not enhanced, show a message
            if (!framebuffer.IsEnhanced())
            {
                var (demoX, demoY) = GuiPlacement.Center.Middle(framebuffer, -100, -20);
                framebuffer.DrawText(demoX, demoY, "Image demo requires Enhanced mode!", Asmo.Gfx.Colors.Red);
                framebuffer.DrawText(demoX, demoY + 10, "Use Settings Panel to enable it.", Asmo.Gfx.Colors.White);
            }
        }
    }
}
