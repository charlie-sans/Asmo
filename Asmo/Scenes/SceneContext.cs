using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Runtime context provided to scenes during lifecycle callbacks, exposing the scene manager,
    /// shared services, and helper methods for stack manipulation.
    /// </summary>
    public readonly struct SceneContext
    {
        private readonly SceneManager _manager;
        private readonly Surface _surface;
        private readonly double _deltaTime;

        internal SceneContext(SceneManager manager, Surface surface, double deltaTime)
        {
            _manager = manager;
            _surface = surface;
            _deltaTime = deltaTime;
        }

        public SceneManager Manager => _manager;
        public SceneServices Services => _manager.Services;
        public Surface Surface => _surface;
        public double DeltaTime => _deltaTime;
        public double TotalTime => _manager.TotalTime;

        public void PushScene(IScene scene, ISceneTransition? transition = null) => _manager.QueuePush(scene, transition);
        public void PopScene(ISceneTransition? transition = null) => _manager.QueuePop(transition);
        public void ReplaceScene(IScene scene, ISceneTransition? transition = null) => _manager.QueueReplace(scene, transition);
        public void ClearScenes(ISceneTransition? transition = null) => _manager.QueueClear(transition);

        public TService GetRequiredService<TService>() where TService : class => Services.GetRequired<TService>();
        public bool TryGetService<TService>(out TService? service) where TService : class => Services.TryGet(out service);
    }
}
