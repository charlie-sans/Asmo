using Asmo;
#nullable enable
using Asmo.Gfx;
using Asmo.Gui;
using Asmo.Types;
using Asmo.Window.input;
using System.Collections.Generic;
namespace GuiDemo
{
    /// <summary>
    /// Demonstrates the enhanced immediate-mode GUI features (theming, widgets, navigation).
    /// </summary>
    public class Game : IConsoleGame
    {
        private Keyboard? kb;
        private Mouse? mouse;

        // Demo state
        private bool showSettings = true;
        private bool musicEnabled = true;
        private float volume = 75f;
        private float progress = 0.35f;
        private string playerName = "Player1";
        private bool altTheme = false;
        private double timeAccum;
        private readonly List<string> log = new();
    private int qualityIndex = 1;
    private readonly string[] qualityOptions = new[] { "Low", "Medium", "High", "Ultra" };

        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
            mouse = new Mouse(surface.Window);
            Gui.SetTheme(GuiTheme.DarkDefault);
        }

        public void Update(double deltaTime)
        {
            timeAccum += deltaTime;
            // Fake progress pulse to show progress bar
            progress = (float)((System.Math.Sin(timeAccum * 0.8) * 0.5) + 0.5);
        }

        private GuiInput BuildGuiInput()
        {
            if (kb == null) return default;
            bool shift = kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift) || kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightShift);
            var typed = System.Array.Empty<char>();
            return new GuiInput
            {
                Up = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up),
                Down = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down),
                Left = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left),
                Right = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right),
                Activate = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter) || kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space),
                Back = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape),
                NextField = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Tab) && !shift,
                PrevField = kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Tab) && shift,
                Backspace = kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace),
                TypedChars = typed ?? System.Array.Empty<char>()
            };
        }

        public void Draw(Surface surface)
        {
            surface.Clear(new Color(15, 18, 24, 255));
            surface.DrawText(10, 6, "GUI Demo", Colors.Yellow);
            surface.DrawText(10, 18, "Arrow / Tab to navigate, Enter/Space to activate.", Colors.White);
            surface.DrawText(10, 28, "Type in the text field when it has focus.", Colors.White);

            int mouseX = mouse?.X ?? 0;
            int mouseY = mouse?.Y ?? 0;
            bool mouseDown = mouse?.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left) ?? false;

            var nav = BuildGuiInput();
            Gui.BeginFrame(16, 50, new Gui.GuiFrameInput { Nav = nav, MouseX = mouseX, MouseY = mouseY, MouseDown = mouseDown });
            Gui.Label(surface, "Settings Panel:");
            Gui.PushIndent();

            if (Gui.Button(surface, showSettings ? "Hide Settings" : "Show Settings"))
            {
                showSettings = !showSettings;
                log.Add("Toggled settings panel");
            }

            if (showSettings)
            {
                Gui.Label(surface, "Audio:");
                Gui.PushIndent();
                Gui.ToggleSwitch(surface, "Music Enabled", ref musicEnabled);
                Gui.SliderFloat(surface, "Volume", ref volume, 0, 100, 220);
                Gui.ProgressBar(surface, progress, 220, 12);
                Gui.PopIndent();

                Gui.Label(surface, "Profile:");
                Gui.PushIndent();
                Gui.TextInput(surface, "Name", ref playerName, 180);
                if (Gui.Dropdown(surface, "Quality", qualityOptions, ref qualityIndex, 140))
                {
                    log.Add($"Quality set to {qualityOptions[qualityIndex]}");
                }
                Gui.PopIndent();

                Gui.Label(surface, "Theme:");
                Gui.PushIndent();
                if (Gui.Button(surface, altTheme ? "Use Dark Theme" : "Use Light Theme"))
                {
                    altTheme = !altTheme;
                    Gui.SetTheme(altTheme ? GuiTheme.Light : GuiTheme.DarkDefault);
                    log.Add("Switched theme");
                }
                Gui.PopIndent();
            }

            Gui.PopIndent();

            Gui.Label(surface, "Log:");
            Gui.PushIndent();
            int maxLog = 6;
            for (int i = log.Count - maxLog; i < log.Count; i++)
            {
                if (i < 0) continue;
                Gui.Label(surface, log[i]);
            }
            Gui.PopIndent();

            Gui.EndFrame();
        }
    }
}
