# Enhanced Graphics System Documentation

The Asmo graphics system now supports multiple quality levels and advanced features like high-quality image loading, bitmap fonts, alpha blending, and more!

## Quick Start

### Creating Windows with Different Quality Levels

```csharp
// Basic retro window (pixel perfect, simple graphics)
var retroWindow = AsmoSetup.CreateRetroWindow("My Retro Game");

// Modern window with enhanced graphics
var modernWindow = AsmoSetup.CreateModernWindow("My Modern Game");

// High-quality window with all features
var hqWindow = AsmoSetup.CreateHighQualityWindow("My HD Game");

// Custom configuration
var customWindow = AsmoSetup.CreateCustomWindow(
    WindowSettings.Presets.Modern, 
    RenderingQuality.Enhanced
);
```

### Runtime Quality Changes

```csharp
// Get the window manager
var manager = window.Manager;

// Quick setups for different scenarios
WindowManager.QuickSetup.PixelArt(manager);      // Crisp pixels, no filtering
WindowManager.QuickSetup.Smooth(manager);        // Smooth scaling, anti-aliasing
WindowManager.QuickSetup.Performance(manager);   // Minimal features for speed
WindowManager.QuickSetup.FullFeatured(manager);  // All features enabled

// Manual configuration
manager.SetQuality(RenderingQuality.HighQuality);
manager.SetGraphicsFeature(GraphicsFeature.AntiAliasing, true);
manager.SetGraphicsFeature(GraphicsFeature.Blending, true);
```

## Quality Levels

### Retro Mode
- **Features**: Basic pixel rendering, built-in fonts, simple shapes
- **Best for**: Pixel art games, retro aesthetics, maximum performance
- **File formats**: None (basic drawing only)

### Enhanced Mode  
- **Features**: Image loading, bitmap fonts, alpha blending, filtered scaling
- **Best for**: Modern 2D games, UI-heavy applications
- **File formats**: PNG, JPEG, BMP, GIF, TIFF, WebP

### High Quality Mode
- **Features**: All Enhanced features + anti-aliasing, high-res textures
- **Best for**: High-resolution games, smooth graphics
- **Max texture size**: 4096x4096 pixels

## Enhanced Surface Features

### Image Loading and Display

```csharp
surface.WithEnhanced(enhanced =>
{
    // Load various image formats
    enhanced.LoadImage("logo", "Assets/logo.png");
    enhanced.LoadImage("background", "Assets/bg.jpg");
    enhanced.LoadImage("sprite", "Assets/character.gif");
    
    // Draw images with scaling and effects
    enhanced.DrawImage("logo", 10, 10);                    // Original size
    enhanced.DrawImage("logo", 50, 50, 64, 64);          // Scaled
    enhanced.DrawImage("logo", 100, 100, 128, 128, 0.5f); // Semi-transparent
    enhanced.DrawImage("sprite", 200, 200, flipX: true);   // Flipped
    
    // Create sprite regions for sprite sheets
    enhanced.CreateImageFromRegion("sprite", "walk1", 0, 0, 32, 32);
    enhanced.CreateImageFromRegion("sprite", "walk2", 32, 0, 32, 32);
    enhanced.DrawImage("walk1", playerX, playerY);
});
```

### Bitmap Fonts

```csharp
surface.WithEnhanced(enhanced =>
{
    // Load bitmap font (8x8 character size)
    enhanced.LoadBitmapFont("pixel", "Assets/pixel_font.png", 8, 8);
    
    // Draw text with different colors
    enhanced.DrawBitmapText("pixel", 10, 100, "Hello World!", Colors.White);
    enhanced.DrawBitmapText("pixel", 10, 120, "Colored text!", Colors.Yellow);
    
    // Multi-line text support
    enhanced.DrawBitmapText("pixel", 10, 140, "Line 1\nLine 2\nLine 3", Colors.Cyan);
});
```

### Alpha Blending

```csharp
surface.WithEnhanced(enhanced =>
{
    if (enhanced.Config.EnableBlending)
    {
        // Semi-transparent colors
        var semiRed = new Color(255, 0, 0, 128);    // 50% transparent red
        var semiBlue = new Color(0, 0, 255, 200);   // 78% opaque blue
        
        // Blend with existing pixels
        enhanced.DrawPixelBlended(x, y, semiRed);
        
        // Images with alpha blending
        enhanced.DrawImage("overlay", 0, 0, alpha: 0.3f);
    }
});
```

## Helper Methods and Extensions

