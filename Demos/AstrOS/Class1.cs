using Asmo;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using Asmo.Window.input;
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Windowing.GraphicsLibraryFramework;
    public class AboutWindow
    {
        public void Draw(Surface surface, AstrOS.BasicWindow win)
        {
            int x = win.X + 24, y = win.Y + 40;
            surface.DrawText(x, y, "AstrOS", Asmo.Gfx.Colors.Yellow);
            y += 32;
            surface.DrawText(x, y, "A sample desktop environment demo.", Asmo.Gfx.Colors.White);
            y += 24;
            surface.DrawText(x, y, "(c) 2025 charlie-sans", Asmo.Gfx.Colors.Gray);
            y += 24;
            surface.DrawText(x, y, "Powered by Asmo Engine", Asmo.Gfx.Colors.Gray);
        }
    }
namespace AstrOS
{
    public class Programs
    {
        public static void Main(string[] args)
        {

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
        private AudioEngine? _audio;
        private AudioBus? _musicBus;
        private AudioBus? _sfxBus;
        private AudioHandle? _ambientHandle;
        private AudioClip? _clickClip;
    private AudioClip? _closeClip;
    private bool _prevLeftDown;
    private bool _prevRightDown;

        public Game() { }

        public void Init(Asmo.Gfx.Surface surface)
        {
            kb = new Keyboard(surface.Window);
            mouse = new Mouse(surface.Window);
            windowManager = new AstrOS.WindowManager();
            terminal = new TerminalEmulator();
            var termWindow = new AstrOS.BasicWindow(40, 40, 480, 320, "Terminal", (s, w) => terminal.Draw(s, w));
            windowManager.AddWindow(termWindow);
            InitialiseAudio();
            terminal?.ConfigureAudio(_sfxBus, _clickClip);
                   var about = new AboutWindow();
            AstrOS.AppRegistry.Register(new AstrOS.App("About", () =>
            {
                var win = new AstrOS.BasicWindow(120, 120, 400, 220, "About AstrOS", (s, w) => about.Draw(s, w));
                windowManager.AddWindow(win);
            }));
        }

        public void Update(double deltaTime)
        {
            _audio?.Update(deltaTime);

            if (kb == null || mouse == null || windowManager == null) return;
            windowManager.Update();

            // Mouse input
            int mx = mouse.X, my = mouse.Y;
            bool left = mouse.IsButtonDown(MouseButton.Left);
            bool right = mouse.IsButtonDown(MouseButton.Right);
            bool leftReleased = _prevLeftDown && !left;
            bool rightReleased = _prevRightDown && !right;
            bool rightJustPressed = !_prevRightDown && right;

            // Only open context menu on rightJustPressed, but pass current right state for rightReleased
            windowManager.HandleMouseInput(mx, my, left, rightJustPressed, leftReleased, rightReleased);

            if (!windowManager.IsContextMenuVisible)
            {
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
                            PlayClick();
                            // Close button
                            if (win.IsOnCloseButton(mx, my))
                            {
                                PlayClose();
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
            }
            else
            {
                mouseDown = false;
                if (draggingWindow != null)
                {
                    draggingWindow.IsDragging = false;
                    draggingWindow = null;
                }
            }

            _prevLeftDown = left;
            _prevRightDown = right;

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
            // Draw desktop background (solid color)
            surface.FillRect(0, 0, surface.Width, surface.Height, surface, Asmo.Gfx.Colors.DarkBlue);
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

        private void InitialiseAudio()
        {
            try
            {
                _audio = new AudioEngine();
                _musicBus = _audio.GetOrCreateBus("music");
                _musicBus.Volume = 0.25f;
                _sfxBus = _audio.GetOrCreateBus("sfx");
                _sfxBus.Volume = 0.8f;

                _clickClip ??= AudioClip.CreateSquare(1000, 0.08, 0.25f);
                _closeClip ??= AudioClip.CreateSquare(320, 0.14, 0.3f);

                var assetRoot = GameEnvironment.AssetRoot ?? AppContext.BaseDirectory;
                var path = Path.Combine(assetRoot, "Assets", "Sound", "Demo.wav");
                AudioClip ambientClip = File.Exists(path)
                    ? AudioClip.Load(path)
                    : AudioClip.CreateSine(180, 4.5, 0.2f);

                // _ambientHandle = _musicBus.Play(ambientClip, new AudioPlaybackSettings
                // {
                //     Loop = true,
                //     Volume = 0.25f,
                //     FadeInSeconds = 1.0
                // });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AstrOS audio init failed: {ex.Message}");
            }
        }

        private void PlayClick()
        {
            if (_sfxBus == null || _clickClip == null) return;

            _sfxBus.Play(_clickClip, new AudioPlaybackSettings
            {
                Volume = 0.55f,
                FadeInSeconds = 0.01
            });
        }

        private void PlayClose()
        {
            if (_sfxBus == null || _closeClip == null) return;

            _sfxBus.Play(_closeClip, new AudioPlaybackSettings
            {
                Volume = 0.6f,
                FadeInSeconds = 0.02
            });
        }
    }

    // TerminalEmulator: logic from original Game, now as a helper class
    public class TerminalEmulator
    {
        private readonly List<string> outputLines = new();
        private string inputBuffer = "";
        private int maxLines = 20;
        private bool showCursor = true;
        private double cursorBlink = 0;
        private readonly HashSet<Keys> prevKeysDown = new();
        private AudioBus? _audioBus;
        private AudioClip? _keyClip;

        public TerminalEmulator()
        {
            outputLines.Add("Simple GUI Terminal Emulator. Type 'help' for commands. Type 'exit' to quit.");
        }

        public void ConfigureAudio(AudioBus? bus, AudioClip? keyClip)
        {
            _audioBus = bus;
            _keyClip = keyClip;
        }

        public void Update(Keyboard kb, double deltaTime)
        {
            bool shift = IsShiftDown(kb);
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
            if (IsKeyJustPressed(kb, Keys.Enter))
            {
                ProcessCommand(inputBuffer);
                inputBuffer = "";
                PlayKeySound();
            }
            // Backspace
            else if (IsKeyJustPressed(kb, Keys.Backspace))
            {
                if (inputBuffer.Length > 0)
                    inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
                PlayKeySound();
            }
            else
            {
                foreach (var k in keysToCheck)
                {
                    if (IsKeyJustPressed(kb, k) && inputBuffer.Length < 64)
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
            PlayKeySound();
        }

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

        private bool IsShiftDown(Keyboard kb)
        {
            return kb.IsKeyDown(Keys.LeftShift) ||
                   kb.IsKeyDown(Keys.RightShift);
        }

        private bool IsKeyJustPressed(Keyboard kb, Keys key)
        {
            return kb.IsKeyDown(key) && !prevKeysDown.Contains(key);
        }

        private void PlayKeySound()
        {
            if (_audioBus == null || _keyClip == null)
                return;

            _audioBus.Play(_keyClip, new AudioPlaybackSettings
            {
                Volume = 0.4f,
                FadeInSeconds = 0.005
            });
        }
    }
}
