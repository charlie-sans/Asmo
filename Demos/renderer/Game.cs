using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
using Demos.Renderer;
namespace TemplateGame
{
    // Demo launcher: press Tab to switch between demos
    public class Game : IConsoleGame
    {
        private IConsoleGame[] demos = new IConsoleGame[]
        {
            new Walk3DGame(),
            // Add more demos here
        };
        private int current = 0;
        private Keyboard kb;
        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
            foreach (var demo in demos) demo.Init(surface);
        }
        public void Update(double deltaTime)
        {
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Tab))
            {
                current = (current + 1) % demos.Length;
            }
            demos[current].Update(deltaTime);
        }
        public void Draw(Surface surface)
        {
            demos[current].Draw(surface);
            surface.DrawText(10, surface.Height - 20, $"Tab: Switch Demo ({current+1}/{demos.Length})", Colors.Gray);
        }
    }
}
