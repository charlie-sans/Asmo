# Scene Demo

Demonstrates the built-in scene management system with a simple stack of scenes:

- Splash screen that automatically transitions to a main menu
- Menu that pushes a gameplay scene via a fade transition
- Gameplay scene that can pause (overlay) and bounce back to the menu

## Running

1. Build the project:
   ```powershell
   dotnet build ..\..\Asmo.slnx
   ```
2. Launch the main `Asmo` window (`dotnet run` from the repository root) and drag the compiled `SceneDemo.dll` onto it, or reference it from another host.

Controls:
- `Enter` – Start gameplay from the menu
- `Backspace` – Return to menu from gameplay
- `P` – Pause/resume gameplay overlay
- Arrow keys – Move the bouncing player orb
