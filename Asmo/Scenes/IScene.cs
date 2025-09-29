using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Contract implemented by individual scenes/screens that participate in the scene stack.
    /// </summary>
    public interface IScene
    {
        /// <summary>
        /// Human-readable name for diagnostics and debugging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// If true, prevents scenes lower on the stack from receiving update ticks.
        /// </summary>
        bool BlocksUpdateBelow { get; }

        /// <summary>
        /// If true, stops rendering scenes lower on the stack (the scene is considered opaque).
        /// </summary>
        bool BlocksDrawBelow { get; }

        void OnEnter(SceneContext context);
        void OnExit(SceneContext context);
        void Update(SceneContext context, double deltaTime);
        void Draw(SceneContext context, Surface surface);
    }
}
