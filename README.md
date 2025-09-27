# Asmo Game Framework & AstroTestGame

## Overview
This project is a simple, extensible C# game framework (Asmo) and a sample game (AstroTestGame) demonstrating its features. It is designed for rapid prototyping, retro-style games, and educational use.

## Features
- Pixel-based 2D graphics (draw text, rectangles, sprites, lines, circles, ellipses, etc.)
- Simple sound/chiptune playback and sound effect helpers
- Keyboard input
- Asset management
- Extensible game environment configuration
- Minimal immediate-mode GUI library (see `Gui/Gui.cs`)
- Example game with moving sprites, power-ups, sound, and screen shake

## Getting Started

### Prerequisites
- .NET 9 SDK or later
- Windows (Linux/Mac may work with minor changes)

### Building
1. Clone the repository:
   ```sh
   git clone https://github.com/charlie-sans/Asmo.git
   ```
2. Open the solution in Visual Studio or run:
   ```sh
   dotnet build
   ```

### Running
- Set `AstroTestGame` as the startup project and run it.
- The game window will open. Use the arrow keys to move, collect power-ups, and hit the DVD logo!

## Graphics Helpers
- `Surface.DrawLine(x0, y0, x1, y1, color)`
- `Surface.DrawCircle(cx, cy, radius, color)`
- `Surface.DrawFilledCircle(cx, cy, radius, color)`
- `Surface.DrawOutlinedRect(x, y, w, h, color)`
- `Surface.DrawEllipse(cx, cy, rx, ry, color)`

## GUI Library
See `Gui/Gui.cs` for usage:
```csharp
Gui.Begin(10, 10);
Gui.Label(surface, "Hello!", Colors.White);
bool pressed = Gui.Button(surface, "Click Me", Colors.Yellow, mouseX, mouseY, mouseDown);
bool checked = false;
Gui.Checkbox(surface, "Enable feature", ref checked, Colors.Cyan, mouseX, mouseY, mouseDown);
```

## GameEnvironment Configuration
- `GameEnvironment.AssetRoot` — Path to assets
- `GameEnvironment.WindowTitle` — Window title
- `GameEnvironment.DefaultFont` — Path to default font
- `GameEnvironment.ShowFps` — Show FPS counter
- `GameEnvironment.ConfigPath` — Path to config file

## Contributing
Pull requests and issues are welcome! Please:
- Use clear commit messages
- Add tests or sample usage for new features
- Keep code style consistent

## License
AGPL V3
