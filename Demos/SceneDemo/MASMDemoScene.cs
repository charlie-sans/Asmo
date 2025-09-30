using System;
using Asmo.Gfx;
using Asmo.Scenes;

namespace Asmo.Scenes
{
    /// <summary>
    /// Demo scene showcasing MASM script execution for rendering and input.
    /// </summary>
    public class MASMDemoScene : SceneBase
    {
        private MASMHost _masmHost;

        public override void OnEnter(SceneContext context)
        {
            _masmHost = new MASMHost(context.Surface);
            // Assume a demo script exists
            _masmHost.Load(Path.Join(GameEnvironment.AssetRoot, "demo.masm"));
            _masmHost.Start();
        }

        public override void Update(SceneContext context, double deltaTime)
        {
            // Execute MASM script steps
            while (_masmHost.IsRunning && _masmHost.Step())
            {
                // Continue executing
            }
        }

        public override void Draw(SceneContext context, Surface surface)
        {
            // Sync memory back to surface after execution
            _masmHost.SyncMemoryToSurface();
        }

        public override void OnExit(SceneContext context)
        {
            _masmHost.Shutdown();
        }
    }
}