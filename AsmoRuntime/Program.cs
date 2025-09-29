using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Asmo;
public class Startup
{
    public static void Main(string[] args)
    {
        // we really should be doing more here... sad to see this as dead as it is.
        // but for now, this is just a stub to launch the window.
        using (var game = new Window.Window())
        {
            game.Run();
        }
    }
}