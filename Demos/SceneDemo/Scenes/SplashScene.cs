using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;

namespace SceneDemo.Scenes
{
    public class SplashScene : SceneBase
    {
        private double _timer;
        private const double Duration = 2.0;

        public override void OnEnter(SceneContext context)
        {
            _timer = 0;
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            _timer += deltaTime;
            if (_timer >= Duration)
            {
                context.ReplaceScene(new MainMenuScene(), new FadeTransition(duration: 0.5));
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            surface.Clear(Colors.DarkBlue);
            surface.DrawText(surface.Width / 2 - 60, surface.Height / 2 - 20, "ASMO SCENE DEMO", Colors.Yellow);
            surface.DrawText(surface.Width / 2 - 56, surface.Height / 2 + 5, "Now with scene stacks!", Colors.Cyan);
        }
    }
}
