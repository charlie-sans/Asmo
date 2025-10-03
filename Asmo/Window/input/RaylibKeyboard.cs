#if USE_RAYLIB
using System;
using System.Collections.Generic;
using Raylib_cs;

namespace Asmo.Window.input
{
    /// <summary>
    /// Frame-polled keyboard state for Raylib backend. Provides edge detection and text input buffering.
    /// </summary>
    public sealed class RaylibKeyboard
    {
        private readonly HashSet<KeyboardKey> _down = new();
        private readonly HashSet<KeyboardKey> _pressed = new();
        private readonly HashSet<KeyboardKey> _released = new();
        private readonly List<char> _typed = new();

        public void Update()
        {
            _pressed.Clear();
            _released.Clear();
            _typed.Clear();

            // Iterate over keys we care about (add more as needed). Raylib lacks an API to enumerate all.
            Span<KeyboardKey> interesting = stackalloc KeyboardKey[]
            {
                KeyboardKey.KEY_LEFT, KeyboardKey.KEY_RIGHT, KeyboardKey.KEY_UP, KeyboardKey.KEY_DOWN,
                KeyboardKey.KEY_ENTER, KeyboardKey.KEY_BACKSPACE, KeyboardKey.KEY_TAB,
                KeyboardKey.KEY_HOME, KeyboardKey.KEY_END,
                KeyboardKey.KEY_SPACE,
                KeyboardKey.KEY_A, KeyboardKey.KEY_B, KeyboardKey.KEY_C, KeyboardKey.KEY_D, KeyboardKey.KEY_E,
                KeyboardKey.KEY_F, KeyboardKey.KEY_G, KeyboardKey.KEY_H, KeyboardKey.KEY_I, KeyboardKey.KEY_J,
                KeyboardKey.KEY_K, KeyboardKey.KEY_L, KeyboardKey.KEY_M, KeyboardKey.KEY_N, KeyboardKey.KEY_O,
                KeyboardKey.KEY_P, KeyboardKey.KEY_Q, KeyboardKey.KEY_R, KeyboardKey.KEY_S, KeyboardKey.KEY_T,
                KeyboardKey.KEY_U, KeyboardKey.KEY_V, KeyboardKey.KEY_W, KeyboardKey.KEY_X, KeyboardKey.KEY_Y,
                KeyboardKey.KEY_Z,
                KeyboardKey.KEY_ZERO, KeyboardKey.KEY_ONE, KeyboardKey.KEY_TWO, KeyboardKey.KEY_THREE,
                KeyboardKey.KEY_FOUR, KeyboardKey.KEY_FIVE, KeyboardKey.KEY_SIX, KeyboardKey.KEY_SEVEN,
                KeyboardKey.KEY_EIGHT, KeyboardKey.KEY_NINE
            };

            foreach (var key in interesting)
            {
                bool isDown = Raylib.IsKeyDown(key);
                bool wasDown = _down.Contains(key);
                if (isDown)
                {
                    if (!wasDown)
                        _pressed.Add(key);
                    _down.Add(key);
                }
                else if (wasDown)
                {
                    _down.Remove(key);
                    _released.Add(key);
                }
            }

            // Text input: gather pressed characters (limited). Raylib.GetCharPressed drains an internal queue.
            int ch;
            while ((ch = Raylib.GetCharPressed()) > 0)
            {
                if (ch is >= 32 and <= 126) // printable ASCII
                    _typed.Add((char)ch);
            }
        }

        public bool IsKeyDown(KeyboardKey key) => _down.Contains(key);
        public bool IsKeyPressed(KeyboardKey key) => _pressed.Contains(key);
        public bool IsKeyReleased(KeyboardKey key) => _released.Contains(key);
        public IReadOnlyList<char> GetPressedChars() => _typed;
    }
}
#endif
