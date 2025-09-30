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

		// Buffer for pressed characters
		private readonly List<char> _pressedChars = new();

		public Keyboard(Asmo.Window.Window window)
		{
			// Subscribe to window events
			window.KeyDown += OnKeyDown;
			window.KeyUp += OnKeyUp;
			window.TextInput += OnTextInput;
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

		// Handle text input events for character input
		private void OnTextInput(TextInputEventArgs e)
		{
			char ch = (char)e.Unicode;
			if (!char.IsControl(ch))
				_pressedChars.Add(ch);
		}

		/// <summary>
		/// Call this at the start of each frame to reset pressed/released states and clear char buffer.
		/// </summary>
		public void Update()
		{
			_keysPressed.Clear();
			_keysReleased.Clear();
			_pressedChars.Clear();
		}

		public bool IsKeyDown(Keys key) => _keysDown.Contains(key);
		public bool IsKeyPressed(Keys key) => _keysPressed.Contains(key);
		public bool IsKeyReleased(Keys key) => _keysReleased.Contains(key);

		/// <summary>
		/// Returns the characters typed since the last frame.
		/// </summary>
		public IEnumerable<char> GetPressedChars()
		{
			return _pressedChars;
		}
	}
}
