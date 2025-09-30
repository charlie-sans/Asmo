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
        public static int ScreenWidth { get; set; } = 640;
        public static int ScreenHeight { get; set; } = 480;
        public static int WindowX { get; set; }
        public static int WindowY { get; set; }
    }
}
