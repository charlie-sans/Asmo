using Asmo.Gfx;

namespace Asmo.Gfx
{
    public interface IConsoleGame
    {
        void Init(Surface surface);
        void Update(double deltaTime);
        void Draw(Surface surface);
    }
}
