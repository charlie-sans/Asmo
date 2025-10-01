# Asmo Quickstart

Welcome! This guide gets you from clone to a running custom game in minutes.

## 1. Prerequisites
- .NET 9 SDK
- Windows 10/11 (OpenTK + GLFW currently tuned for Windows build)
- GPU with basic OpenGL 3.3+ support

Check versions:
```powershell
dotnet --version
```

## 2. Clone & Build
```powershell
git clone https://github.com/charlie-sans/Asmo.git
cd Asmo
dotnet build
```

## 3. Run a Demo
List demos:
```powershell
dir .\Demos
```
Run the runtime host:
```powershell
dotnet run --project .\AsmoRuntime
```
Drag & drop a demo game's build output folder (e.g. `Demos/AstroTestGame/bin/Debug/net9.0`) onto the window to launch it.

## 4. Create Your Own Minimal Game
Create a new folder under `Demos` (example: `MyFirstGame`). Add a class implementing `IConsoleGame`:
```csharp
using Asmo.Gfx;
using Asmo;

public class MyFirstGame : IConsoleGame {
    int t = 0;
    public void Load(ConsoleHost host, Surface fb) { }
    public void Update(ConsoleHost host, Surface fb, double delta) {
        t++;
        fb.Clear(Colors.Black);
        Font.Default.DrawText(fb, 4, 4, $"Hello Asmo {t}", Colors.White);
    }
}
```
Then modify your chosen launcher (e.g. a small `Program.cs`) or use the runtime host to point to your assembly. Fast path: temporarily swap the default demo the runtime loads.

## 5. Loading Assets
Put images / audio in a folder and load via helpers:
```csharp
var sprite = SpriteLoader.Load("Assets/player.png");
```
Fonts: `RuntimeFontAtlas` builds from TTF at startup if needed.

## 6. Input Basics
Access keyboard state via the `ConsoleHost` or window callbacks (simplified wrappers planned). For now typical pattern inside Update:
```csharp
if (host.IsKeyDown(Keys.Left)) { /* move */ }
```

## 7. Audio Basics
(Coming soon) Mixer refactor will change this. Current pattern:
```csharp
var clip = AudioClip.Load("Audio/laser.wav");
clip.Play();
```

## 8. Scenes (If Enabled)
With the scene manager integrated:
```csharp
sceneManager.Push(new MainMenuScene());
```
Documentation pending final migration—see `scene-management-system.md` for design.

## 9. MASM (Experimental)
MASM integration is under active development. Refer to `masm-integration.md` for status.

## 10. Build Configuration Tips
- Use `dotnet build -c Release` for faster runtime.
- VS / Rider: enable multi-target logs if diagnosing OpenTK issues.

## 11. Troubleshooting
| Symptom | Tip |
|---------|-----|
| Garbled window title | Avoid per-frame Title sets; use stable assignment once at startup. |
| Black window | Ensure OpenGL 3.3 drivers installed; update GPU drivers. |
| Audio pops | Check sample rate & avoid allocating per frame. |

## 12. Next Steps
- Read the roadmap: `Plans/2025-10-roadmap.md`
- Explore demos under `Demos/`
- Try adding a GUI element (`Gui/Gui.cs`)
- Watch for upcoming debug overlay & mixer changes

## 13. Contributing
Open an issue for feature proposals; attach minimal repros for bugs. PRs should include a short note in `CHANGELOG.md` (to be added) and, when possible, a new demo or test.

---
Happy hacking!
