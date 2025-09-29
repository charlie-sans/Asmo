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

            var sine = AudioClip.CreateSine(440, 3.0, 0.5f);
            _handle = _audio.MasterBus.Play(sine, new AudioPlaybackSettings { Volume = 0.7f });
            _lastEvent = "Sine wave started.";
        }

        public void Update(double deltaTime)
        {
            _audio?.Update(deltaTime);
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.DarkBlue);
            surface.DrawText(20, 20, "Minimal Audio Demo", Colors.Yellow);
            surface.DrawText(20, 40, _lastEvent, Colors.White);
        }
    }
}
