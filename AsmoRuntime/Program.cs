using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL3;
namespace Asmo;
public class Startup
{
    public static void Main(string[] args)
    {
        // we really should be doing more here... sad to see this as dead as it is.
        // but for now, this is just a stub to launch the window.

        bool useVr = false;
        foreach (var arg in args)
        {
            if (arg == "--vr" || arg == "-vr") useVr = true;
        }

        using (var game = new Window.Window(new OpenTK.Windowing.Desktop.GameWindowSettings()
        {
            Win32SuspendTimerOnDrag = true,
            UpdateFrequency = 30.0
        }, useVr))
        {
            game.Run();
        }
    }
}