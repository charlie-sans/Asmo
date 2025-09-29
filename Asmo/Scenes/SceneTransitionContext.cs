using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Context object passed to scene transitions during their lifecycle.
    /// </summary>
    public readonly struct SceneTransitionContext
    {
        internal SceneTransitionContext(SceneManager manager, Surface surface, double deltaTime, double elapsed, IScene? fromScene, IScene? toScene)
        {
            Manager = manager;
            Surface = surface;
            DeltaTime = deltaTime;
            Elapsed = elapsed;
            FromScene = fromScene;
            ToScene = toScene;
        }

        public SceneManager Manager { get; }
        public SceneServices Services => Manager.Services;
        public Surface Surface { get; }
        public double DeltaTime { get; }
        public double Elapsed { get; }
        public IScene? FromScene { get; }
        public IScene? ToScene { get; }
    }
}
