using Asmo;
using Asmo.Sound;
using Asmo.Window.input;
using CSCore.SoundOut;
using Megadrive;
using Microsoft.Xna.Framework;
using System;
using System.Threading;
using Asmo.Types;
using Asmo.Gfx;
using CSCore.Codecs;
using CSCore;

namespace AstroTestGame
{
    public class Game : Asmo.Gfx.IConsoleGame
    {
    private Keyboard? kb = null;
    private System.Collections.Generic.HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> prevKeysDown = new();
        private System.Collections.Generic.List<string> outputLines = new System.Collections.Generic.List<string>();
        private string inputBuffer = "";
        private int maxLines = 20;
        private bool showCursor = true;
        private double cursorBlink = 0;
        private bool exitRequested = false;

        public Game() { }

        public void Init(Asmo.Gfx.Surface surface)
        {
            kb = new Keyboard(surface.Window);
            outputLines.Clear();
            inputBuffer = "";
            outputLines.Add("Simple GUI Terminal Emulator. Type 'help' for commands. Type 'exit' to quit.");
            exitRequested = false;
        }

        public void Update(double deltaTime)
        {
            if (kb == null) return;
            // Handle key input for text entry
            bool shift = IsShiftDown();
            var keysToCheck = new System.Collections.Generic.List<OpenTK.Windowing.GraphicsLibraryFramework.Keys>();
            for (var k = OpenTK.Windowing.GraphicsLibraryFramework.Keys.A; k <= OpenTK.Windowing.GraphicsLibraryFramework.Keys.Z; k++)
                keysToCheck.Add(k);
            for (var k = OpenTK.Windowing.GraphicsLibraryFramework.Keys.D0; k <= OpenTK.Windowing.GraphicsLibraryFramework.Keys.D9; k++)
                keysToCheck.Add(k);
            keysToCheck.AddRange(new[] {
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Period,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Comma,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Minus,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Equal,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Slash,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backslash,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Semicolon,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.Apostrophe,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftBracket,
                OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightBracket
            });

            // Enter
            if (IsKeyJustPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
            {
                ProcessCommand(inputBuffer);
                inputBuffer = "";
            }
            // Backspace
            else if (IsKeyJustPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace))
            {
                if (inputBuffer.Length > 0)
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            }
            else
            {
                foreach (var k in keysToCheck)
                {
                    if (IsKeyJustPressed(k) && inputBuffer.Length < 64)
                    {
                        char? ch = null;
                        if (k >= OpenTK.Windowing.GraphicsLibraryFramework.Keys.A && k <= OpenTK.Windowing.GraphicsLibraryFramework.Keys.Z)
                        {
                            char c = (char)('a' + (k - OpenTK.Windowing.GraphicsLibraryFramework.Keys.A));
                            ch = shift ? char.ToUpper(c) : c;
                        }
                        else if (k >= OpenTK.Windowing.GraphicsLibraryFramework.Keys.D0 && k <= OpenTK.Windowing.GraphicsLibraryFramework.Keys.D9)
                        {
                            ch = (char)('0' + (k - OpenTK.Windowing.GraphicsLibraryFramework.Keys.D0));
                        }
                        else
                        {
                            ch = KeyToChar(k, shift);
                        }
                        if (ch != null)
                            inputBuffer += ch;
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
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter)) prevKeysDown.Add(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter);
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace)) prevKeysDown.Add(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace);
        }
        // Returns true if the key is down this frame but was not down last frame
        private bool IsKeyJustPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
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
        }

        // Simple key-to-char mapping for symbols
        private char? KeyToChar(OpenTK.Windowing.GraphicsLibraryFramework.Keys key, bool shift)
        {
            switch (key)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space: return ' ';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Period: return '.';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Comma: return ',';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Minus: return '-';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Equal: return '=';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Slash: return '/';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backslash: return '\\';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Semicolon: return ';';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Apostrophe: return '\'';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftBracket: return '[';
                case OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightBracket: return ']';
            }
            return null;
        }

        // Helper to check if shift is down
        private bool IsShiftDown()
        {
            if (kb == null) return false;
            return kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift) ||
                   kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightShift);
        }
    }
}
