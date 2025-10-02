using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
namespace TemplateGame
{
    // Template for a minimal Asmo demo/game
    public class Game : IConsoleGame
    {
    private Keyboard? kb;
    private MASMHost? masmHost;
    private bool masmLoaded = false;
    private string masmStatus = "";

        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
            // Load MASM script
            masmHost = new MASMHost(surface);
            masmLoaded = masmHost.Load("Demos/masmTests/demo.masm");
            if (masmLoaded)
            {
                masmHost.Run();
                masmStatus = "MASM loaded and started.";
            }
            else
            {
                masmStatus = "MASM load failed.";
            }
        }

        public void Update(double deltaTime)
        {
            if (kb == null) return;
            // Step MASM script if loaded and running
            if (masmLoaded && masmHost != null && masmHost.IsRunning)
            {
                // bool cont = masmHost.Step();
                // if (!cont)
                // {
                //     masmStatus = "MASM halted.";
                // }
            }
        }

        public void Draw(Surface surface)
        {
            // surface.Clear(Colors.Black);
            // If MASM is loaded, sync framebuffer to surface
            if (masmLoaded && masmHost != null)
            {
                masmHost.SyncMemoryToSurface();
                surface.DrawText(10, 10, "MASM Demo: Cyan Box", Colors.Yellow);
                surface.DrawText(10, 30, masmStatus, Colors.White);
            }
            else
            {
                surface.DrawText(10, 10, "MASM Demo: Load failed", Colors.Red);
            }
        }
    }
}
