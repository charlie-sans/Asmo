
# Asmo Game Console Framework

Welcome to **Asmo**! This is a fun, modern C#/.NET framework for making retro-style and modern 2D games. It comes with a simple sample game (AstroTestGame) to show you how it works.

---

## 🚀 Quick Start

1. **Requirements:**
   - .NET 9 SDK or newer
   - Windows (Linux/Mac might work with tweaks)

2. **Build & Run:**
   ```sh
   git clone https://github.com/charlie-sans/Asmo.git
   cd Asmo
   dotnet run
   ```
   Or open the solution in Visual Studio and run AstroTestGame.

2.5. **Drag and drop:**
   drag and drop the net9.0 folder from the AstroTestGame/bin/Debug/net9.0 folder on the asmo window and the game will start

3. **Play!**
   - Arrow keys to move
   - Collect power-ups
   - Hit the DVD logo for fun

---

## 🎮 Features

- Easy 2D pixel graphics (draw text, shapes, sprites, etc.)
- Chiptune & sound effect playback
- Keyboard input
- Simple asset management
- Minimal, immediate-mode GUI (see `Gui/Gui.cs`)
- Example game included

---

## 🖼️ Drawing & GUI Example

```csharp
Gui.Begin(10, 10);
Gui.Label(surface, "Hello!", Colors.White);
if (Gui.Button(surface, "Click Me", Colors.Yellow, mouseX, mouseY, mouseDown)) {
    // Button was clicked!
}
```

---

## ⚙️ Customization

You can tweak the game environment in code:

- `GameEnvironment.AssetRoot` — Where your assets live
- `GameEnvironment.WindowTitle` — Window title
- `GameEnvironment.ShowFps` — Show FPS counter

---

## 🤝 Contributing

Pull requests and issues are welcome! Please keep code style tidy and add examples for new features.

---

## 📄 License

AGPL v3
