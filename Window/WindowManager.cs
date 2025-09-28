using Asmo.Gfx;

namespace Asmo.Window
{
    /// <summary>
    /// Helper class for managing window settings and configurations at runtime
    /// </summary>
    public class WindowManager
    {
        private readonly Window _window;
        private WindowSettings _currentSettings;
        private GraphicsConfig _currentGraphicsConfig;

        public WindowManager(Window window)
        {
            _window = window;
            _currentSettings = window.Settings;
            _currentGraphicsConfig = window.GraphicsConfig;
        }

        /// <summary>
        /// Change the rendering quality and update graphics configuration
        /// </summary>
        public void SetQuality(RenderingQuality quality)
        {
            var newConfig = GraphicsConfig.FromQuality(quality);
            _window.UpdateGraphicsConfig(newConfig);
            _currentGraphicsConfig = newConfig;
        }

        /// <summary>
        /// Apply a window settings preset
        /// </summary>
        public void ApplyPreset(WindowSettings preset)
        {
            _window.UpdateSettings(preset);
            _currentSettings = preset;
        }

        /// <summary>
        /// Toggle VSync on/off
        /// </summary>
        public void SetVSync(bool enabled)
        {
            _currentSettings.VSync = enabled;
            _window.UpdateSettings(_currentSettings);
        }

        /// <summary>
        /// Toggle fullscreen mode
        /// </summary>
        public void SetFullscreen(bool enabled)
        {
            _currentSettings.Fullscreen = enabled;
            _window.UpdateSettings(_currentSettings);
        }

        /// <summary>
        /// Update window title
        /// </summary>
        public void SetTitle(string title)
        {
            _currentSettings.Title = title;
            _window.UpdateSettings(_currentSettings);
        }

        /// <summary>
        /// Resize the window and framebuffer
        /// </summary>
        public void Resize(int windowWidth, int windowHeight, int? framebufferWidth = null, int? framebufferHeight = null)
        {
            _currentSettings.Width = windowWidth;
            _currentSettings.Height = windowHeight;
            
            if (framebufferWidth.HasValue)
                _currentSettings.FramebufferWidth = framebufferWidth.Value;
            if (framebufferHeight.HasValue)
                _currentSettings.FramebufferHeight = framebufferHeight.Value;
            
            _window.UpdateSettings(_currentSettings);
        }

        /// <summary>
        /// Enable/disable specific graphics features
        /// </summary>
        public void SetGraphicsFeature(GraphicsFeature feature, bool enabled)
        {
            switch (feature)
            {
                case GraphicsFeature.AntiAliasing:
                    _currentGraphicsConfig.EnableAntiAliasing = enabled;
                    break;
                case GraphicsFeature.ImageLoading:
                    _currentGraphicsConfig.EnableImageLoading = enabled;
                    break;
                case GraphicsFeature.BitmapFonts:
                    _currentGraphicsConfig.EnableBitmapFonts = enabled;
                    break;
                case GraphicsFeature.FilteredScaling:
                    _currentGraphicsConfig.EnableFilteredScaling = enabled;
                    break;
                case GraphicsFeature.Blending:
                    _currentGraphicsConfig.EnableBlending = enabled;
                    break;
            }
            
            _window.UpdateGraphicsConfig(_currentGraphicsConfig);
        }

        /// <summary>
        /// Create a custom quality configuration
        /// </summary>
        public void CreateCustomQuality(bool antiAliasing = false, bool imageLoading = true, 
            bool bitmapFonts = true, bool filteredScaling = true, bool blending = true, int maxTextureSize = 2048)
        {
            var customConfig = new GraphicsConfig
            {
                Quality = RenderingQuality.Enhanced, // Base it on Enhanced
                EnableAntiAliasing = antiAliasing,
                EnableImageLoading = imageLoading,
                EnableBitmapFonts = bitmapFonts,
                EnableFilteredScaling = filteredScaling,
                EnableBlending = blending,
                MaxTextureSize = maxTextureSize
            };
            
            _window.UpdateGraphicsConfig(customConfig);
            _currentGraphicsConfig = customConfig;
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            var defaultSettings = new WindowSettings();
            var defaultConfig = GraphicsConfig.FromQuality(RenderingQuality.Retro);
            
            _window.UpdateSettings(defaultSettings);
            _window.UpdateGraphicsConfig(defaultConfig);
            
            _currentSettings = defaultSettings;
            _currentGraphicsConfig = defaultConfig;
        }

        /// <summary>
        /// Get current window and graphics information
        /// </summary>
        public (WindowSettings settings, GraphicsConfig config) GetCurrentConfiguration()
        {
            return (_currentSettings, _currentGraphicsConfig);
        }

        /// <summary>
        /// Quick setup for game development scenarios
        /// </summary>
        public static class QuickSetup
        {
            /// <summary>
            /// Setup for pixel art games - crisp pixels, no filtering
            /// </summary>
            public static void PixelArt(WindowManager manager)
            {
                manager.SetQuality(RenderingQuality.Enhanced);
                manager.SetGraphicsFeature(GraphicsFeature.FilteredScaling, false);
                manager.SetGraphicsFeature(GraphicsFeature.AntiAliasing, false);
            }

            /// <summary>
            /// Setup for smooth graphics - filtering and anti-aliasing enabled
            /// </summary>
            public static void Smooth(WindowManager manager)
            {
                manager.SetQuality(RenderingQuality.HighQuality);
                manager.SetGraphicsFeature(GraphicsFeature.FilteredScaling, true);
                manager.SetGraphicsFeature(GraphicsFeature.AntiAliasing, true);
            }

            /// <summary>
            /// Performance focused setup - minimal features for speed
            /// </summary>
            public static void Performance(WindowManager manager)
            {
                manager.SetQuality(RenderingQuality.Retro);
            }

            /// <summary>
            /// Full featured setup - all advanced features enabled
            /// </summary>
            public static void FullFeatured(WindowManager manager)
            {
                manager.SetQuality(RenderingQuality.HighQuality);
                manager.CreateCustomQuality(
                    antiAliasing: true,
                    imageLoading: true,
                    bitmapFonts: true,
                    filteredScaling: true,
                    blending: true,
                    maxTextureSize: 4096
                );
            }
        }
    }

    /// <summary>
    /// Individual graphics features that can be toggled
    /// </summary>
    public enum GraphicsFeature
    {
        AntiAliasing,
        ImageLoading,
        BitmapFonts,
        FilteredScaling,
        Blending
    }
}