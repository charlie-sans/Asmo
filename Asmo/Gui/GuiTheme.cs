using Asmo.Types;

namespace Asmo.Gui
{
    /// <summary>
    /// Represents a GUI theme (palette + metrics). Additional themes can be user-defined.
    /// </summary>
    public class GuiTheme
    {
        // Core colors
        public Color WindowBg { get; set; } = new Color(25, 28, 36, 255);
        public Color Text { get; set; } = new Color(230, 235, 245, 255);
        public Color TextDisabled { get; set; } = new Color(120, 125, 135, 255);
        public Color Border { get; set; } = new Color(70, 80, 95, 255);
        public Color BorderFocus { get; set; } = new Color(140, 180, 255, 255);

        // Buttons
        public Color Button { get; set; } = new Color(50, 80, 140, 255);
        public Color ButtonHovered { get; set; } = new Color(70, 110, 190, 255);
        public Color ButtonActive { get; set; } = new Color(40, 65, 110, 255);

        // Inputs
        public Color InputBg { get; set; } = new Color(40, 48, 60, 255);
        public Color InputBgActive { get; set; } = new Color(55, 66, 82, 255);
        public Color SliderGrab { get; set; } = new Color(180, 200, 255, 255);
        public Color ProgressBarFill { get; set; } = new Color(90, 150, 255, 255);
        public Color ToggleOn { get; set; } = new Color(90, 170, 90, 255);
        public Color ToggleOff { get; set; } = new Color(90, 90, 90, 255);

        // Misc
        public Color Highlight { get; set; } = new Color(255, 200, 100, 255);

        // Metrics
        public int FramePaddingX { get; set; } = 6;
        public int FramePaddingY { get; set; } = 4;
        public int ItemSpacingY { get; set; } = 22;
        public int ItemSpacingX { get; set; } = 8;
        public int IndentSize { get; set; } = 16;

        public static GuiTheme DarkDefault => new();
        public static GuiTheme Light => new GuiTheme
        {
            WindowBg = new Color(240, 240, 245, 255),
            Text = new Color(25, 30, 42, 255),
            TextDisabled = new Color(140, 140, 150, 255),
            Border = new Color(170, 170, 180, 255),
            BorderFocus = new Color(90, 120, 255, 255),
            Button = new Color(210, 215, 230, 255),
            ButtonHovered = new Color(190, 200, 225, 255),
            ButtonActive = new Color(160, 170, 200, 255),
            InputBg = new Color(230, 230, 240, 255),
            InputBgActive = new Color(215, 215, 230, 255),
            SliderGrab = new Color(90, 120, 255, 255),
            ProgressBarFill = new Color(120, 150, 250, 255),
            ToggleOn = new Color(70, 170, 90, 255),
            ToggleOff = new Color(110, 110, 120, 255),
            Highlight = new Color(255, 140, 60, 255)
        };
    }

    /// <summary>
    /// Simple struct representing per-frame navigational & typing input for GUI.
    /// Caller is responsible for populating based on actual input devices.
    /// </summary>
    public struct GuiInput
    {
        public bool Up;
        public bool Down;
        public bool Left;
        public bool Right;
        public bool Activate;     // Enter / Space / A (controller)
        public bool Back;         // Escape / B (controller)
        public bool NextField;    // Tab
        public bool PrevField;    // Shift+Tab
        public bool Backspace;
        public bool Delete;
        public bool MoveCursorLeft;
        public bool MoveCursorRight;
        public char[] TypedChars; // Raw typed characters this frame
    }
}
