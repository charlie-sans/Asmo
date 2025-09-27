namespace Asmo
{
    public static class GameEnvironment
    {
        public static string? AssetRoot { get; set; }
        // New properties for configuration and UI
        public static string WindowTitle { get; set; } = "Asmo Game";
        public static string DefaultFont { get; set; } = "Assets/Font/Default.fnt";
        public static bool ShowFps { get; set; } = false;
        public static string ConfigPath { get; set; } = "config.json";
    }
}
