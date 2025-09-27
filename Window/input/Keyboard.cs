using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;

namespace Asmo.Window.input
{
	public class Keyboard
	{
		private readonly HashSet<Keys> _keysDown = new();
		private readonly HashSet<Keys> _keysPressed = new();
		private readonly HashSet<Keys> _keysReleased = new();

		public Keyboard(Asmo.Window.Window window)
		{
			// Subscribe to window events
			window.KeyDown += OnKeyDown;
			window.KeyUp += OnKeyUp;
		}

		private void OnKeyDown(KeyboardKeyEventArgs e)
		{
			if (_keysDown.Add(e.Key))
				_keysPressed.Add(e.Key);
		}

		private void OnKeyUp(KeyboardKeyEventArgs e)
		{
			_keysDown.Remove(e.Key);
			_keysReleased.Add(e.Key);
		}

		/// <summary>
		/// Call this at the start of each frame to reset pressed/released states.
		/// </summary>
		public void Update()
		{
			_keysPressed.Clear();
			_keysReleased.Clear();
		}

		public bool IsKeyDown(Keys key) => _keysDown.Contains(key);
		public bool IsKeyPressed(Keys key) => _keysPressed.Contains(key);
		public bool IsKeyReleased(Keys key) => _keysReleased.Contains(key);
	}
}
