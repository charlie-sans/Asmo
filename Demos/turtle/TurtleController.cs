using System;
using Asmo.Gfx;
using Asmo.Types;

namespace TemplateGame
{
    [Lua.LuaObject]
    public partial class TurtleController
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public double Angle { get; private set; } // In degrees
        public bool PenDown { get; private set; } = true;
        public Color PenColor { get; set; } = Colors.DarkBlue;
        public int PenWidth { get; set; } = 2;
        private Surface? _surface;
        public TurtleController(int startX, int startY)
        {
            X = startX;
            Y = startY;
            Angle = 0;
        }
        public void AttachSurface(Surface surface) => _surface = surface;
        public void Forward(int distance)
        {
            int newX = X + (int)(distance * Math.Cos(Angle * Math.PI / 180));
            int newY = Y + (int)(distance * Math.Sin(Angle * Math.PI / 180));
            if (PenDown && _surface != null)
                _surface.DrawLine(X, Y, newX, newY, PenColor);
            X = newX;
            Y = newY;
        }
        public void Backward(int distance) => Forward(-distance);
        public void Left(double degrees) => Angle = (Angle - degrees) % 360;
        public void Right(double degrees) => Angle = (Angle + degrees) % 360;
        public void PenUp() => PenDown = false;
        public void PenDownFunc() => PenDown = true;
        public void Goto(int x, int y)
        {
            if (PenDown && _surface != null)
                _surface.DrawLine(X, Y, x, y, PenColor);
            X = x;
            Y = y;
        }
        public void SetAngle(double angle) => Angle = angle % 360;
        public void SetColor(Color color) => PenColor = color;
    }
}
