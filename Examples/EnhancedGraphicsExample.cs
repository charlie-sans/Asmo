using Asmo.Window;
using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Examples
{
    /// <summary>
    /// Example showing how to use the enhanced graphics system
    /// </summary>
    public static class EnhancedGraphicsExample
    {
        /// <summary>
        /// Create a window with different quality presets
        /// </summary>
        public static void RunExamples()
        {
            // Example 1: Create a basic retro window
            Console.WriteLine("Creating Retro window...");
            var retroWindow = Asmo.Window.Window.CreateWithQuality(RenderingQuality.Retro);
            
            // Example 2: Create an enhanced window with modern preset
            Console.WriteLine("Creating Enhanced window...");
            var enhancedWindow = Asmo.Window.Window.CreateWithQuality(
                RenderingQuality.Enhanced, 
                WindowSettings.Presets.Modern
            );
            
            // Example 3: Create a high-quality window
            Console.WriteLine("Creating High Quality window...");
            var hqWindow = Asmo.Window.Window.CreateWithQuality(RenderingQuality.HighQuality);
            
            // Example 4: Runtime quality changes using WindowManager
            Console.WriteLine("Demonstrating runtime quality changes...");
            DemonstrateRuntimeChanges(enhancedWindow);
            
            // Example 5: Quick setup scenarios
            Console.WriteLine("Demonstrating quick setups...");
            DemonstrateQuickSetups(enhancedWindow);
        }

        private static void DemonstrateRuntimeChanges(Asmo.Window.Window window)
        {
            var manager = window.Manager;
            
            // Change to pixel art mode
            WindowManager.QuickSetup.PixelArt(manager);
            Console.WriteLine("Switched to Pixel Art mode");
            
            // Change to smooth graphics
            WindowManager.QuickSetup.Smooth(manager);
            Console.WriteLine("Switched to Smooth mode");
            
            // Apply a preset
            manager.ApplyPreset(WindowSettings.Presets.HighRes);
            Console.WriteLine("Applied HD preset");
            
            // Toggle specific features
            manager.SetGraphicsFeature(GraphicsFeature.AntiAliasing, true);
            manager.SetGraphicsFeature(GraphicsFeature.Blending, true);
            Console.WriteLine("Enabled anti-aliasing and blending");
            
            // Create custom configuration
            manager.CreateCustomQuality(
                antiAliasing: true,
                imageLoading: true,
                bitmapFonts: true,
                filteredScaling: false, // Keep pixel-perfect
                blending: true,
                maxTextureSize: 2048
            );
            Console.WriteLine("Applied custom quality configuration");
        }

        private static void DemonstrateQuickSetups(Asmo.Window.Window window)
        {
            var manager = window.Manager;
            
            // Performance setup for slower hardware
            WindowManager.QuickSetup.Performance(manager);
            Console.WriteLine("Applied Performance setup");
            
            // Full featured setup for modern hardware
            WindowManager.QuickSetup.FullFeatured(manager);
            Console.WriteLine("Applied Full Featured setup");
            
            // Back to pixel art for game development
            WindowManager.QuickSetup.PixelArt(manager);
            Console.WriteLine("Applied Pixel Art setup");
        }

        /// <summary>
        /// Example of using enhanced surface features
        /// </summary>
        public static void EnhancedSurfaceExample(Gfx.Surface surface)
        {
            // Check if enhanced features are available
            if (surface.IsEnhanced())
            {
                Console.WriteLine("Enhanced features available!");
                Console.WriteLine(surface.GetFeatureSummary());
                
                // Use enhanced features
                surface.WithEnhanced(enhanced =>
                {
                    // Load an image (supports PNG, JPEG, BMP, GIF, etc.)
                    if (enhanced.LoadImage("logo", "Assets/logo.png"))
                    {
                        // Draw it at different scales
                        enhanced.DrawImage("logo", 10, 10); // Original size
                        enhanced.DrawImage("logo", 50, 50, 32, 32); // Scaled down
                        enhanced.DrawImage("logo", 100, 100, 128, 128, 0.5f); // Scaled up, semi-transparent
                        
                        // Create a sprite from part of the image
                        enhanced.CreateImageFromRegion("logo", "logo_corner", 0, 0, 16, 16);
                        enhanced.DrawImage("logo_corner", 200, 200);
                    }
                    
                    // Load a bitmap font
                    if (enhanced.LoadBitmapFont("pixel", "Assets/font.png", 8, 8))
                    {
                        enhanced.DrawBitmapText("pixel", 10, 250, "Hello Enhanced World!", Gfx.Colors.Yellow);
                    }
                    
                    // Use alpha blending
                    if (enhanced.Config.EnableBlending)
                    {
                        var semiTransparent = new Color(255, 0, 0, 128);
                        enhanced.DrawPixelBlended(300, 300, semiTransparent);
                    }
                });
            }
            else
            {
                Console.WriteLine("Basic surface - enhanced features not available");
                // Fallback to basic drawing
                surface.DrawText(10, 10, "Basic Mode", Gfx.Colors.White);
                surface.DrawRect(10, 30, 100, 50, Gfx.Colors.Red);
            }
            
            // These work on both basic and enhanced surfaces
            surface.DrawLine(0, 0, surface.Width, surface.Height, Gfx.Colors.Green);
            surface.DrawCircle(surface.Width / 2, surface.Height / 2, 50, Gfx.Colors.Blue);
        }

        /// <summary>
        /// Quick helper methods example
        /// </summary>
        public static void QuickHelpersExample(Gfx.Surface surface)
        {
            // Quick draw an image without manually loading it
            surface.QuickDrawImage("Assets/splash.png", 0, 0, 320, 240);
            
            // Quick draw bitmap text
            surface.QuickDrawBitmapText("Assets/font.png", 8, 8, 10, 10, "Quick Text!", Gfx.Colors.Cyan);
            
            // Apply effects
            surface.ApplyEffect(SurfaceEffect.Fade, 0.3f);
        }
    }
}