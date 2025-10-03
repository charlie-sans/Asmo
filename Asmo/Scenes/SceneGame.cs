using System;
using Asmo.Gfx;
using Asmo.Window.input;

namespace Asmo.Scenes
{
    /// <summary>
    /// Helper base class for <see cref="IConsoleGame"/> implementations that leverage the scene manager.
    /// </summary>
    public abstract class SceneGame : IConsoleGame
    {
    protected SceneManager SceneManager { get; private set; } = null!;
    protected Surface Surface { get; private set; } = null!;
    protected SceneServices Services => SceneManager.Services;
    // AudioEngine removed in aggressive cleanup; reintroduce later.

        public virtual void Init(Surface surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));

            Surface = surface;
            SceneManager = new SceneManager(surface);
            RegisterCoreServices(surface);
            ConfigureGame(SceneManager, Services);
        }

        /// <summary>
        /// Allows derived games to register additional services (input devices, audio, etc.).
        /// </summary>
        protected virtual void RegisterCoreServices(Surface surface)
        {
            // Register Raylib keyboard shim
            Services.Register(new Keyboard());

            Services.Register(surface);
            // Audio removed for now.
        }

        /// <summary>
        /// Called once after initialization to set up the initial scene stack.
        /// </summary>
        protected abstract void ConfigureGame(SceneManager sceneManager, SceneServices services);

        public virtual void Update(double deltaTime)
        {
            SceneManager.Update(deltaTime);
            if (Services.TryGet<Keyboard>(out var keyboard)) keyboard!.Update();
        }

        public virtual void Draw(Surface surface)
        {
            SceneManager.Draw(surface);
        }
    }
}
