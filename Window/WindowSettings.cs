using OpenTK.Mathematics;

namespace Asmo.Window
{
    /// <summary>
    /// Helper class for configuring window settings
    /// </summary>
    public class WindowSettings
    {
        public string Title { get; set; } = "Asmo Game Console";
        public int Width { get; set; } = 768;
        public int Height { get; set; } = 512;
        public int FramebufferWidth { get; set; } = 384;
        public int FramebufferHeight { get; set; } = 256;
        public bool Resizable { get; set; } = true;
        public bool VSync { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public Vector2i? MinSize { get; set; } = null;
        public Vector2i? MaxSize { get; set; } = null;

        /// <summary>
        /// Common presets for different display types
        /// </summary>
        public static class Presets
        {
            /// <summary>
            /// Classic 16:10 retro gaming resolution
            /// </summary>
            public static WindowSettings Retro => new()
            {
                Title = "Asmo Retro Console",
                Width = 640,
                Height = 400,
                FramebufferWidth = 320,
                FramebufferHeight = 200,
                Resizable = false,
                VSync = false
            };

            /// <summary>
            /// Standard modern gaming resolution
            /// </summary>
            public static WindowSettings Modern => new()
            {
                Title = "Asmo Game Console",
                Width = 1280,
                Height = 720,
                FramebufferWidth = 640,
                FramebufferHeight = 360,
                Resizable = true,
                VSync = true
            };

            /// <summary>
            /// High resolution for detailed graphics
            /// </summary>
            public static WindowSettings HighRes => new()
            {
                Title = "Asmo HD Console",
                Width = 1920,
                Height = 1080,
                FramebufferWidth = 1960,
                FramebufferHeight = 1540,
                Resizable = true,
                VSync = true
            };

            /// <summary>
            /// Square aspect ratio for certain game types
            /// </summary>
            public static WindowSettings Square => new()
            {
                Title = "Asmo Square Console",
                Width = 800,
                Height = 800,
                FramebufferWidth = 400,
                FramebufferHeight = 400,
                Resizable = true,
                VSync = true
            };
        }

        /// <summary>
        /// Calculate the scaling factor from framebuffer to window
        /// </summary>
        public (float scaleX, float scaleY) GetScalingFactors()
        {
            return ((float)FramebufferWidth / Width, (float)FramebufferHeight / Height);
        }

        /// <summary>
        /// Get the effective pixel scale (assuming uniform scaling)
        /// </summary>
        public float GetPixelScale()
        {
            return (float)Width / FramebufferWidth;
        }
    }
}