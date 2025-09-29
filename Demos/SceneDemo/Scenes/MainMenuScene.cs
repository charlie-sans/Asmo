using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Window.input;
using SceneDemo;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SceneDemo.Scenes
{
    public class MainMenuScene : SceneBase
    {
        private AudioBus? _sfxBus;
        private SceneAudioLibrary? _audioLibrary;

        public override void OnEnter(SceneContext context)
        {
            // Reset keyboard state so lingering key presses don't trigger immediately.
            if (context.TryGetService<Keyboard>(out var keyboard))
            {
                keyboard!.Update();
            }

            if (context.TryGetService<AudioEngine>(out var audio))
            {
                _sfxBus = audio!.GetOrCreateBus("sfx");
                SceneDiagnostics.Log("MainMenuScene acquired SFX bus.");
            }
            else
            {
                SceneDiagnostics.Log("MainMenuScene failed to resolve AudioEngine service.");
            }

            context.TryGetService<SceneAudioLibrary>(out _audioLibrary);
            if (_audioLibrary != null)
            {
                SceneDiagnostics.Log("MainMenuScene received SceneAudioLibrary.");
            }
            else
            {
                SceneDiagnostics.Log("MainMenuScene missing SceneAudioLibrary service.");
            }
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            var keyboard = context.GetRequiredService<Keyboard>();

            if (keyboard.IsKeyPressed(Keys.Enter))
            {
                PlayMenuSound(_audioLibrary?.MenuForward);
                context.PushScene(new GameplayScene(), new FadeTransition(duration: 0.35));
            }

            if (keyboard.IsKeyPressed(Keys.Escape))
            {
                PlayMenuSound(_audioLibrary?.MenuBack);
                context.ClearScenes(new FadeTransition(duration: 0.3));
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            surface.Clear(Colors.DarkMagenta);
            surface.DrawText(surface.Width / 2 - 40, 40, "SCENE DEMO", Colors.White);
            surface.DrawText(40, 90, "Press Enter to start gameplay", Colors.Yellow);
            surface.DrawText(40, 110, "Press Escape to quit back to launcher", Colors.Cyan);
            surface.DrawText(40, 140, "During gameplay, press P to pause", Colors.White);
        }

        private void PlayMenuSound(AudioClip? clip)
        {
            if (clip == null || _sfxBus == null)
            {
                SceneDiagnostics.Log("MainMenuScene cannot play menu sound (missing clip or bus).");
                return;
            }

            _sfxBus.Play(clip, new AudioPlaybackSettings
            {
                Volume = 0.7f,
                FadeInSeconds = 0.02
            });
            SceneDiagnostics.Log("MainMenuScene played menu sound.");
        }
    }
}
