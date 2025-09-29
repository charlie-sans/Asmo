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
    public class GameplayScene : SceneBase
    {
        private int _playerX;
        private int _playerY;
        private int _velocityX = 1;
        private int _velocityY = 1;
        private int _score;
        private double _elapsed;
        private int _milestone = 1;

        private AudioBus? _sfxBus;
        private SceneAudioLibrary? _audioLibrary;
        private double _bounceCooldown;

        public override void OnEnter(SceneContext context)
        {
            _score = 0;
            _elapsed = 0;
            _milestone = 1;
            _playerX = context.Surface.Width / 2;
            _playerY = context.Surface.Height / 2;

            var random = context.GetRequiredService<System.Random>();
            _velocityX = random.Next(2) == 0 ? -1 : 1;
            _velocityY = random.Next(2) == 0 ? -1 : 1;

            if (context.TryGetService<AudioEngine>(out var audio))
            {
                _sfxBus = audio!.GetOrCreateBus("sfx");
                SceneDiagnostics.Log("GameplayScene acquired SFX bus.");
            }
            else
            {
                SceneDiagnostics.Log("GameplayScene failed to resolve AudioEngine service.");
            }

            context.TryGetService<SceneAudioLibrary>(out _audioLibrary);
            if (_audioLibrary != null)
            {
                SceneDiagnostics.Log("GameplayScene received SceneAudioLibrary.");
            }
            else
            {
                SceneDiagnostics.Log("GameplayScene missing SceneAudioLibrary service.");
            }
            _bounceCooldown = 0;
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            _elapsed += deltaTime;
            if (_bounceCooldown > 0)
            {
                _bounceCooldown = Math.Max(0, _bounceCooldown - deltaTime);
            }

            var keyboard = context.GetRequiredService<Keyboard>();

            if (keyboard.IsKeyPressed(Keys.P))
            {
                PlaySfx(_audioLibrary?.PauseToggle, volume: 0.25f);
                context.PushScene(new PauseScene(), new FadeTransition(fadeColor: Colors.DarkGray, duration: 0.25, bidirectional: false));
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Backspace))
            {
                PlaySfx(_audioLibrary?.MenuBack, volume: 0.4f);
                context.ReplaceScene(new MainMenuScene(), new FadeTransition(duration: 0.35));
                return;
            }

            if (keyboard.IsKeyDown(Keys.Left)) _playerX -= 2;
            if (keyboard.IsKeyDown(Keys.Right)) _playerX += 2;
            if (keyboard.IsKeyDown(Keys.Up)) _playerY -= 2;
            if (keyboard.IsKeyDown(Keys.Down)) _playerY += 2;

            bool bounced = BounceWithinBounds(context.Surface);
            if (bounced)
            {
                PlaySfx(_audioLibrary?.Bounce, volume: 0.3f, minInterval: 0.08);
            }

            _score = (int)(_elapsed * 10);
            if (_audioLibrary != null && _score >= 100 * _milestone)
            {
                PlaySfx(_audioLibrary.Collect, volume: 0.45f);
                _milestone++;
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            surface.Clear(Colors.DarkGreen);
            surface.DrawText(10, 10, "Gameplay Scene", Colors.White);
            surface.DrawText(10, 25, $"Score: {_score}", Colors.Yellow);
            surface.DrawText(10, 40, "Use arrow keys to move. P to pause. Backspace for menu.", Colors.White);

            var playerColor = new Color(255, 180, 80, 255);
            surface.DrawFilledCircle(_playerX, _playerY, 8, playerColor);
        }

        private bool BounceWithinBounds(Surface surface)
        {
            var minX = 10;
            var minY = 60;
            var maxX = surface.Width - 10;
            var maxY = surface.Height - 10;
            bool bounced = false;

            _playerX = Math.Clamp(_playerX, minX, maxX);
            _playerY = Math.Clamp(_playerY, minY, maxY);

            if (_playerX == minX || _playerX == maxX)
            {
                _velocityX = -_velocityX;
                bounced = true;
            }

            if (_playerY == minY || _playerY == maxY)
            {
                _velocityY = -_velocityY;
                bounced = true;
            }

            _playerX += _velocityX;
            _playerY += _velocityY;

            return bounced;
        }

        private void PlaySfx(AudioClip? clip, float volume = 0.5f, double minInterval = 0.0)
        {
            if (clip == null || _sfxBus == null)
            {
                SceneDiagnostics.Log("GameplayScene cannot play SFX (missing clip or bus).");
                return;
            }

            if (minInterval > 0 && _bounceCooldown > 0)
            {
                SceneDiagnostics.Log("GameplayScene skipped SFX due to cooldown.");
                return;
            }

            _sfxBus.Play(clip, new AudioPlaybackSettings
            {
                Volume = volume,
                FadeInSeconds = 0.005
            });
            SceneDiagnostics.Log($"GameplayScene played SFX (volume={volume:0.00}).");

            if (minInterval > 0)
            {
                _bounceCooldown = minInterval;
            }
        }
    }
}
