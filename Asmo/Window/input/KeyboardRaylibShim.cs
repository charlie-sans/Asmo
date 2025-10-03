#if USE_RAYLIB
using System.Collections.Generic;
using Raylib_cs;
using OTKeys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace Asmo.Window.input
{
    // Provides original Keyboard API surface for demos, backed by Raylib.
    public class Keyboard
    {
        public Keyboard() {}
        // Legacy demos call new Keyboard(surface.Window); accept and ignore parameter
        public Keyboard(object? _) {}
        private readonly HashSet<OTKeys> _down = new();
        private readonly HashSet<OTKeys> _pressed = new();
        private readonly HashSet<OTKeys> _released = new();
        private readonly List<char> _typed = new();

        // Call once per frame (at end) in demos currently -> keep semantics identical
        public void Update()
        {
            _pressed.Clear(); _released.Clear(); _typed.Clear();
            // poll interesting keys
            foreach (var map in _map)
            {
                bool isDown = Raylib.IsKeyDown(map.KeyRay);
                bool wasDown = _down.Contains(map.KeyOT);
                if (isDown)
                {
                    if (!wasDown) _pressed.Add(map.KeyOT);
                    _down.Add(map.KeyOT);
                }
                else if (wasDown)
                {
                    _down.Remove(map.KeyOT);
                    _released.Add(map.KeyOT);
                }
            }
            int ch;
            while ((ch = Raylib.GetCharPressed()) > 0)
            {
                if (ch is >= 32 and <= 126) _typed.Add((char)ch);
            }
        }

        public bool IsKeyDown(OTKeys k) => _down.Contains(k);
        public bool IsKeyPressed(OTKeys k) => _pressed.Contains(k);
        public bool IsKeyReleased(OTKeys k) => _released.Contains(k);
        public IEnumerable<char> GetPressedChars() => _typed;

        private readonly (OTKeys KeyOT, KeyboardKey KeyRay)[] _map = new[]
        {
            (OTKeys.Left, KeyboardKey.KEY_LEFT),
            (OTKeys.Right, KeyboardKey.KEY_RIGHT),
            (OTKeys.Up, KeyboardKey.KEY_UP),
            (OTKeys.Down, KeyboardKey.KEY_DOWN),
            (OTKeys.Home, KeyboardKey.KEY_HOME),
            (OTKeys.End, KeyboardKey.KEY_END),
            (OTKeys.Enter, KeyboardKey.KEY_ENTER),
            (OTKeys.Backspace, KeyboardKey.KEY_BACKSPACE),
            (OTKeys.Tab, KeyboardKey.KEY_TAB),
            (OTKeys.Space, KeyboardKey.KEY_SPACE),
            (OTKeys.Escape, KeyboardKey.KEY_ESCAPE),
            (OTKeys.B, KeyboardKey.KEY_B),(OTKeys.I, KeyboardKey.KEY_I),(OTKeys.U, KeyboardKey.KEY_U),(OTKeys.W, KeyboardKey.KEY_W),
            (OTKeys.A, KeyboardKey.KEY_A),(OTKeys.S, KeyboardKey.KEY_S),(OTKeys.D, KeyboardKey.KEY_D),
            (OTKeys.P, KeyboardKey.KEY_P),(OTKeys.Z, KeyboardKey.KEY_Z),(OTKeys.C, KeyboardKey.KEY_C),(OTKeys.E, KeyboardKey.KEY_E),
            (OTKeys.F, KeyboardKey.KEY_F),(OTKeys.X, KeyboardKey.KEY_X),(OTKeys.V, KeyboardKey.KEY_V),(OTKeys.G, KeyboardKey.KEY_G),
            (OTKeys.H, KeyboardKey.KEY_H),(OTKeys.N, KeyboardKey.KEY_N),(OTKeys.J, KeyboardKey.KEY_J),(OTKeys.M, KeyboardKey.KEY_M),
            (OTKeys.L, KeyboardKey.KEY_L),
            (OTKeys.D0, KeyboardKey.KEY_ZERO),
            (OTKeys.D1, KeyboardKey.KEY_ONE),
            (OTKeys.D2, KeyboardKey.KEY_TWO),
            (OTKeys.D3, KeyboardKey.KEY_THREE),
            (OTKeys.D4, KeyboardKey.KEY_FOUR),
            (OTKeys.D5, KeyboardKey.KEY_FIVE),
            (OTKeys.D6, KeyboardKey.KEY_SIX),
            (OTKeys.D7, KeyboardKey.KEY_SEVEN),
            (OTKeys.D8, KeyboardKey.KEY_EIGHT),
            (OTKeys.D9, KeyboardKey.KEY_NINE),
            (OTKeys.Period, KeyboardKey.KEY_PERIOD),
            (OTKeys.Comma, KeyboardKey.KEY_COMMA),
            (OTKeys.Minus, KeyboardKey.KEY_MINUS),
            (OTKeys.Equal, KeyboardKey.KEY_EQUAL),
            (OTKeys.Slash, KeyboardKey.KEY_SLASH),
            (OTKeys.Backslash, KeyboardKey.KEY_BACKSLASH),
            (OTKeys.Semicolon, KeyboardKey.KEY_SEMICOLON),
            (OTKeys.Apostrophe, KeyboardKey.KEY_APOSTROPHE),
            (OTKeys.LeftBracket, KeyboardKey.KEY_LEFT_BRACKET),
            (OTKeys.RightBracket, KeyboardKey.KEY_RIGHT_BRACKET),
            (OTKeys.PageUp, KeyboardKey.KEY_PAGE_UP),(OTKeys.PageDown, KeyboardKey.KEY_PAGE_DOWN),
            (OTKeys.F1, KeyboardKey.KEY_F1),(OTKeys.F2, KeyboardKey.KEY_F2),(OTKeys.F3, KeyboardKey.KEY_F3),(OTKeys.F4, KeyboardKey.KEY_F4),
            (OTKeys.LeftShift, KeyboardKey.KEY_LEFT_SHIFT),
            (OTKeys.RightShift, KeyboardKey.KEY_RIGHT_SHIFT),
            (OTKeys.LeftControl, KeyboardKey.KEY_LEFT_CONTROL),(OTKeys.RightControl, KeyboardKey.KEY_RIGHT_CONTROL),
            (OTKeys.Delete, KeyboardKey.KEY_DELETE)
        };
    }
}
#endif
