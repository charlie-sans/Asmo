#if USE_RAYLIB
using Raylib_cs;
using OTMb = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;

namespace Asmo.Window.input
{
    // Simple mouse shim replicating minimal original API used by demos
    public class Mouse
    {
        public int X { get; private set; }
        public int Y { get; private set; }
    public bool IsPressed => Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT);
    public bool WasPressedThisFrame => Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);

        public Mouse() {}
        public Mouse(object? _) {} // legacy ctor signature

        public bool IsButtonDown(OTMb btn) => btn switch
        {
            OTMb.Left => Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT),
            OTMb.Right => Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT),
            OTMb.Middle => Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE),
            _ => false
        };

        public void Update(int surfaceWidth, int surfaceHeight)
        {
            X = Raylib.GetMouseX();
            Y = Raylib.GetMouseY();
            // (Could add letterbox transform if needed later)
        }
    }
}
#endif