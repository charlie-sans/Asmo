# Audio Demo

Interactive showcase for the shared Asmo audio engine. The demo focuses on practical features like buses, fades, panning, and randomized one-shot sounds so you can sanity-check the mixer without jumping into the larger scene framework.

## Controls

| Keys | Action |
| ---- | ------ |
| `1` | Toggle the ambient pad loop (music bus) |
| `2` | Toggle a pulsing loop with automatic panning |
| `Space` | Fire a randomized one-shot SFX on the SFX bus |
| `Backspace` | Fade out loops and stop all playback |
| `H` / `J` | Decrease / increase music bus volume |
| `N` / `M` | Decrease / increase SFX bus volume |
| `Up` / `Down` | Adjust the master bus volume |
| `Left` / `Right` | Shift the default pan for newly triggered SFX |

A small HUD shows current bus volumes, loop state, and the last action performed so you can visually confirm what the mixer is doing.

## Building

```powershell
cd D:\Asmo
dotnet build Demos\AudioDemo\AudioDemo.csproj
```

Drop the resulting DLL into the Asmo runtime window or load it via your preferred workflow to try out the sounds.
