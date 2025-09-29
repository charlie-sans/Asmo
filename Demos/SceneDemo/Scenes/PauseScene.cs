using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Types;
using Asmo.Window.input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SceneDemo;

namespace SceneDemo.Scenes
{
    public class PauseScene : SceneBase
    {
        public override bool BlocksDrawBelow => false;
        private AudioBus? _sfxBus;
        private SceneAudioLibrary? _audioLibrary;

        public override void OnEnter(SceneContext context)
        {
            if (context.TryGetService<Keyboard>(out var keyboard))
            {
                keyboard!.Update();
            }

            if (context.TryGetService<AudioEngine>(out var audio))
            {
                _sfxBus = audio!.GetOrCreateBus("sfx");
            }

            context.TryGetService<SceneAudioLibrary>(out _audioLibrary);
            PlayToggleSound();
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            var keyboard = context.GetRequiredService<Keyboard>();

            if (keyboard.IsKeyPressed(Keys.P) || keyboard.IsKeyPressed(Keys.Escape))
            {
                PlayToggleSound();
                context.PopScene(new FadeTransition(duration: 0.2, bidirectional: false));
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            // Draw translucent overlay
            for (int x = 0; x < surface.Width; x++)
            {
                var column = surface.Pixels[x];
                for (int y = 0; y < surface.Height; y++)
                {
                    var src = column[y];
                    column[y] = new Color((src.R + 40) / 2, (src.G + 40) / 2, (src.B + 40) / 2, src.A);
                }
            }

            surface.DrawOutlinedRect(20, 40, surface.Width - 40, surface.Height - 80, Colors.White);
            surface.DrawText(40, 80, "PAUSED", Colors.Yellow);
            surface.DrawText(40, 100, "Press P or Escape to resume", Colors.White);
        }

        private void PlayToggleSound()
        {
            if (_sfxBus == null || _audioLibrary == null)
                return;

            _sfxBus.Play(_audioLibrary.PauseToggle, new AudioPlaybackSettings
            {
                Volume = 0.35f,
                FadeInSeconds = 0.01
            });
        }
    }
}
