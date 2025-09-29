using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Types;
using Asmo.Window.input;
using OpenTK.Windowing.GraphicsLibraryFramework;

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

        public override void OnEnter(SceneContext context)
        {
            _score = 0;
            _elapsed = 0;
            _playerX = context.Surface.Width / 2;
            _playerY = context.Surface.Height / 2;

            var random = context.GetRequiredService<System.Random>();
            _velocityX = random.Next(2) == 0 ? -1 : 1;
            _velocityY = random.Next(2) == 0 ? -1 : 1;
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            _elapsed += deltaTime;
            var keyboard = context.GetRequiredService<Keyboard>();

            if (keyboard.IsKeyPressed(Keys.P))
            {
                context.PushScene(new PauseScene(), new FadeTransition(fadeColor: Colors.DarkGray, duration: 0.25, bidirectional: false));
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Backspace))
            {
                context.ReplaceScene(new MainMenuScene(), new FadeTransition(duration: 0.35));
                return;
            }

            if (keyboard.IsKeyDown(Keys.Left)) _playerX -= 2;
            if (keyboard.IsKeyDown(Keys.Right)) _playerX += 2;
            if (keyboard.IsKeyDown(Keys.Up)) _playerY -= 2;
            if (keyboard.IsKeyDown(Keys.Down)) _playerY += 2;

            BounceWithinBounds(context.Surface);
            _score = (int)(_elapsed * 10);
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

        private void BounceWithinBounds(Surface surface)
        {
            var minX = 10;
            var minY = 60;
            var maxX = surface.Width - 10;
            var maxY = surface.Height - 10;

            _playerX = Math.Clamp(_playerX, minX, maxX);
            _playerY = Math.Clamp(_playerY, minY, maxY);

            if (_playerX == minX || _playerX == maxX)
            {
                _velocityX = -_velocityX;
            }

            if (_playerY == minY || _playerY == maxY)
            {
                _velocityY = -_velocityY;
            }

            _playerX += _velocityX;
            _playerY += _velocityY;
        }
    }
}
