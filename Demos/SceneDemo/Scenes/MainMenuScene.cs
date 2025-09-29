using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Window.input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SceneDemo.Scenes
{
    public class MainMenuScene : SceneBase
    {
        public override void OnEnter(SceneContext context)
        {
            // Reset keyboard state so lingering key presses don't trigger immediately.
            if (context.TryGetService<Keyboard>(out var keyboard))
            {
                keyboard!.Update();
            }
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            var keyboard = context.GetRequiredService<Keyboard>();

            if (keyboard.IsKeyPressed(Keys.Enter))
            {
                context.PushScene(new GameplayScene(), new FadeTransition(duration: 0.35));
            }

            if (keyboard.IsKeyPressed(Keys.Escape))
            {
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
    }
}
