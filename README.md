
# Asmo Game Console Framework

Welcome to **Asmo**! This is a fun, modern C#/.NET framework for making retro-style and modern 2D games. It comes with a simple sample game (AstroTestGame) to show you how it works.

---

## 🚀 Quick Start (Summary)
Full guide: see `docs/quickstart.md`.

1. Install .NET 9 SDK
2. Clone & build:
   ```powershell
   git clone https://github.com/charlie-sans/Asmo.git
   cd Asmo
   dotnet run --project .\AsmoRuntime
   ```
3. Drag & drop a demo game's `bin/Debug/net9.0` folder onto the window to launch it.
4. Open `docs/quickstart.md` to build your own minimal game.

---

## 🎮 Features (Current Slice)

- 2D pixel framebuffer + text & sprite rendering
- Basic audio playback (mixer overhaul planned)
- Keyboard input (gamepad module planned)
- Simple asset loading helpers
- Immediate-mode GUI (`Gui/Gui.cs`)
- Sample demos (`Demos/`)
- Early scene system (see `Plans/scene-management-system.md`)

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

## ⚙️ Configuration Hints
- `GameEnvironment.AssetRoot` – asset lookup base
- `GameEnvironment.WindowTitle` – initial window title (avoid per-frame changes)
- `GameEnvironment.ShowFps` – overlay FPS counter

---

## 🤝 Contributing
See roadmap: `Plans/2025-10-roadmap.md` for active epics.
Guidelines (draft):
- Prefer small, focused PRs
- Include a demo or test where feasible
- Document new public APIs briefly in `docs/` (or create a stub)

---


## Notes.

when developing games with Asmo, sometimes Dotnet won't grab your libraries correctly. If you encounter issues, try the following steps:
1. set the output type to Exe in your .csproj file
2. define a empty Main method in your game project "tricks" dotnet to treat it as an executable and include all dependencies correctly.
3. build your project again.

that should resolve most dependency issues.
---

## 📄 License

AGPL v3
