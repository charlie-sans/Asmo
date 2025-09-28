using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Gfx
{
    /// <summary>
    /// Extension methods for Surface to provide easier access to enhanced features
    /// </summary>
    public static class SurfaceExtensions
    {
        /// <summary>
        /// Check if this surface supports enhanced features
        /// </summary>
        public static bool IsEnhanced(this Surface surface)
        {
            return surface is EnhancedSurface;
        }

        /// <summary>
        /// Get the graphics configuration if this is an enhanced surface
        /// </summary>
        public static GraphicsConfig? GetGraphicsConfig(this Surface surface)
        {
            return surface is EnhancedSurface enhanced ? enhanced.Config : null;
        }

        /// <summary>
        /// Execute code only if enhanced features are available
        /// </summary>
        public static void WithEnhanced(this Surface surface, Action<EnhancedSurface> action)
        {
            if (surface is EnhancedSurface enhanced)
            {
                action(enhanced);
            }
        }

        /// <summary>
        /// Execute code with enhanced features and return a result
        /// </summary>
        public static T WithEnhanced<T>(this Surface surface, Func<EnhancedSurface, T> func, T defaultValue = default(T))
        {
            if (surface is EnhancedSurface enhanced)
            {
                return func(enhanced);
            }
            return defaultValue;
        }

        /// <summary>
        /// Quick helper to load and draw an image in one call
        /// </summary>
        public static bool QuickDrawImage(this Surface surface, string filePath, int x, int y, int? width = null, int? height = null)
        {
            return surface.WithEnhanced(enhanced =>
            {
                string imageName = $"_quick_{Path.GetFileName(filePath)}";
                if (enhanced.LoadImage(imageName, filePath))
                {
                    enhanced.DrawImage(imageName, x, y, width, height);
                    return true;
                }
                return false;
            }, false);
        }

        /// <summary>
        /// Quick helper to load and draw bitmap text in one call
        /// </summary>
        public static bool QuickDrawBitmapText(this Surface surface, string fontPath, int charWidth, int charHeight, 
            int x, int y, string text, Asmo.Types.Color tint)
        {
            return surface.WithEnhanced(enhanced =>
            {
                string fontName = $"_quick_{Path.GetFileName(fontPath)}";
                if (enhanced.LoadBitmapFont(fontName, fontPath, charWidth, charHeight))
                {
                    enhanced.DrawBitmapText(fontName, x, y, text, tint);
                    return true;
                }
                return false;
            }, false);
        }

        /// <summary>
        /// Get a summary of available features for this surface
        /// </summary>
        public static string GetFeatureSummary(this Surface surface)
        {
            if (surface is EnhancedSurface enhanced)
            {
                var config = enhanced.Config;
                var features = new List<string>();
                
                if (config.EnableImageLoading) features.Add("Images");
                if (config.EnableBitmapFonts) features.Add("Bitmap Fonts");
                if (config.EnableBlending) features.Add("Alpha Blending");
                if (config.EnableAntiAliasing) features.Add("Anti-Aliasing");
                if (config.EnableFilteredScaling) features.Add("Filtered Scaling");
                
                return $"Enhanced Surface ({config.Quality}) - Features: {string.Join(", ", features)}";
            }
            
            return "Basic Surface (Retro) - Features: Pixels, Shapes, Built-in Fonts";
        }

        /// <summary>
        /// Apply a graphics effect to the entire surface (if enhanced)
        /// </summary>
        public static void ApplyEffect(this Surface surface, SurfaceEffect effect, float intensity = 1.0f)
        {
            surface.WithEnhanced(enhanced =>
            {
                switch (effect)
                {
                    case SurfaceEffect.Fade:
                        ApplyFadeEffect(enhanced, intensity);
                        break;
                    case SurfaceEffect.Tint:
                        ApplyTintEffect(enhanced, intensity);
                        break;
                    // Add more effects as needed
                }
            });
        }

        private static void ApplyFadeEffect(EnhancedSurface surface, float intensity)
        {
            var fadeColor = new Color(0, 0, 0, (int)(255 * intensity));
            for (int x = 0; x < surface.Width; x++)
            {
                for (int y = 0; y < surface.Height; y++)
                {
                    surface.DrawPixelBlended(x, y, fadeColor);
                }
            }
        }

        private static void ApplyTintEffect(EnhancedSurface surface, float intensity)
        {
            // Example tint effect - add more sophisticated effects as needed
            var tintColor = new Color(255, 200, 200, (int)(128 * intensity));
            for (int x = 0; x < surface.Width; x++)
            {
                for (int y = 0; y < surface.Height; y++)
                {
                    surface.DrawPixelBlended(x, y, tintColor);
                }
            }
        }
    }

    /// <summary>
    /// Available surface effects
    /// </summary>
    public enum SurfaceEffect
    {
        Fade,
        Tint,
        // Add more effects as needed
    }
}