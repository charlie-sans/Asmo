using Asmo;
using Asmo.Gfx;
using Asmo.Window;

namespace Asmo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Starting Asmo Console ===");
            
            // Let's start with the simplest retro window to ensure basic functionality works
            Console.WriteLine("Creating retro window...");
            var window = AsmoSetup.CreateRetroWindow("Asmo Retro Game Console");
            
            Console.WriteLine($"Created window:");
            Console.WriteLine($"  - Type: {window.GetType().Name}");
            Console.WriteLine($"  - Settings: {window.Settings.Width}x{window.Settings.Height}");
            Console.WriteLine($"  - Framebuffer: {window.Settings.FramebufferWidth}x{window.Settings.FramebufferHeight}");
            Console.WriteLine($"  - Graphics Quality: {window.GraphicsConfig.Quality}");
            Console.WriteLine($"  - Features:");
            Console.WriteLine($"    * Image Loading: {window.GraphicsConfig.EnableImageLoading}");
            Console.WriteLine($"    * Bitmap Fonts: {window.GraphicsConfig.EnableBitmapFonts}");
            Console.WriteLine($"    * Anti-aliasing: {window.GraphicsConfig.EnableAntiAliasing}");
            Console.WriteLine($"    * Blending: {window.GraphicsConfig.EnableBlending}");
            
            Console.WriteLine("Starting window...");
            window.Run();
            
            Console.WriteLine("Window closed.");
        }
    }
}