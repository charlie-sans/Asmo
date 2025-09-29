using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Asmo.Window.input
{
    public class Mouse
    {
        private bool[] _buttonsDown = new bool[8];
        private bool[] _buttonsPressed = new bool[8];
        private bool[] _buttonsReleased = new bool[8];
        private readonly Asmo.Window.Window _window;
        
        public int X { get; private set; }
        public int Y { get; private set; }
        
        /// <summary>
        /// Convenience property for left mouse button pressed this frame
        /// </summary>
        public bool IsPressed => IsButtonPressed(MouseButton.Left);

        public Mouse(Asmo.Window.Window window)
        {
            _window = window;
            window.MouseDown += OnMouseDown;
            window.MouseUp += OnMouseUp;
            window.MouseMove += OnMouseMove;
        }

        private void OnMouseDown(MouseButtonEventArgs e)
        {
            int idx = (int)e.Button;
            if (idx >= 0 && idx < _buttonsDown.Length && !_buttonsDown[idx])
            {
                _buttonsDown[idx] = true;
                _buttonsPressed[idx] = true;
            }
        }

        private void OnMouseUp(MouseButtonEventArgs e)
        {
            int idx = (int)e.Button;
            if (idx >= 0 && idx < _buttonsDown.Length)
            {
                _buttonsDown[idx] = false;
                _buttonsReleased[idx] = true;
            }
        }

        private void OnMouseMove(MouseMoveEventArgs e)
        {
            // Get current window and framebuffer sizes
            double windowWidth = _window.Size.X;
            double windowHeight = _window.Size.Y;
            double framebufferWidth = _window.FrameBufferX;
            double framebufferHeight = _window.FrameBufferY;

            // Calculate scale to fit framebuffer into window while preserving aspect ratio
            double scale = Math.Min(windowWidth / framebufferWidth, windowHeight / framebufferHeight);

            // Calculate size of the displayed framebuffer in window coordinates
            double displayWidth = framebufferWidth * scale;
            double displayHeight = framebufferHeight * scale;

            // Calculate black bar offsets (letterboxing/pillarboxing)
            double offsetX = (windowWidth - displayWidth) / 2.0;
            double offsetY = (windowHeight - displayHeight) / 2.0;

            // Mouse position relative to the displayed framebuffer
            double mx = e.Position.X - offsetX;
            double my = e.Position.Y - offsetY;

            // Default to -1 (off framebuffer)
            int fx = -1, fy = -1;

            if (mx >= 0 && my >= 0 && mx < displayWidth && my < displayHeight)
            {
                // Map to framebuffer coordinates (Y=0 at top)
                fx = (int)Math.Round(mx / scale);
                fy = (int)Math.Round(my / scale);
                // Clamp to framebuffer bounds
                fx = Math.Clamp(fx, 0, (int)framebufferWidth - 1);
                fy = Math.Clamp(fy, 0, (int)framebufferHeight - 1);
            }

            X = fx;
            Y = fy;

            // Debug output occasionally
            if (X % 32 == 0 && Y % 32 == 0 && X >= 0 && Y >= 0) // Only log at certain positions to avoid spam
            {
                Console.WriteLine($"Mouse: Window({e.Position.X:F1}, {e.Position.Y:F1}) -> Framebuffer({X}, {Y})");
                Console.WriteLine($"Window: {windowWidth}x{windowHeight}, Framebuffer: {framebufferWidth}x{framebufferHeight}, Offset: {offsetX},{offsetY}, Scale: {scale:F2}");
            }
        }

        /// <summary>
        /// Call this at the start of each frame to reset pressed/released states.
        /// </summary>
        public void Update()
        {
            var mouseState = _window.MouseState;
            double windowWidth = _window.Size.X;
            double windowHeight = _window.Size.Y;
            double framebufferWidth = _window.FrameBufferX;
            double framebufferHeight = _window.FrameBufferY;

            // Calculate scale and offsets for letterboxing/pillarboxing
            double scale = Math.Min(windowWidth / framebufferWidth, windowHeight / framebufferHeight);
            double displayWidth = framebufferWidth * scale;
            double displayHeight = framebufferHeight * scale;
            double offsetX = (windowWidth - displayWidth) / 2.0;
            double offsetY = (windowHeight - displayHeight) / 2.0;

            double mx = mouseState.Position.X - offsetX;
            double my = mouseState.Position.Y - offsetY;

            int fx = -1, fy = -1;
            if (mx >= 0 && my >= 0 && mx < displayWidth && my < displayHeight)
            {
                fx = (int)Math.Round(mx / scale);
                fy = (int)Math.Round(my / scale);
                fx = Math.Clamp(fx, 0, (int)framebufferWidth - 1);
                fy = Math.Clamp(fy, 0, (int)framebufferHeight - 1);
            }
            X = fx;
            Y = fy;

            // Update button states
            for (int i = 0; i < _buttonsDown.Length; i++)
            {
                bool isDown = mouseState.IsButtonDown((MouseButton)i);
                _buttonsPressed[i] = isDown && !_buttonsDown[i];
                _buttonsReleased[i] = !isDown && _buttonsDown[i];
                _buttonsDown[i] = isDown;
            }
        }

        public bool IsButtonDown(MouseButton button) => _buttonsDown[(int)button];
        public bool IsButtonPressed(MouseButton button) => _buttonsPressed[(int)button];
        public bool IsButtonReleased(MouseButton button) => _buttonsReleased[(int)button];
    }
}
