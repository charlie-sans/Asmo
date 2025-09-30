using System;
using Asmo.Window;
using Microsoft.Xna.Framework;
namespace Asmo
{
    public static class AsmoHost
    {
        /// <summary>
        /// Launch the Asmo application in standalone mode (for debugging/testing from any game)
        /// </summary>
        /// <summary>
        /// Launch the Asmo application in standalone mode, using a custom window/game factory.
        /// </summary>
        /// <param name="windowFactory">A function that returns a new Window.Window or derived instance.</param>
        public static void LaunchStandalone(Func<Asmo.Gfx.IConsoleGame> windowFactory)
        {
            // Create the game instance using the provided factory
                var game = windowFactory();
            // Create the main window
            
            var consoleHost = new ConsoleHost();
            
            var window = new Asmo.Window.Window(new OpenTK.Windowing.Desktop.GameWindowSettings()
            {
                UpdateFrequency = 144.0,
            });
            // Load the game into the window
            consoleHost.LoadGame(game, window.framebuffer);
            // Run the window (starts the main loop)
            Console.WriteLine("Starting Asmo in standalone mode...");
            window.Run();
        }
    }
}