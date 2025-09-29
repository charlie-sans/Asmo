using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Window.input;

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
            Services.Register(new System.Random());
            _audioLibrary = new SceneAudioLibrary();
            Services.Register(_audioLibrary);

            var audio = Services.GetRequired<AudioEngine>();
            _musicBus = audio.GetOrCreateBus("music");
            _musicBus.Volume = 0.35f;
            _sfxBus = audio.GetOrCreateBus("sfx");
            _sfxBus.Volume = 0.75f;
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

            sceneManager.PushScene(new Scenes.SplashScene(), new FadeTransition(duration: 0.4));
        }

        internal AudioBus SfxBus => _sfxBus ??= Services.GetRequired<AudioEngine>().GetOrCreateBus("sfx");
        internal SceneAudioLibrary AudioLibrary => _audioLibrary;
        internal AudioHandle? AmbientHandle => _ambientHandle;
    }
}