### Quick Drawing Methods

```csharp
// Quick image drawing without manual loading
surface.QuickDrawImage("Assets/splash.png", 0, 0, 320, 240);

// Quick bitmap text without manual font loading  
surface.QuickDrawBitmapText("Assets/font.png", 8, 8, 10, 10, "Quick Text!", Colors.Cyan);
```

### Feature Detection

```csharp
// Check if enhanced features are available
if (surface.IsEnhanced())
{
    Console.WriteLine("Enhanced features available!");
    Console.WriteLine(surface.GetFeatureSummary());
    
    var config = surface.GetGraphicsConfig();
    if (config.EnableImageLoading)
    {
        // Load and use images
    }
}
```

### Visual Effects

```csharp
// Apply screen effects
surface.ApplyEffect(SurfaceEffect.Fade, 0.5f);    // 50% fade to black
surface.ApplyEffect(SurfaceEffect.Tint, 0.3f);    // Slight red tint
```

## Settings Panel Integration

The system includes a built-in settings panel for runtime configuration:

```csharp
// In your render loop
if (showSettings)
{
    var (x, y) = GuiPlacement.Left.Top(surface, 10, 50);
    GuiSetup.SettingsPanel.Render(surface, window.Manager, x, y);
}
```

The settings panel allows users to:
- Switch between quality levels (Retro/Enhanced/High Quality)
- Apply window presets (Retro/Modern/HD)
- Toggle advanced options (VSync, Fullscreen)
- Reset to defaults

## Performance Considerations

### Memory Management

```csharp
surface.WithEnhanced(enhanced =>
{
    // Unload specific images when done
    enhanced.UnloadImage("large_background");
    
    // Get image information
    var info = enhanced.GetImageInfo("sprite");
    if (info.HasValue)
    {
        Console.WriteLine($"Size: {info.Value.width}x{info.Value.height}");
    }
});

// Surfaces automatically dispose resources when the window closes
```

### Quality vs Performance

- **Retro Mode**: Fastest, minimal memory usage
- **Enhanced Mode**: Good balance of features and performance  
- **High Quality Mode**: Best visuals, highest resource usage

### Best Practices

1. **Use appropriate quality for your target**: Retro for pixel art games, Enhanced for most 2D games, High Quality for premium experiences
2. **Preload assets**: Load images and fonts during initialization, not during gameplay
3. **Manage memory**: Unload unused images, especially large ones
4. **Test on target hardware**: Higher quality modes may not be suitable for all devices

## Integration Examples

### Game Development

```csharp
public class MyGame : IConsoleGame
{
    public void Init(Surface surface)
    {
        // Setup for pixel art game
        if (surface.Window != null)
        {
            WindowManager.QuickSetup.PixelArt(surface.Window.Manager);
        }
        
        // Load game assets
        surface.WithEnhanced(enhanced =>
        {
            enhanced.LoadImage("player", "Assets/player.png");
            enhanced.LoadImage("tiles", "Assets/tileset.png");
            enhanced.LoadBitmapFont("ui", "Assets/ui_font.png", 8, 8);
        });
    }
    
    public void Draw(Surface surface)
    {
        surface.WithEnhanced(enhanced =>
        {
            // Draw game world
            enhanced.DrawImage("tiles", 0, 0);
            enhanced.DrawImage("player", playerX, playerY);
            
            // Draw UI
            enhanced.DrawBitmapText("ui", 10, surface.Height - 20, 
                $"Score: {score}", Colors.White);
        });
    }
}
```

### Settings Menu

```csharp
public void RenderSettingsMenu(Surface surface)
{
    var (mouseX, mouseY, mousePressed) = GuiSetup.GetMouseState(surface);
    
    Gui.Begin(50, surface.Height - 100);
    
    if (Gui.Button(surface, "Pixel Art Mode", Colors.Green, mouseX, mouseY, mousePressed))
    {
        WindowManager.QuickSetup.PixelArt(surface.Window.Manager);
    }
    
    if (Gui.Button(surface, "High Quality Mode", Colors.Blue, mouseX, mouseY, mousePressed))
    {
        WindowManager.QuickSetup.FullFeatured(surface.Window.Manager);
    }
    
    Gui.Label(surface, surface.GetFeatureSummary(), Colors.White);
}
```

This enhanced graphics system provides a powerful foundation for both retro-style pixel art games and modern high-quality 2D applications, with easy runtime switching between quality modes and comprehensive ImageSharp integration for maximum file format support!