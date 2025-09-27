using OpenTK;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Asmo;
public class Program
{
    public static void Main(string[] args)
    {
        using (var game = new Window.Window())
        {
            game.Run();
        }
    }
}