using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Window.input;
using System;
using System.IO;
using System.Text;

namespace SceneDemo
{
    public class SceneDemoGame : SceneGame
    {
        private SceneAudioLibrary _audioLibrary = null!;
        private AudioBus? _musicBus;
        private AudioBus? _sfxBus;
        private AudioHandle? _ambientHandle;

        protected override void RegisterCoreServices(Surface surface)
        {
            base.RegisterCoreServices(surface);
            AudioEngine.DiagnosticsEnabled = true;
            var logPath = Path.Combine(AppContext.BaseDirectory, "scene-demo-audio.log");
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AudioEngine.DiagnosticsSink = msg =>
            {
                Console.WriteLine(msg);
                File.AppendAllText(logPath, msg + Environment.NewLine, encoding);
            };
            SceneDiagnostics.Log("Audio diagnostics configured for SceneDemoGame.");
            Services.Register(new System.Random());
            _audioLibrary = new SceneAudioLibrary();
            Services.Register(_audioLibrary);
            SceneDiagnostics.Log("SceneAudioLibrary registered as service.");

            var audio = Services.GetRequired<AudioEngine>();
            _musicBus = audio.GetOrCreateBus("music");
            _musicBus.Volume = 0.35f;
            SceneDiagnostics.Log($"Music bus ready (volume={_musicBus.Volume:0.00}).");
            _sfxBus = audio.GetOrCreateBus("sfx");
            _sfxBus.Volume = 0.75f;
            SceneDiagnostics.Log($"SFX bus ready (volume={_sfxBus.Volume:0.00}).");
        }

        protected override void ConfigureGame(SceneManager sceneManager, SceneServices services)
        {
            var audio = services.GetRequired<AudioEngine>();
            if (_musicBus == null)
            {
                _musicBus = audio.GetOrCreateBus("music");
            }

            _ambientHandle = _musicBus.Play(_audioLibrary.AmbientLoop, new AudioPlaybackSettings
            {
                Loop = true,
                Volume = 0.4f,
                FadeInSeconds = 1.2
            });
            SceneDiagnostics.Log("Ambient loop started during game configuration.");

            sceneManager.PushScene(new Scenes.SplashScene(), new FadeTransition(duration: 0.4));
            SceneDiagnostics.Log("SplashScene pushed onto scene stack.");
        }

        internal AudioBus SfxBus => _sfxBus ??= Services.GetRequired<AudioEngine>().GetOrCreateBus("sfx");
        internal SceneAudioLibrary AudioLibrary => _audioLibrary;
        internal AudioHandle? AmbientHandle => _ambientHandle;
    }
}
