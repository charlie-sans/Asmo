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

namespace AstrOS
{
    public class Programs
    {
        public static void Main(string[] args)
        {
            AsmoHost.LaunchStandalone(() => new AstrOS.Game());
        }
    }
    public class Game : Asmo.Gfx.IConsoleGame
    {
        private Keyboard? kb = null;
        private Mouse? mouse = null;
        private AstrOS.WindowManager? windowManager = null;
        private TerminalEmulator? terminal = null;
        private bool mouseDown = false;
        private int mouseDownX, mouseDownY;
        private AstrOS.BasicWindow? draggingWindow = null;

        public Game() { }

        public void Init(Asmo.Gfx.Surface surface)
        {
            kb = new Keyboard(surface.Window);
            mouse = new Mouse(surface.Window);
            windowManager = new AstrOS.WindowManager();
            terminal = new TerminalEmulator();
            var termWindow = new AstrOS.BasicWindow(40, 40, 480, 320, "Terminal", (s, w) => terminal.Draw(s, w));
            windowManager.AddWindow(termWindow);
        }

        public void Update(double deltaTime)
        {
            if (kb == null || mouse == null || windowManager == null) return;
            windowManager.Update();

            // Mouse input
            int mx = mouse.X, my = mouse.Y;
            bool left = mouse.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left);

            if (left && !mouseDown)
            {
                mouseDown = true;
                mouseDownX = mx; mouseDownY = my;
                // Check for window interaction (from topmost)
                for (int i = windowManager.Windows.Count - 1; i >= 0; i--)
                {
                    var win = windowManager.Windows[i];
                    if (win.IsPointInside(mx, my))
                    {
                        windowManager.BringToFront(win);
                        win.IsFocused = true;
                        // Close button
                        if (win.IsOnCloseButton(mx, my))
                        {
                            windowManager.CloseWindow(win);
                            break;
                        }
                        // Title bar drag
                        else if (win.IsOnTitleBar(mx, my))
                        {
                            draggingWindow = win;
                            win.DragOffsetX = mx - win.X;
                            win.DragOffsetY = my - win.Y;
                            win.IsDragging = true;
                            break;
                        }
                    }
                }
            }
            else if (!left && mouseDown)
            {
                mouseDown = false;
                if (draggingWindow != null)
                {
                    draggingWindow.IsDragging = false;
                    draggingWindow = null;
                }
            }
            else if (left && draggingWindow != null)
            {
                // Drag window
                draggingWindow.X = mx - draggingWindow.DragOffsetX;
                draggingWindow.Y = my - draggingWindow.DragOffsetY;
            }

            // Only one window focused at a time
            for (int i = 0; i < windowManager.Windows.Count; i++)
                windowManager.Windows[i].IsFocused = (windowManager.Windows[i] == windowManager.FocusedWindow);

            // Forward input to focused window (terminal)
            if (windowManager.FocusedWindow != null && windowManager.FocusedWindow.DrawContent != null)
            {
                if (windowManager.FocusedWindow.Title == "Terminal" && terminal != null)
                {
                    terminal.Update(kb, deltaTime);
                }
            }
        }

        public void Draw(Asmo.Gfx.Surface surface)
        {
            surface.Clear(Asmo.Gfx.Colors.Black);
            windowManager?.DrawAll(surface);

            // Draw mouse cursor (simple cross)
            if (mouse != null)
            {
                int mx = mouse.X, my = mouse.Y;
                // Draw a white cross
                for (int dx = -4; dx <= 4; dx++)
                    surface.SetPixel(mx + dx, my, Asmo.Gfx.Colors.White);
                for (int dy = -4; dy <= 4; dy++)
                    surface.SetPixel(mx, my + dy, Asmo.Gfx.Colors.White);
            }
        }
    }

    // TerminalEmulator: logic from original Game, now as a helper class
    public class TerminalEmulator
    {
        private System.Collections.Generic.List<string> outputLines = new();
        private string inputBuffer = "";
        private int maxLines = 20;
        private bool showCursor = true;
        private double cursorBlink = 0;
        private System.Collections.Generic.HashSet<OpenTK.Windowing.GraphicsLibraryFramework.Keys> prevKeysDown = new();

        public TerminalEmulator()
        {
            outputLines.Add("Simple GUI Terminal Emulator. Type 'help' for commands. Type 'exit' to quit.");
        }

        public void Update(Keyboard kb, double deltaTime)
        {
            bool shift = IsShiftDown(kb);
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
            if (IsKeyJustPressed(kb, OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
            {
                ProcessCommand(inputBuffer);
                inputBuffer = "";
            }
            // Backspace
            else if (IsKeyJustPressed(kb, OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace))
            {
                if (inputBuffer.Length > 0)
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            }
            else
            {
                foreach (var k in keysToCheck)
                {
                    if (IsKeyJustPressed(kb, k) && inputBuffer.Length < 64)
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

        public void Draw(Asmo.Gfx.Surface surface, AstrOS.BasicWindow win)
        {
            int x = win.X + 8, y = win.Y + 28, lineH = 20;
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

        private bool IsShiftDown(Keyboard kb)
        {
            return kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftShift) ||
                   kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightShift);
        }

        private bool IsKeyJustPressed(Keyboard kb, OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
        {
            return kb.IsKeyDown(key) && !prevKeysDown.Contains(key);
        }
    }
}
