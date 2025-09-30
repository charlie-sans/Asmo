using Asmo;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using System;
using System.IO;
using System.Text;

namespace AudioDemo
{
    /// <summary>
    /// Minimal audio demo: plays a sine wave on startup and logs all actions.
    /// </summary>
    public sealed class MinimalAudioDemoGame : IConsoleGame
    {
        private AudioEngine? _audio;
        private AudioHandle? _handle;
        private string _lastEvent = "Press Space to play SFX.";
        private DebugOverlay _debugOverlay = new DebugOverlay();
        private Asmo.Window.input.Keyboard? _keyboard;

        public void Init(Surface surface)
        {
            _audio = new AudioEngine();
            AudioEngine.DiagnosticsEnabled = true;
            var logPath = Path.Combine(AppContext.BaseDirectory, "minimal-audio.log");
            var logEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AudioEngine.DiagnosticsSink = msg =>
            {
                Console.WriteLine(msg);
                File.AppendAllText(logPath, msg + Environment.NewLine, logEncoding);
            };

            // Try to get keyboard from surface.Window if available
            if (surface.Window != null)
                _keyboard = new Asmo.Window.input.Keyboard(surface.Window);
            if (_keyboard != null)
                _debugOverlay.SetKeyboard(_keyboard);

            _lastEvent = "Sine wave started.";
        }

        public void Update(double deltaTime)
        {
            _debugOverlay.BeginFrame();
            _audio?.Update(deltaTime);
            _keyboard?.Update();
            if (_keyboard != null && _keyboard.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.F3))
            {
                _debugOverlay.Toggle();
            }
            _debugOverlay.EndFrame();
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.DarkBlue);
            surface.DrawText(20, 20, "Minimal Audio Demo", Colors.Yellow);
            surface.DrawText(20, 40, _lastEvent, Colors.White);
            _debugOverlay.Render(surface);
        }
    }
}
