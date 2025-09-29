using Asmo;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using Asmo.Window.input;
using System;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AstroTestGame
{
    public class Game : Asmo.Gfx.IConsoleGame
    {
    private Keyboard? kb = null;
    private readonly HashSet<Keys> prevKeysDown = new();
        private readonly List<string> outputLines = new();
        private string inputBuffer = "";
        private int maxLines = 20;
        private bool showCursor = true;
        private double cursorBlink = 0;
        private bool exitRequested = false;
        private AudioEngine? _audio;
        private AudioBus? _sfxBus;
        private AudioClip? _keyClip;

        public Game() { }

        public void Init(Asmo.Gfx.Surface surface)
        {
            kb = new Keyboard(surface.Window);
            outputLines.Clear();
            inputBuffer = "";
            outputLines.Add("Simple GUI Terminal Emulator. Type 'help' for commands. Type 'exit' to quit.");
            exitRequested = false;
            InitialiseAudio();
        }

        public void Update(double deltaTime)
        {
            _audio?.Update(deltaTime);
            if (kb == null) return;
            // Handle key input for text entry
            bool shift = IsShiftDown();
            var keysToCheck = new List<Keys>();
            for (var k = Keys.A; k <= Keys.Z; k++)
                keysToCheck.Add(k);
            for (var k = Keys.D0; k <= Keys.D9; k++)
                keysToCheck.Add(k);
            keysToCheck.AddRange(new[] {
                Keys.Space,
                Keys.Period,
                Keys.Comma,
                Keys.Minus,
                Keys.Equal,
                Keys.Slash,
                Keys.Backslash,
                Keys.Semicolon,
                Keys.Apostrophe,
                Keys.LeftBracket,
                Keys.RightBracket
            });

            // Enter
            if (IsKeyJustPressed(Keys.Enter))
            {
                ProcessCommand(inputBuffer);
                inputBuffer = "";
                PlayKeySound();
            }
            // Backspace
            else if (IsKeyJustPressed(Keys.Backspace))
            {
                if (inputBuffer.Length > 0)
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                PlayKeySound();
            }
            else
            {
                foreach (var k in keysToCheck)
                {
                    if (IsKeyJustPressed(k) && inputBuffer.Length < 64)
                    {
                        char? ch = null;
                        if (k >= Keys.A && k <= Keys.Z)
                        {
                            char c = (char)('a' + (k - Keys.A));
                            ch = shift ? char.ToUpper(c) : c;
                        }
                        else if (k >= Keys.D0 && k <= Keys.D9)
                        {
                            ch = (char)('0' + (k - Keys.D0));
                        }
                        else
                        {
                            ch = KeyToChar(k, shift);
                        }
                        if (ch != null)
                        {
                            inputBuffer += ch;
                            PlayKeySound();
                        }
                    }
                }
            }
            // Cursor blink
            cursorBlink += deltaTime;
            if (cursorBlink > 0.5)
            {
                showCursor = !showCursor;
                cursorBlink = 0;
            }

            // Update prevKeysDown for next frame
            prevKeysDown.Clear();
            foreach (var k in keysToCheck)
                if (kb.IsKeyDown(k)) prevKeysDown.Add(k);
            if (kb.IsKeyDown(Keys.Enter)) prevKeysDown.Add(Keys.Enter);
            if (kb.IsKeyDown(Keys.Backspace)) prevKeysDown.Add(Keys.Backspace);
        }
        // Returns true if the key is down this frame but was not down last frame
        private bool IsKeyJustPressed(Keys key)
        {
            if (kb == null) return false;
            return kb.IsKeyDown(key) && !prevKeysDown.Contains(key);
        
        }

        public void Draw(Asmo.Gfx.Surface surface)
        {
            surface.Clear(Asmo.Gfx.Colors.Black);
            int x = 0, y = 24, lineH = 20;
            int start = Math.Max(0, outputLines.Count - maxLines);
            for (int i = start; i < outputLines.Count; i++)
            {
                surface.DrawText(x, y, outputLines[i], Asmo.Gfx.Colors.White);
                y += lineH;
            }
            // Draw prompt and input
            string prompt = "> " + inputBuffer + (showCursor ? "_" : " ");
            surface.DrawText(x, y, prompt, Asmo.Gfx.Colors.Yellow);
        }

        private void ProcessCommand(string cmd)
        {
            outputLines.Add("> " + cmd);
            if (cmd.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                outputLines.Add("Exiting terminal emulator.");
                exitRequested = true;
            }
            else if (cmd.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                outputLines.Add("Available commands:");
                outputLines.Add("  help  - Show this help message");
                outputLines.Add("  echo  - Echo back your input");
                outputLines.Add("  exit  - Exit the terminal emulator");
            }
            else if (cmd.StartsWith("echo ", StringComparison.OrdinalIgnoreCase))
            {
                outputLines.Add(cmd.Substring(5));
            }
            else if (cmd.Equals("echo", StringComparison.OrdinalIgnoreCase))
            {
                outputLines.Add("");
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                outputLines.Add($"Unknown command: {cmd}");
            }
            // Limit output lines
            if (outputLines.Count > 100)
                outputLines.RemoveRange(0, outputLines.Count - 100);
            PlayKeySound();
        }

        // Simple key-to-char mapping for symbols
        private char? KeyToChar(Keys key, bool shift)
        {
            switch (key)
            {
                case Keys.Space: return ' ';
                case Keys.Period: return '.';
                case Keys.Comma: return ',';
                case Keys.Minus: return '-';
                case Keys.Equal: return '=';
                case Keys.Slash: return '/';
                case Keys.Backslash: return '\\';
                case Keys.Semicolon: return ';';
                case Keys.Apostrophe: return '\'';
                case Keys.LeftBracket: return '[';
                case Keys.RightBracket: return ']';
            }
            return null;
        }

        // Helper to check if shift is down
        private bool IsShiftDown()
        {
            if (kb == null) return false;
            return kb.IsKeyDown(Keys.LeftShift) ||
                   kb.IsKeyDown(Keys.RightShift);
        }

        private void InitialiseAudio()
        {
            _audio = new AudioEngine();
            _sfxBus = _audio.GetOrCreateBus("sfx");
            _sfxBus.Volume = 0.75f;
            _keyClip = AudioClip.CreateSquare(980, 0.08, 0.25f);
        }

        private void PlayKeySound()
        {
            if (_sfxBus == null || _keyClip == null)
                return;

            _sfxBus.Play(_keyClip, new AudioPlaybackSettings
            {
                Volume = 0.5f,
                FadeInSeconds = 0.005
            });
        }
    }
}
