using Asmo.Gfx;
using Asmo.Window;

namespace Asmo
{
    /// <summary>
    /// Static helper class for easy Asmo setup
    /// </summary>
    public static class AsmoSetup
    {
        /// <summary>
        /// Create a retro-style window (pixel perfect, simple graphics)
        /// </summary>
        public static Asmo.Window.Window CreateRetroWindow(string title = "Asmo Retro Console")
        {
            var settings = WindowSettings.Presets.Retro;
            settings.Title = title;
            return Asmo.Window.Window.CreateWithQuality(RenderingQuality.Retro, settings);
        }

        /// <summary>
        /// Create a modern gaming window with enhanced graphics
        /// </summary>
        public static Asmo.Window.Window CreateModernWindow(string title = "Asmo Game Console")
        {
            var settings = WindowSettings.Presets.Modern;
            settings.Title = title;
            return Asmo.Window.Window.CreateWithQuality(RenderingQuality.Enhanced, settings);
        }

        /// <summary>
        /// Create a high-quality window with all features enabled
        /// </summary>
        public static Asmo.Window.Window CreateHighQualityWindow(string title = "Asmo HD Console")
        {
            var settings = WindowSettings.Presets.HighRes;
            settings.Title = title;
            return Asmo.Window.Window.CreateWithQuality(RenderingQuality.HighQuality, settings);
        }

        /// <summary>
        /// Create a custom window with specific settings
        /// </summary>
        public static Asmo.Window.Window CreateCustomWindow(WindowSettings settings, RenderingQuality quality = RenderingQuality.Enhanced)
        {
            return Asmo.Window.Window.CreateWithQuality(quality, settings);
        }
    }

    /// <summary>
    /// Quick GUI setup helpers
    /// </summary>
    public static class GuiSetup
    {
        /// <summary>
        /// Setup common GUI elements with proper mouse handling
        /// </summary>
        public static (int mouseX, int mouseY, bool mousePressed) GetMouseState(Surface surface)
        {
            var window = surface.Window;
            var mouse = window?.Mouse;
            int mouseX = 0, mouseY = 0;
            bool mousePressed = false;

            if (window != null && mouse != null)
            {
                // Calculate actual scaling factors based on current window size
                float scaleX = (float)surface.Width / window.Size.X;
                float scaleY = (float)surface.Height / window.Size.Y;
                
                mouseX = (int)(mouse.X * scaleX);
                // Convert mouse Y from screen coordinates (top-left) to OpenGL coordinates (bottom-left)
                mouseY = surface.Height - (int)(mouse.Y * scaleY) - 1;
                mousePressed = mouse.IsButtonPressed(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);
            }

            return (mouseX, mouseY, mousePressed);
        }
    }
}