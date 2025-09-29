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
        private int playerX = 40, playerY = 40;
        private int playerW = 12, playerH = 12;
        private readonly Asmo.Types.Color playerColor = Colors.Cyan;

        public void Init(Surface surface)
        {
            // Initialize input, state, etc.
            kb = new Keyboard(surface.Window);
        }

        public void Update(double deltaTime)
        {
            if (kb == null) return;
            // Example: Move player with arrow keys
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left)) playerX -= 2;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right)) playerX += 2;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up)) playerY -= 2;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down)) playerY += 2;
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);
            surface.DrawText(10, 10, "Template Game", Colors.Yellow);
            surface.DrawRect(playerX, playerY, playerW, playerH, playerColor);
            surface.DrawText(10, 30, "Use arrow keys to move the square.", Colors.White);
        }
    }
}
