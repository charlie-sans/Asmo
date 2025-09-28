namespace Asmo.Gfx
{
    /// <summary>
    /// Rendering quality levels that enable different graphics features
    /// </summary>
    public enum RenderingQuality
    {
        /// <summary>
        /// Basic pixel-perfect rendering with simple fonts and shapes
        /// </summary>
        Retro = 0,
        
        /// <summary>
        /// Enhanced rendering with bitmap fonts and basic image support
        /// </summary>
        Enhanced = 1,
        
        /// <summary>
        /// High quality rendering with full image formats, anti-aliasing, and advanced effects
        /// </summary>
        HighQuality = 2
    }

    /// <summary>
    /// Configuration for graphics rendering features
    /// </summary>
    public class GraphicsConfig
    {
        public RenderingQuality Quality { get; set; } = RenderingQuality.Retro;
        public bool EnableAntiAliasing { get; set; } = false;
        public bool EnableImageLoading { get; set; } = false;
        public bool EnableBitmapFonts { get; set; } = false;
        public bool EnableFilteredScaling { get; set; } = false;
        public bool EnableBlending { get; set; } = false;
        public int MaxTextureSize { get; set; } = 1024;

        /// <summary>
        /// Apply quality presets
        /// </summary>
        public static GraphicsConfig FromQuality(RenderingQuality quality)
        {
            return quality switch
            {
                RenderingQuality.Retro => new GraphicsConfig
                {
                    Quality = RenderingQuality.Retro,
                    EnableAntiAliasing = false,
                    EnableImageLoading = false,
                    EnableBitmapFonts = false,
                    EnableFilteredScaling = false,
                    EnableBlending = false,
                    MaxTextureSize = 256
                },
                RenderingQuality.Enhanced => new GraphicsConfig
                {
                    Quality = RenderingQuality.Enhanced,
                    EnableAntiAliasing = false,
                    EnableImageLoading = true,
                    EnableBitmapFonts = true,
                    EnableFilteredScaling = true,
                    EnableBlending = true,
                    MaxTextureSize = 1024
                },
                RenderingQuality.HighQuality => new GraphicsConfig
                {
                    Quality = RenderingQuality.HighQuality,
                    EnableAntiAliasing = true,
                    EnableImageLoading = true,
                    EnableBitmapFonts = true,
                    EnableFilteredScaling = true,
                    EnableBlending = true,
                    MaxTextureSize = 4096
                },
                _ => new GraphicsConfig()
            };
        }
    }
}