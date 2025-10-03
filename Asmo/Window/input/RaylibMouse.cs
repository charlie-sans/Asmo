#if USE_RAYLIB
using System;
using Raylib_cs;

namespace Asmo.Window.input
{
    /// <summary>
    /// Runtime mouse state provider for the Raylib backend. Handles letterboxing mapping
    /// from window coordinates to logical framebuffer coordinates.
    /// </summary>
    public sealed class RaylibMouse
    {
        private readonly Func<(int fbW, int fbH)> _getFramebufferSize;
        private int _lastButtons; // bitmask previous frame
        private int _pressed;     // bitmask of buttons pressed this frame
        private int _released;    // bitmask of buttons released this frame

        public int X { get; private set; } = -1; // logical framebuffer coords or -1 if off
        public int Y { get; private set; } = -1;

        public RaylibMouse(Func<(int fbW, int fbH)> getFramebufferSize)
        {
            _getFramebufferSize = getFramebufferSize;
        }

        /// <summary>
        /// Update mouse state. Must be called once per frame before querying Pressed/Released.
        /// </summary>
        public void Update()
        {
            var (fbW, fbH) = _getFramebufferSize();
            int winW = Raylib.GetScreenWidth();
            int winH = Raylib.GetScreenHeight();

            // Compute scale preserving aspect (same logic as renderer's letterbox)
            float srcAspect = (float)fbW / fbH;
            float winAspect = (float)winW / winH;
            float drawW, drawH;
            if (winAspect > srcAspect)
            {
                drawH = winH;
                drawW = drawH * srcAspect;
            }
            else
            {
                drawW = winW;
                drawH = drawW / srcAspect;
            }
            float offsetX = (winW - drawW) * 0.5f;
            float offsetY = (winH - drawH) * 0.5f;

            int rawX = Raylib.GetMouseX();
            int rawY = Raylib.GetMouseY();
            float relX = rawX - offsetX;
            float relY = rawY - offsetY;
            if (relX >= 0 && relY >= 0 && relX < drawW && relY < drawH)
            {
                float scale = drawW / fbW; // drawW = fbW * scale
                int fx = (int)MathF.Floor(relX / scale);
                int fy = (int)MathF.Floor(relY / scale);
                fx = Math.Clamp(fx, 0, fbW - 1);
                fy = Math.Clamp(fy, 0, fbH - 1);
                X = fx; Y = fy;
            }
            else
            {
                X = Y = -1; // off framebuffer
            }

            // Buttons (support up to first 3 for now)
            int current = 0;
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT)) current |= 1 << 0;
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT)) current |= 1 << 1;
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE)) current |= 1 << 2;

            int changed = current ^ _lastButtons;
            _pressed = changed & current;          // bits that are now down
            _released = changed & _lastButtons;    // bits that were down and now up
            _lastButtons = current;
        }

        public bool IsButtonDown(MouseButton btn) => btn switch
        {
            MouseButton.MOUSE_BUTTON_LEFT => (_lastButtons & (1 << 0)) != 0,
            MouseButton.MOUSE_BUTTON_RIGHT => (_lastButtons & (1 << 1)) != 0,
            MouseButton.MOUSE_BUTTON_MIDDLE => (_lastButtons & (1 << 2)) != 0,
            _ => false
        };
        public bool IsButtonPressed(MouseButton btn) => btn switch
        {
            MouseButton.MOUSE_BUTTON_LEFT => (_pressed & (1 << 0)) != 0,
            MouseButton.MOUSE_BUTTON_RIGHT => (_pressed & (1 << 1)) != 0,
            MouseButton.MOUSE_BUTTON_MIDDLE => (_pressed & (1 << 2)) != 0,
            _ => false
        };
        public bool IsButtonReleased(MouseButton btn) => btn switch
        {
            MouseButton.MOUSE_BUTTON_LEFT => (_released & (1 << 0)) != 0,
            MouseButton.MOUSE_BUTTON_RIGHT => (_released & (1 << 1)) != 0,
            MouseButton.MOUSE_BUTTON_MIDDLE => (_released & (1 << 2)) != 0,
            _ => false
        };
    }
}
#endif
