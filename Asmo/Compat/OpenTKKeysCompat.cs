#if USE_RAYLIB
// Minimal compatibility shim so demos referencing OpenTK.Windowing.GraphicsLibraryFramework.Keys compile
namespace OpenTK.Windowing.GraphicsLibraryFramework
{
    public enum Keys
    {
        Left, Right, Up, Down,
        Home, End, Enter, Backspace, Tab, Space, Escape,
    B, I, U, W, A, S, D, P, Z,
    C, E, F, X, V, G, H, N, J, M, L,
        D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
        Period, Comma, Minus, Equal,
        Slash, Backslash, Semicolon, Apostrophe,
        LeftBracket, RightBracket,
        PageUp, PageDown,
        F1, F2, F3, F4,
        LeftShift, RightShift, LeftControl, RightControl,
        Delete
    }
    public enum MouseButton
    {
        Left, Right, Middle
    }
}
#endif
