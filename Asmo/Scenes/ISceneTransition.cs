using Asmo.Gfx;

namespace Asmo.Scenes
{
    /// <summary>
    /// Represents a visual transition that plays while scenes are pushed, popped, or replaced.
    /// </summary>
    public interface ISceneTransition
    {
        void Begin(SceneTransitionContext context);
        bool Update(SceneTransitionContext context);
        void Draw(SceneTransitionContext context, Surface surface);
        void Complete(SceneTransitionContext context);
    }
}
