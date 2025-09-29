using Asmo.Gfx;
using Asmo.Scenes;
using Asmo.Scenes.Transitions;
using Asmo.Window.input;

namespace SceneDemo
{
    public class SceneDemoGame : SceneGame
    {
        protected override void RegisterCoreServices(Surface surface)
        {
            base.RegisterCoreServices(surface);
            Services.Register(new System.Random());
        }

        protected override void ConfigureGame(SceneManager sceneManager, SceneServices services)
        {
            sceneManager.PushScene(new Scenes.SplashScene(), new FadeTransition(duration: 0.4));
        }
    }
}
