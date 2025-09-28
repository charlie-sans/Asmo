using Asmo.Types;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using AsmoColor = Asmo.Types.Color; // Alias to avoid conflicts

namespace Asmo.Gfx
{
    /// <summary>
    /// Extended Surface with high-quality graphics features using ImageSharp
    /// </summary>
    public class EnhancedSurface : Surface
    {
        public GraphicsConfig Config { get; private set; }
        private Dictionary<string, Image<Rgba32>> _loadedImages = new();
        private Dictionary<string, BitmapFont> _loadedFonts = new();

        public EnhancedSurface(int width, int height, GraphicsConfig? config = null) 
            : base(width, height)
        {
            Config = config ?? GraphicsConfig.FromQuality(RenderingQuality.Retro);
        }

        /// <summary>
        /// Load an image from file (PNG, JPEG, BMP, GIF, TIFF, WebP, etc.)
        /// </summary>
        public bool LoadImage(string name, string filePath)
        {
            if (!Config.EnableImageLoading)
            {
                Console.WriteLine($"Image loading disabled. Enable Enhanced or HighQuality rendering.");
                return false;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    var image = Image.Load<Rgba32>(filePath);
                    
                    // Check texture size limits
                    if (image.Width > Config.MaxTextureSize || image.Height > Config.MaxTextureSize)
                    {
                        Console.WriteLine($"Warning: Image {filePath} ({image.Width}x{image.Height}) exceeds max texture size ({Config.MaxTextureSize}x{Config.MaxTextureSize}). Resizing...");
                        
                        float scale = Math.Min((float)Config.MaxTextureSize / image.Width, (float)Config.MaxTextureSize / image.Height);
                        int newWidth = (int)(image.Width * scale);
                        int newHeight = (int)(image.Height * scale);
                        
                        image.Mutate(x => x.Resize(newWidth, newHeight));
                    }
                    
                    _loadedImages[name] = image;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load image {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draw a loaded image with optional scaling and effects
        /// </summary>
        public void DrawImage(string name, int x, int y, int? width = null, int? height = null, float alpha = 1.0f, bool flipX = false, bool flipY = false)
        {
            if (!_loadedImages.TryGetValue(name, out var image))
            {
                Console.WriteLine($"Image '{name}' not found. Load it first with LoadImage().");
                return;
            }

            int drawWidth = width ?? image.Width;
            int drawHeight = height ?? image.Height;

            // Apply transformations if needed
            var processedImage = image;
            bool needsProcessing = flipX || flipY || (alpha < 1.0f && Config.EnableBlending);
            
            if (needsProcessing)
            {
                processedImage = image.Clone();
                processedImage.Mutate(ctx =>
                {
                    if (flipX) ctx.Flip(FlipMode.Horizontal);
                    if (flipY) ctx.Flip(FlipMode.Vertical);
                });
            }

            // High-quality scaling with filtering if enabled
            if (Config.EnableFilteredScaling && (drawWidth != image.Width || drawHeight != image.Height))
            {
                var scaledImage = processedImage.Clone();
                scaledImage.Mutate(x => x.Resize(drawWidth, drawHeight, KnownResamplers.Lanczos3));
                
                DrawImageDirect(scaledImage, x, y, alpha);
                
                if (scaledImage != processedImage) scaledImage.Dispose();
            }
            else
            {
                // Nearest neighbor scaling for pixel art
                DrawImageNearestNeighbor(processedImage, x, y, drawWidth, drawHeight, alpha);
            }
            
            if (processedImage != image) processedImage.Dispose();
        }

        /// <summary>
        /// Draw image directly without scaling
        /// </summary>
        private void DrawImageDirect(Image<Rgba32> image, int x, int y, float alpha = 1.0f)
        {
            for (int py = 0; py < image.Height; py++)
            {
                for (int px = 0; px < image.Width; px++)
                {
                    var pixel = image[px, py];
                    if (pixel.A > 0) // Only draw non-transparent pixels
                    {
                        var asmoColor = new AsmoColor(pixel.R, pixel.G, pixel.B, (int)(pixel.A * alpha));
                        
                        if (Config.EnableBlending && asmoColor.A < 255)
                        {
                            DrawPixelBlended(x + px, y + py, asmoColor);
                        }
                        else
                        {
                            SetPixel(x + px, y + py, asmoColor);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Draw image with nearest neighbor scaling for pixel art
        /// </summary>
        private void DrawImageNearestNeighbor(Image<Rgba32> image, int x, int y, int drawWidth, int drawHeight, float alpha = 1.0f)
        {
            for (int py = 0; py < drawHeight; py++)
            {
                for (int px = 0; px < drawWidth; px++)
                {
                    // Calculate source pixel using nearest neighbor
                    int srcX = (px * image.Width) / drawWidth;
                    int srcY = (py * image.Height) / drawHeight;
                    
                    if (srcX < image.Width && srcY < image.Height)
                    {
                        var pixel = image[srcX, srcY];
                        if (pixel.A > 0) // Only draw non-transparent pixels
                        {
                            var asmoColor = new AsmoColor(pixel.R, pixel.G, pixel.B, (int)(pixel.A * alpha));
                            
                            if (Config.EnableBlending && asmoColor.A < 255)
                            {
                                DrawPixelBlended(x + px, y + py, asmoColor);
                            }
                            else
                            {
                                SetPixel(x + px, y + py, asmoColor);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Create a new image from a portion of a loaded image (sprite sheet support)
        /// </summary>
        public bool CreateImageFromRegion(string sourceName, string newName, int srcX, int srcY, int srcWidth, int srcHeight)
        {
            if (!_loadedImages.TryGetValue(sourceName, out var sourceImage))
            {
                Console.WriteLine($"Source image '{sourceName}' not found.");
                return false;
            }

            try
            {
                var region = sourceImage.Clone(ctx => ctx.Crop(new Rectangle(srcX, srcY, srcWidth, srcHeight)));
                _loadedImages[newName] = region;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create image region: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load a bitmap font from file with ImageSharp
        /// </summary>
        public bool LoadBitmapFont(string name, string filePath, int charWidth, int charHeight, string charset = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~")
        {
            if (!Config.EnableBitmapFonts)
            {
                Console.WriteLine($"Bitmap fonts disabled. Enable Enhanced or HighQuality rendering.");
                return false;
            }

            try
            {
                if (File.Exists(filePath))
                {
                    var fontImage = Image.Load<Rgba32>(filePath);
                    var font = new BitmapFont(fontImage, charWidth, charHeight, charset);
                    _loadedFonts[name] = font;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load bitmap font {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Draw text using a loaded bitmap font
        /// </summary>
        public void DrawBitmapText(string fontName, int x, int y, string text, AsmoColor tint)
        {
            if (!_loadedFonts.TryGetValue(fontName, out var font))
            {
                Console.WriteLine($"Bitmap font '{fontName}' not found. Load it first with LoadBitmapFont().");
                return;
            }

            font.DrawText(this, x, y, text, tint);
        }

        /// <summary>
        /// Draw with alpha blending (if enabled)
        /// </summary>
        public void DrawPixelBlended(int x, int y, AsmoColor color)
        {
            if (!Config.EnableBlending || color.A == 255)
            {
                SetPixel(x, y, color);
                return;
            }

            // Get current pixel
            var current = GetPixel(x, y);
            if (current == null) return;

            // Alpha blend
            float alpha = color.A / 255f;
            float invAlpha = 1f - alpha;

            var blended = new AsmoColor(
                (int)(color.R * alpha + current.R * invAlpha),
                (int)(color.G * alpha + current.G * invAlpha),
                (int)(color.B * alpha + current.B * invAlpha),
                Math.Max(color.A, current.A)
            );

            SetPixel(x, y, blended);
        }

        /// <summary>
        /// Get pixel color at coordinates
        /// </summary>
        public AsmoColor? GetPixel(int x, int y)
        {
            int arrayY = Height - 1 - y;
            if (x >= 0 && x < Width && arrayY >= 0 && arrayY < Height)
                return Pixels[x][arrayY];
            return null;
        }

        /// <summary>
        /// Get information about a loaded image
        /// </summary>
        public (int width, int height)? GetImageInfo(string name)
        {
            if (_loadedImages.TryGetValue(name, out var image))
            {
                return (image.Width, image.Height);
            }
            return null;
        }

        /// <summary>
        /// Unload a specific image to free memory
        /// </summary>
        public bool UnloadImage(string name)
        {
            if (_loadedImages.TryGetValue(name, out var image))
            {
                image.Dispose();
                _loadedImages.Remove(name);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public override void Dispose()
        {
            foreach (var image in _loadedImages.Values)
            {
                image.Dispose();
            }
            _loadedImages.Clear();
            _loadedFonts.Clear();
            base.Dispose();
        }
    }

    /// <summary>
    /// Bitmap font implementation using ImageSharp
    /// </summary>
    public class BitmapFont
    {
        private readonly Image<Rgba32> _image;
        private readonly int _charWidth;
        private readonly int _charHeight;
        private readonly Dictionary<char, (int x, int y)> _charPositions;

        public BitmapFont(Image<Rgba32> image, int charWidth, int charHeight, string charset)
        {
            _image = image;
            _charWidth = charWidth;
            _charHeight = charHeight;
            _charPositions = new Dictionary<char, (int x, int y)>();

            // Calculate character positions
            int charsPerRow = image.Width / charWidth;
            for (int i = 0; i < charset.Length; i++)
            {
                int row = i / charsPerRow;
                int col = i % charsPerRow;
                _charPositions[charset[i]] = (col * charWidth, row * charHeight);
            }
        }

        public void DrawText(EnhancedSurface surface, int x, int y, string text, AsmoColor tint)
        {
            int currentX = x;
            
            foreach (char c in text)
            {
                if (c == '\n')
                {
                    currentX = x;
                    y -= _charHeight;
                    continue;
                }

                if (_charPositions.TryGetValue(c, out var pos))
                {
                    DrawChar(surface, currentX, y, pos.x, pos.y, tint);
                }
                
                currentX += _charWidth;
            }
        }

        private void DrawChar(EnhancedSurface surface, int destX, int destY, int srcX, int srcY, AsmoColor tint)
        {
            for (int py = 0; py < _charHeight; py++)
            {
                for (int px = 0; px < _charWidth; px++)
                {
                    if (srcX + px < _image.Width && srcY + py < _image.Height)
                    {
                        var pixel = _image[srcX + px, srcY + py];
                        if (pixel.A > 0) // Only draw non-transparent pixels
                        {
                            // Apply tint
                            var tintedColor = new AsmoColor(
                                (pixel.R * tint.R) / 255,
                                (pixel.G * tint.G) / 255,
                                (pixel.B * tint.B) / 255,
                                (pixel.A * tint.A) / 255
                            );
                            
                            if (surface.Config.EnableBlending && tintedColor.A < 255)
                            {
                                surface.DrawPixelBlended(destX + px, destY + py, tintedColor);
                            }
                            else
                            {
                                surface.SetPixel(destX + px, destY + py, tintedColor);
                            }
                        }
                    }
                }
            }
        }
    }
}