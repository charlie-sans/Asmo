using Asmo.Gfx;

namespace Asmo.Window
{
    public class ConsoleHost
    {
        public IConsoleGame? Game { get; private set; }

        public void LoadGame(IConsoleGame game, Surface surface)
        {
            Game = game;
            Game.Init(surface);
        }

        public void Update(double deltaTime)
        {
            Game?.Update(deltaTime);
        }

        public void Draw(Surface surface)
        {
            Game?.Draw(surface);
        }
    }
}
