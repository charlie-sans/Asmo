using Asmo.Gfx;
using Asmo.Window;
using Asmo.Window.input;
using Asmo.Types;

namespace Asmo.Gui
{
    /// <summary>
    /// Helper class for GUI setup and mouse handling
    /// </summary>
    public static class GuiSetup
    {
        /// <summary>
        /// Get mouse state relative to the framebuffer for GUI interactions
        /// </summary>
        public static (int mouseX, int mouseY, bool mousePressed) GetMouseState(Surface framebuffer)
        {
            if (framebuffer?.Window?.Mouse == null)
                return (0, 0, false);

            var mouse = framebuffer.Window.Mouse;
            return (mouse.X, mouse.Y, mouse.IsPressed);
        }

        /// <summary>
        /// Create a GUI settings panel for window configuration
        /// </summary>
        public static class SettingsPanel
        {
            private static bool _showAdvanced = false;
            private static string[] _qualityOptions = { "Retro", "Enhanced", "High Quality" };
            private static int _selectedQuality = 0;
            private static bool _vsyncEnabled = true;
            private static bool _fullscreenEnabled = false;
            private static bool _initialized = false;

            /// <summary>
            /// Render a settings panel that allows runtime configuration changes
            /// </summary>
            public static void Render(Surface surface, WindowManager windowManager, int x = 10, int y = 10)
            {
                var (mouseX, mouseY, mousePressed) = GetMouseState(surface);
                
                // Initialize settings from current window state (only once)
                if (!_initialized)
                {
                    var (settings, config) = windowManager.GetCurrentConfiguration();
                    _selectedQuality = (int)config.Quality;
                    _vsyncEnabled = settings.VSync;
                    _fullscreenEnabled = settings.Fullscreen;
                    _initialized = true;
                    Console.WriteLine($"Settings panel initialized - Quality: {config.Quality}, VSync: {_vsyncEnabled}");
                }
                
                Gui.Begin(x, y);
                Gui.Label(surface, "=== WINDOW SETTINGS ===", Colors.Yellow);
                
                // Show current window info
                var (currentSettings, currentConfig) = windowManager.GetCurrentConfiguration();
                Gui.Label(surface, $"Current: {currentConfig.Quality}", Colors.Cyan);
                Gui.Label(surface, $"Resolution: {currentSettings.Width}x{currentSettings.Height}", Colors.White);
                Gui.Label(surface, $"Framebuffer: {currentSettings.FramebufferWidth}x{currentSettings.FramebufferHeight}", Colors.White);
                
                // Quality selector
                Gui.Label(surface, $"Select Quality: {_qualityOptions[_selectedQuality]}", Colors.White);
                if (Gui.Button(surface, "< Prev Quality", Colors.Cyan, mouseX, mouseY, mousePressed))
                {
                    _selectedQuality = (_selectedQuality - 1 + _qualityOptions.Length) % _qualityOptions.Length;
                    var quality = (RenderingQuality)_selectedQuality;
                    Console.WriteLine($"Switching to quality: {quality}");
                    windowManager.SetQuality(quality);
                }
                
                if (Gui.Button(surface, "Next Quality >", Colors.Cyan, mouseX, mouseY, mousePressed))
                {
                    _selectedQuality = (_selectedQuality + 1) % _qualityOptions.Length;
                    var quality = (RenderingQuality)_selectedQuality;
                    Console.WriteLine($"Switching to quality: {quality}");
                    windowManager.SetQuality(quality);
                }

                // Quick setup buttons
                Gui.Label(surface, "--- Quick Setups ---", Colors.Yellow);
                if (Gui.Button(surface, "Pixel Art Mode", Colors.Green, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying Pixel Art setup");
                    WindowManager.QuickSetup.PixelArt(windowManager);
                    _selectedQuality = (int)RenderingQuality.Retro;
                }
                
                if (Gui.Button(surface, "Smooth Graphics", Colors.Green, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying Smooth setup");
                    WindowManager.QuickSetup.Smooth(windowManager);
                    _selectedQuality = (int)RenderingQuality.Enhanced;
                }
                
                if (Gui.Button(surface, "Full Featured", Colors.Green, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying Full Featured setup");
                    WindowManager.QuickSetup.FullFeatured(windowManager);
                    _selectedQuality = (int)RenderingQuality.HighQuality;
                }

                // Window presets
                Gui.Label(surface, "--- Window Presets ---", Colors.Yellow);
                if (Gui.Button(surface, "Retro Preset", Colors.Magenta, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying Retro preset");
                    windowManager.ApplyPreset(WindowSettings.Presets.Retro);
                }
                
                if (Gui.Button(surface, "Modern Preset", Colors.Magenta, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying Modern preset");
                    windowManager.ApplyPreset(WindowSettings.Presets.Modern);
                }
                
                if (Gui.Button(surface, "HD Preset", Colors.Magenta, mouseX, mouseY, mousePressed))
                {
                    Console.WriteLine("Applying HD preset");
                    windowManager.ApplyPreset(WindowSettings.Presets.HighRes);
                }

                // Toggle options
                if (Gui.Checkbox(surface, "Show Advanced", ref _showAdvanced, Colors.Cyan, mouseX, mouseY, mousePressed))
                {
                    // Advanced options toggled
                }

                if (_showAdvanced)
                {
                    Gui.Label(surface, "--- Advanced Options ---", Colors.Gray);
                    
                    if (Gui.Checkbox(surface, "V-Sync", ref _vsyncEnabled, Colors.White, mouseX, mouseY, mousePressed))
                    {
                        Console.WriteLine($"Setting VSync: {_vsyncEnabled}");
                        windowManager.SetVSync(_vsyncEnabled);
                    }
                    
                    if (Gui.Checkbox(surface, "Fullscreen", ref _fullscreenEnabled, Colors.White, mouseX, mouseY, mousePressed))
                    {
                        Console.WriteLine($"Setting Fullscreen: {_fullscreenEnabled}");
                        windowManager.SetFullscreen(_fullscreenEnabled);
                    }
                    
                    if (Gui.Button(surface, "Reset to Defaults", Colors.Red, mouseX, mouseY, mousePressed))
                    {
                        Console.WriteLine("Resetting to defaults");
                        windowManager.ResetToDefaults();
                        _selectedQuality = 0;
                        _vsyncEnabled = true;
                        _fullscreenEnabled = false;
                        _showAdvanced = false;
                    }
                }
            }
        }
    }
}