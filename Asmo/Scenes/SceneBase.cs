using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Convenience base class for scenes with sensible defaults.
    /// </summary>
    public abstract class SceneBase : IScene
    {
        public virtual string Name => GetType().Name;

        /// <inheritdoc />
        public virtual bool BlocksUpdateBelow => true;

        /// <inheritdoc />
        public virtual bool BlocksDrawBelow => true;

        public virtual void OnEnter(SceneContext context) { }
        public virtual void OnExit(SceneContext context) { }
        public virtual void Update(SceneContext context, double deltaTime) { }
        public virtual void Draw(SceneContext context, Surface surface) { }
    }
}
