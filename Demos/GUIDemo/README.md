# GUI Demo

Demonstrates Asmo's enhanced immediate-mode GUI features: theming, keyboard navigation, sliders, toggle switches, progress bars, and text input.

## Showcased Features
- Theming system (`GuiTheme.DarkDefault`, `GuiTheme.Light`) with runtime switching.
- Focus & navigation: Arrow keys / Tab / Shift+Tab to move focus; Enter/Space to activate.
- Widgets: Button, ToggleSwitch, SliderFloat, ProgressBar, TextInput, Label hierarchy with indenting.
- Layout helpers: indent management, sequential vertical flow.
- Simple log list showing interaction events.

## Controls
- Up / Down or Tab / Shift+Tab: change focused widget.
- Enter / Space / Left click: activate or toggle.
- Type when the text field is focused to append characters; Backspace to delete.
- Click the theme button to switch palettes.

## File Overview
- `Game.cs` – Demo implementation calling the `Gui` API each frame.
- `GuiTheme.cs` – Theme & input definitions (in main project, not inside this folder).

## Future Enhancements (Not yet implemented here)
- Dropdown/select widget.
- Controller (gamepad) navigation mapping.
- Disabled widget states & tooltips.
- Scrollable panels.

Enjoy experimenting with the new GUI layer!
