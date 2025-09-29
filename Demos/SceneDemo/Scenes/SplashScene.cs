using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using SceneDemo;

namespace SceneDemo.Scenes
{
    public class SplashScene : SceneBase
    {
        private double _timer;
        private const double Duration = 2.0;
        private AudioBus? _sfxBus;
        private SceneAudioLibrary? _audioLibrary;

        public override void OnEnter(SceneContext context)
        {
            _timer = 0;
            if (context.TryGetService<AudioEngine>(out var audio))
            {
                _sfxBus = audio!.GetOrCreateBus("sfx");
            }

            context.TryGetService<SceneAudioLibrary>(out _audioLibrary);
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= Duration)
            {
                PlayForwardSound();
                context.ReplaceScene(new MainMenuScene(), new FadeTransition(duration: 0.5));
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            surface.Clear(Colors.DarkBlue);
            surface.DrawText(surface.Width / 2 - 60, surface.Height / 2 - 20, "ASMO SCENE DEMO", Colors.Yellow);
            surface.DrawText(surface.Width / 2 - 56, surface.Height / 2 + 5, "Now with scene stacks!", Colors.Cyan);
        }

        private void PlayForwardSound()
        {
            if (_sfxBus == null || _audioLibrary == null)
                return;

            _sfxBus.Play(_audioLibrary.MenuForward, new AudioPlaybackSettings
            {
                Volume = 0.6f,
                FadeInSeconds = 0.02
            });
        }
    }
}
