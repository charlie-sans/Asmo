using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
using Lua;

namespace TemplateGame
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }
    public class Game : IConsoleGame
    {
        private TurtleController turtle;
        private Surface _surface;
        private LuaState state;
        private bool scriptRan = false;

        public void Init(Surface surface)
        {
            _surface = surface;
            int startX = surface.Width / 2;
            int startY = surface.Height / 2;
            turtle = new TurtleController(startX, startY);
            turtle.AttachSurface(surface);

            // Initialize LuaState
            state = LuaState.Create();
            // Expose the turtle to Lua as a global
            state.Environment["turtle"] = turtle;
        }

        public void Update(double deltaTime)
        {
            if (!scriptRan)
            {
                // Run a Lua script to control the turtle
                string script = @"
turtle:PenDownFunc()
for i=1,4 do
    turtle:Forward(60)
    turtle:Right(90)
end
turtle:PenUp()
";
                try
                {
                    // Lua-CSharp is async, but Update is not, so block for demo
                    state.DoStringAsync(script).GetAwaiter().GetResult();
                    scriptRan = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lua error: {ex.Message}");
                }
            }
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);
            surface.DrawText(10, 10, "Lua Turtle Demo", Colors.Yellow);
            surface.DrawText(10, 30, "Turtle is controlled by Lua!", Colors.White);
            // Optionally, draw the turtle as a triangle
            if (turtle != null)
            {
                int size = 10;
                double angleRad = turtle.Angle * Math.PI / 180.0;
                int x = turtle.X;
                int y = turtle.Y;
                int x1 = x + (int)(size * Math.Cos(angleRad));
                int y1 = y + (int)(size * Math.Sin(angleRad));
                int x2 = x + (int)(size * Math.Cos(angleRad + 2.5));
                int y2 = y + (int)(size * Math.Sin(angleRad + 2.5));
                int x3 = x + (int)(size * Math.Cos(angleRad - 2.5));
                int y3 = y + (int)(size * Math.Sin(angleRad - 2.5));
                surface.DrawLine(x, y, x1, y1, Colors.White);
                surface.DrawLine(x, y, x2, y2, Colors.White);
                surface.DrawLine(x, y, x3, y3, Colors.White);
            }
        }
    }
}
