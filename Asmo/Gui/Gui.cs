using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Gui
{
    public static class Gui
    {
        // Layout state
        private static int cursorX = 10, cursorY = 10;
        private static int baseX = 10, baseY = 10;
        private static int lastItemWidth = 0, lastItemHeight = 0, lastItemStartY = 0;
        private static int indentLevel = 0;

        // Navigation / focus state
        private static int focusedIndex = 0;
        private static int widgetCountLastFrame = 0;
        private static int widgetCountThisFrame = 0;
        private static bool focusDirty = false; // when clicking sets focus
        private static GuiInput currentInput;
    private static GuiInput prevInput; // for edge detection

    // Key edge flags (set each BeginFrame)
    private static bool keyActivateEdge, keyBackEdge, keyUpEdge, keyDownEdge, keyLeftEdge, keyRightEdge, keyNextFieldEdge, keyPrevFieldEdge;

    // Mouse edge detection & cached positional state
    private static bool prevMouseDown = false;
    private static bool currentMouseDown = false;
    private static int currentMouseX = 0;
    private static int currentMouseY = 0;

    // Clipping / scroll panel state
    private struct ClipRect { public int X, Y, W, H; }
    private static readonly System.Collections.Generic.Stack<ClipRect> clipStack = new();
    private static bool IsInsideClip(int x, int y)
    {
        if (clipStack.Count == 0) return true;
        var c = clipStack.Peek();
        return x >= c.X && x < c.X + c.W && y >= c.Y && y < c.Y + c.H;
    }
    private static void SetClippedPixel(Surface s, int x, int y, Color color)
    {
        if (IsInsideClip(x, y)) s.SetPixel(x, y, color);
    }

    // Dropdown persistent state
    private class DropdownState { public bool Open; }
    private static readonly System.Collections.Generic.Dictionary<string, DropdownState> dropdownStates = new();

        // Theme
        private static GuiTheme theme = GuiTheme.DarkDefault;
        public static GuiTheme Theme => theme;
        public static void SetTheme(GuiTheme t) { if (t != null) theme = t; }

        // Public API: begin new frame (with optional input) anchored at position
        public static void BeginFrame(int x, int y, GuiInput input)
        {
            // Apply navigation BEFORE resetting widgets so movement works using last frame counts
            currentInput = input;
            // Compute key edges
            keyActivateEdge = input.Activate && !prevInput.Activate;
            keyBackEdge = input.Back && !prevInput.Back;
            keyUpEdge = input.Up && !prevInput.Up;
            keyDownEdge = input.Down && !prevInput.Down;
            keyLeftEdge = input.Left && !prevInput.Left;
            keyRightEdge = input.Right && !prevInput.Right;
            keyNextFieldEdge = input.NextField && !prevInput.NextField;
            keyPrevFieldEdge = input.PrevField && !prevInput.PrevField;

            if (keyBackEdge)
                focusedIndex = -1; // unfocus on Escape / Back (edge only)
            if (widgetCountLastFrame > 0)
            {
                if (keyDownEdge || keyNextFieldEdge)
                    focusedIndex = focusedIndex < 0 ? 0 : (focusedIndex + 1) % widgetCountLastFrame;
                else if (keyUpEdge || keyPrevFieldEdge)
                    focusedIndex = focusedIndex < 0 ? widgetCountLastFrame - 1 : (focusedIndex - 1 + widgetCountLastFrame) % widgetCountLastFrame;
            }
            widgetCountThisFrame = 0;
            baseX = x; baseY = y;
            cursorX = x; cursorY = y;
            indentLevel = 0;
            currentMouseDown = false; // reset; will be set if overload with mouseDown used
        }

        // Overload capturing mouse state for proper edge detection
        public static void BeginFrame(int x, int y, GuiInput input, bool mouseDown)
        {
            BeginFrame(x, y, input);
            currentMouseDown = mouseDown;
        }

        // Combined nav + mouse frame input (IMGUI-style convenience)
        public struct GuiFrameInput
        {
            public GuiInput Nav;
            public int MouseX;
            public int MouseY;
            public bool MouseDown;
        }

        public static void BeginFrame(int x, int y, GuiFrameInput frameInput)
        {
            BeginFrame(x, y, frameInput.Nav, frameInput.MouseDown);
            currentMouseX = frameInput.MouseX;
            currentMouseY = frameInput.MouseY;
        }

        // Backwards-compatible overload (no navigation / default input)
        public static void Begin(int x = 10, int y = 10)
        {
            BeginFrame(x, y, default(GuiInput));
        }

        public static void EndFrame()
        {
            widgetCountLastFrame = widgetCountThisFrame;
            focusDirty = false;
            prevMouseDown = currentMouseDown;
            prevInput = currentInput; // store for next-frame edge detection
        }

        public static void ClearFocus() => focusedIndex = -1;

        private static int NextWidgetIndex() => widgetCountThisFrame++;
        private static bool IsFocused(int index) => index == focusedIndex;
        private static bool ActivationRequested(bool hovered, bool mousePressEdge, bool isFocused)
            => (hovered && mousePressEdge) || (isFocused && keyActivateEdge);
        private static void RequestFocus(int index)
        {
            focusedIndex = index;
            focusDirty = true;
        }

        private static int LineSpacingY => theme?.ItemSpacingY ?? 22;
        private static int IndentSize => theme?.IndentSize ?? 16;

        public static void PushIndent() => indentLevel++;
        public static void PopIndent() { if (indentLevel > 0) indentLevel--; }

        public static void SameLine(int spacingX = -1)
        {
            cursorX += lastItemWidth + (spacingX >= 0 ? spacingX : (theme?.ItemSpacingX ?? 8));
            cursorY = lastItemStartY; // restore to start line for horizontal layering
        }

        private static void NewLine()
        {
            cursorX = baseX + indentLevel * IndentSize;
            cursorY += LineSpacingY;
        }
        public static void DrawVerticalGradientRect(Asmo.Gfx.Surface surface, int x, int y, int w, int h, Asmo.Types.Color top, Asmo.Types.Color bottom)
    {
        for (int i = 0; i < h; i++)
        {
            float t = h > 1 ? (float)i / (h - 1) : 0f;
            byte r = (byte)(top.R + t * (bottom.R - top.R));
            byte g = (byte)(top.G + t * (bottom.G - top.G));
            byte b = (byte)(top.B + t * (bottom.B - top.B));
            byte a = (byte)(top.A + t * (bottom.A - top.A));
            var rowColor = new Asmo.Types.Color(r, g, b, a);
            for (int j = 0; j < w; j++)
                surface.SetPixel(x + j, y + i, rowColor);
        }
    }

        public static void Label(Surface surface, string text, Color? overrideColor = null)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            var c = overrideColor ?? theme.Text;
            surface.DrawText(cursorX, cursorY, text, c);
            lastItemWidth = text.Length * 7; // rough width heuristic
            lastItemHeight = LineSpacingY;
            NewLine();
        }

        public static bool Button(Surface surface, string text, Color color, int mouseX, int mouseY, bool mouseDown)
        {
            // legacy signature keeps explicit color parameter as fallback (so old code still works)
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            int w = text.Length * 8 + 20;
            int h = 28;
            int radius = 6;
            // Shadow
            Color shadow = new Color(0, 0, 0, 60);
            surface.DrawFilledCircle(cursorX + radius + 2, cursorY + radius + 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + w - radius - 2, cursorY + radius + 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + radius + 2, cursorY + h - radius - 2, radius, shadow);
            surface.DrawFilledCircle(cursorX + w - radius - 2, cursorY + h - radius - 2, radius, shadow);
            surface.DrawRect(cursorX + 2, cursorY + radius + 2, w - 4, h - 2 * radius - 4, shadow);
            surface.DrawRect(cursorX + radius + 2, cursorY + 2, w - 2 * radius - 4, h - 4, shadow);

            // State colors
            bool hovered = mouseX >= cursorX && mouseX < cursorX + w && mouseY >= cursorY && mouseY < cursorY + h;
            bool focused = IsFocused(idx);
            bool mousePressed = mouseDown && !prevMouseDown;
            bool clicked = ActivationRequested(hovered, mousePressed, focused);
            if (mousePressed && hovered)
                RequestFocus(idx);
            // Use theme if available (color param is fallback border/text)
            Color bg = clicked ? theme.ButtonActive : hovered || focused ? theme.ButtonHovered : theme.Button;
            Color fg = color.A > 0 ? color : theme.Text;
            Color border = focused ? theme.BorderFocus : theme.Border;

            // Filled rounded rect (simulate with circles + rects)
            surface.DrawFilledCircle(cursorX + radius, cursorY + radius, radius, bg);
            surface.DrawFilledCircle(cursorX + w - radius - 1, cursorY + radius, radius, bg);
            surface.DrawFilledCircle(cursorX + radius, cursorY + h - radius - 1, radius, bg);
            surface.DrawFilledCircle(cursorX + w - radius - 1, cursorY + h - radius - 1, radius, bg);
            surface.DrawRect(cursorX + radius, cursorY, w - 2 * radius, h, bg);
            surface.DrawRect(cursorX, cursorY + radius, w, h - 2 * radius, bg);

            // Border
            surface.DrawOutlinedRect(cursorX, cursorY, w, h, border);
            if (focused)
            {
                // subtle focus halo
                surface.DrawOutlinedRect(cursorX - 1, cursorY - 1, w + 2, h + 2, theme.BorderFocus);
            }

            // Text
            surface.DrawText(cursorX + (w - text.Length * 7) / 2, cursorY + (h - 12) / 2, text, fg);
            lastItemWidth = w; lastItemHeight = h; cursorY += LineSpacingY + 10; cursorX = baseX + indentLevel * IndentSize;
            return clicked;
        }

        // Themed overload without needing to pass a color
        public static bool Button(Surface surface, string text, int mouseX, int mouseY, bool mouseDown)
            => Button(surface, text, theme.Text, mouseX, mouseY, mouseDown);

        // IMGUI-style overload using per-frame cached mouse state
        public static bool Button(Surface surface, string text)
            => Button(surface, text, theme.Text, currentMouseX, currentMouseY, currentMouseDown);

        public static bool Checkbox(Surface surface, string text, ref bool value, Color color, int mouseX, int mouseY, bool mouseDown)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            int boxSize = 14;
            bool hovered = mouseX >= cursorX && mouseX < cursorX + boxSize && mouseY >= cursorY && mouseY < cursorY + boxSize;
            bool focused = IsFocused(idx);
            bool mousePressed = mouseDown && !prevMouseDown;
            bool clicked = ActivationRequested(hovered, mousePressed, focused);
            if (mousePressed && hovered)
                RequestFocus(idx);
            if (clicked) value = !value;
            var border = focused ? theme.BorderFocus : theme.Border;
            surface.DrawOutlinedRect(cursorX, cursorY, boxSize, boxSize, border);
            var fill = value ? theme.Highlight : theme.InputBg;
            if (value)
                surface.DrawFilledCircle(cursorX + boxSize / 2, cursorY + boxSize / 2, 5, fill);
            surface.DrawText(cursorX + boxSize + 6, cursorY, text, theme.Text);
            lastItemWidth = boxSize + 6 + text.Length * 7; lastItemHeight = boxSize;
            NewLine();
            return clicked;
        }

        public static bool Checkbox(Surface surface, string text, ref bool value)
            => Checkbox(surface, text, ref value, theme.Text, currentMouseX, currentMouseY, currentMouseDown);

        public static bool ToggleSwitch(Surface surface, string text, ref bool value, int mouseX, int mouseY, bool mouseDown)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            int w = 36; int h = 18; int radius = h / 2;
            bool hovered = mouseX >= cursorX && mouseX < cursorX + w && mouseY >= cursorY && mouseY < cursorY + h;
            bool focused = IsFocused(idx);
            bool mousePressed = mouseDown && !prevMouseDown;
            bool clicked = ActivationRequested(hovered, mousePressed, focused);
            if (mousePressed && hovered)
                RequestFocus(idx);
            if (clicked) value = !value;
            Color track = value ? theme.ToggleOn : theme.ToggleOff;
            Color knob = theme.SliderGrab;
            // Track (rounded): approximated by two circles + rect
            surface.DrawFilledCircle(cursorX + radius, cursorY + radius, radius, track);
            surface.DrawFilledCircle(cursorX + w - radius - 1, cursorY + radius, radius, track);
            surface.DrawRect(cursorX + radius, cursorY, w - 2 * radius, h, track);
            int knobX = value ? cursorX + w - radius - 1 : cursorX + radius;
            surface.DrawFilledCircle(knobX, cursorY + radius, radius - 2, knob);
            if (focused)
                surface.DrawOutlinedRect(cursorX - 1, cursorY - 1, w + 2, h + 2, theme.BorderFocus);
            surface.DrawText(cursorX + w + 8, cursorY + 2, text, theme.Text);
            lastItemWidth = w + 8 + text.Length * 7; lastItemHeight = h;
            NewLine();
            return clicked;
        }

        public static bool ToggleSwitch(Surface surface, string text, ref bool value)
            => ToggleSwitch(surface, text, ref value, currentMouseX, currentMouseY, currentMouseDown);

        public static bool SliderFloat(Surface surface, string label, ref float value, float min, float max, int width, int mouseX, int mouseY, bool mouseDown)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            int h = 16;
            int labelW = label.Length * 7 + 10;
            int trackX = cursorX + labelW;
            int trackY = cursorY + 2;
            int trackW = width - labelW;
            if (trackW < 30) trackW = 30;
            bool hovered = mouseX >= trackX && mouseX < trackX + trackW && mouseY >= trackY && mouseY < trackY + h;
            bool focused = IsFocused(idx);
            bool active = hovered && mouseDown;
            if ((active || (focused && currentInput.Activate)) && max > min)
            {
                float t = (mouseX - trackX) / (float)(trackW - 1);
                if (!active) t = (value - min) / (max - min); // keyboard activation doesn't drag
                t = t < 0 ? 0 : t > 1 ? 1 : t;
                value = min + t * (max - min);
            }
            // track bg
            surface.DrawOutlinedRect(trackX, trackY, trackW, h, focused ? theme.BorderFocus : theme.Border);
            // fill portion
            float norm = (max > min) ? (value - min) / (max - min) : 0f;
            int filled = (int)(norm * (trackW - 2));
            for (int fy = 1; fy < h - 1; fy++)
                for (int fx = 1; fx <= filled && fx < trackW - 1; fx++)
                    surface.SetPixel(trackX + fx, trackY + fy, theme.ProgressBarFill);
            // knob
            int knobX = trackX + 1 + filled;
            surface.DrawOutlinedRect(knobX - 1, trackY, 3, h, theme.SliderGrab);
            // label + value
            string valStr = ((int)value).ToString();
            surface.DrawText(cursorX, cursorY, label, theme.Text);
            surface.DrawText(trackX + trackW + 6, cursorY, valStr, theme.Text);
            lastItemWidth = trackW + labelW + valStr.Length * 7 + 6; lastItemHeight = h;
            NewLine();
            return hovered && mouseDown; // return true when user is interacting
        }

        public static bool SliderFloat(Surface surface, string label, ref float value, float min, float max, int width)
            => SliderFloat(surface, label, ref value, min, max, width, currentMouseX, currentMouseY, currentMouseDown);

        public static void ProgressBar(Surface surface, float value, int width, int height)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            if (width < 20) width = 20; if (height < 8) height = 8;
            float clamped = value < 0 ? 0 : value > 1 ? 1 : value;
            // Simple border (no clipping logic yet)
            for (int bx = 0; bx < width; bx++)
            {
                SetClippedPixel(surface, cursorX + bx, cursorY, theme.Border);
                SetClippedPixel(surface, cursorX + bx, cursorY + height - 1, theme.Border);
            }
            for (int by = 0; by < height; by++)
            {
                SetClippedPixel(surface, cursorX, cursorY + by, theme.Border);
                SetClippedPixel(surface, cursorX + width - 1, cursorY + by, theme.Border);
            }
            int fillW = (int)(clamped * (width - 2));
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x <= fillW && x < width - 1; x++)
                    SetClippedPixel(surface, cursorX + x, cursorY + y, theme.ProgressBarFill);
            lastItemWidth = width; lastItemHeight = height;
            NewLine();
        }

        // Dropdown widget: returns true if selection changed.
        public static bool Dropdown(Surface surface, string label, string[] options, ref int selectedIndex, int width, int mouseX, int mouseY, bool mouseDown)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            if (options == null || options.Length == 0)
            {
                options = System.Array.Empty<string>();
                selectedIndex = -1;
            }
            if (selectedIndex < 0 || selectedIndex >= options.Length)
                selectedIndex = options.Length > 0 ? 0 : -1;

            // Acquire state
            if (!dropdownStates.TryGetValue(label, out var state))
                dropdownStates[label] = state = new DropdownState();

            int itemHeight = 16;
            int labelWidth = label.Length * 7 + 8;
            int boxX = cursorX + labelWidth;
            int boxY = cursorY;
            int boxW = width > 40 ? width : 80;
            int boxH = itemHeight;
            bool hoveredBox = mouseX >= boxX && mouseX < boxX + boxW && mouseY >= boxY && mouseY < boxY + boxH;
            bool focused = IsFocused(idx);
            bool mousePressed = mouseDown && !prevMouseDown;
            bool activation = ActivationRequested(hoveredBox, mousePressed, focused);

            // Toggle open/close on activation
            if (activation)
            {
                state.Open = !state.Open;
                RequestFocus(idx);
            }

            // Close on Back or unfocus
            if (currentInput.Back && focused)
                state.Open = false;

            // Keyboard navigation inside open dropdown
            bool selectionChanged = false;
            if (state.Open && focused && options.Length > 0)
            {
                    if (keyDownEdge)
                {
                    selectedIndex = (selectedIndex + 1) % options.Length; selectionChanged = true;
                }
                    else if (keyUpEdge)
                {
                    selectedIndex = (selectedIndex - 1 + options.Length) % options.Length; selectionChanged = true;
                }
                    if (keyActivateEdge)
                {
                    state.Open = false; // choose & close
                }
            }

            // If open and click outside list+box: close
            if (state.Open && mousePressed && !hoveredBox)
            {
                // compute list area bounds
                int listY = boxY + boxH;
                int listH = options.Length * itemHeight;
                bool insideList = mouseX >= boxX && mouseX < boxX + boxW && mouseY >= listY && mouseY < listY + listH;
                if (!insideList)
                    state.Open = false;
            }

            // Draw label
            surface.DrawText(cursorX, cursorY, label, theme.Text);

            // Draw box background
            Color boxBg = focused ? theme.InputBgActive : theme.InputBg;
            for (int y = 0; y < boxH; y++)
                for (int x = 0; x < boxW; x++)
                    surface.SetPixel(boxX + x, boxY + y, boxBg);
            surface.DrawOutlinedRect(boxX, boxY, boxW, boxH, focused ? theme.BorderFocus : theme.Border);

            // Selected text + arrow
            string selectedText = (selectedIndex >= 0 && selectedIndex < options.Length) ? options[selectedIndex] : "";
            int maxChars = (boxW - 14) / 7; // leave space for arrow
            if (selectedText.Length > maxChars && maxChars > 3)
                selectedText = selectedText.Substring(0, maxChars - 3) + "...";
            surface.DrawText(boxX + 3, boxY + 2, selectedText, theme.Text);
            surface.DrawText(boxX + boxW - 10, boxY + 2, state.Open ? "^" : "v", theme.Text);

            // Open list rendering (overlay; doesn't affect layout height)
            if (state.Open && options.Length > 0)
            {
                int listY = boxY + boxH;
                for (int i = 0; i < options.Length; i++)
                {
                    int optY = listY + i * itemHeight;
                    bool optHover = mouseX >= boxX && mouseX < boxX + boxW && mouseY >= optY && mouseY < optY + itemHeight;
                    // background row
                    Color rowBg = (i == selectedIndex) ? theme.Highlight : theme.InputBg;
                    if (optHover && i != selectedIndex)
                        rowBg = theme.ButtonHovered;
                    for (int y = 0; y < itemHeight; y++)
                        for (int x = 0; x < boxW; x++)
                            SetClippedPixel(surface, boxX + x, optY + y, rowBg);
                    for (int bx = 0; bx < boxW; bx++)
                    {
                        SetClippedPixel(surface, boxX + bx, optY, theme.Border);
                        SetClippedPixel(surface, boxX + bx, optY + itemHeight - 1, theme.Border);
                    }
                    for (int by = 0; by < itemHeight; by++)
                    {
                        SetClippedPixel(surface, boxX, optY + by, theme.Border);
                        SetClippedPixel(surface, boxX + boxW - 1, optY + by, theme.Border);
                    }
                    // text (truncate if needed)
                    string optText = options[i];
                    if (optText.Length > maxChars && maxChars > 3)
                        optText = optText.Substring(0, maxChars - 3) + "...";
                    surface.DrawText(boxX + 3, optY + 2, optText, theme.Text);
                    // Handle click on option
                    if (mousePressed && optHover)
                    {
                        if (i != selectedIndex) { selectedIndex = i; selectionChanged = true; }
                        state.Open = false;
                        RequestFocus(idx);
                    }
                }
                // Combined outline already drawn per row
            }

            lastItemWidth = labelWidth + boxW; lastItemHeight = boxH;
            NewLine();
            return selectionChanged;
        }

        public static bool Dropdown(Surface surface, string label, string[] options, ref int selectedIndex, int width)
            => Dropdown(surface, label, options, ref selectedIndex, width, currentMouseX, currentMouseY, currentMouseDown);

        // Basic text input. Very limited (ASCII) – expands when focused & handles typed chars from GuiInput.
        public static bool TextInput(Surface surface, string id, ref string text, int width, int mouseX, int mouseY, bool mouseDown)
        {
            int idx = NextWidgetIndex();
            lastItemStartY = cursorY;
            int h = 18;
            bool hovered = mouseX >= cursorX && mouseX < cursorX + width && mouseY >= cursorY && mouseY < cursorY + h;
            bool isFocused = IsFocused(idx);
            bool mousePressed = mouseDown && !prevMouseDown;
            bool activate = ActivationRequested(hovered, mousePressed, isFocused);
            if ((mousePressed && hovered) || activate)
                RequestFocus(idx);
            // background
            bool nowFocused = IsFocused(idx);
            Color bg = nowFocused ? theme.InputBgActive : theme.InputBg;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < width; x++)
                    surface.SetPixel(cursorX + x, cursorY + y, bg);
            surface.DrawOutlinedRect(cursorX, cursorY, width, h, nowFocused ? theme.BorderFocus : theme.Border);
            // handle typing
            if (nowFocused && currentInput.TypedChars != null)
            {
                foreach (var ch in currentInput.TypedChars)
                {
                    if (ch >= ' ' && ch <= '~') text += ch;
                }
            }
            if (nowFocused && currentInput.Backspace && text.Length > 0)
                text = text.Substring(0, text.Length - 1);
            string display = text;
            // clamp visible length
            int maxChars = (width - 6) / 7;
            if (display.Length > maxChars)
                display = display.Substring(display.Length - maxChars);
            surface.DrawText(cursorX + 3, cursorY + 2, display + (nowFocused ? "_" : string.Empty), theme.Text);
            lastItemWidth = width; lastItemHeight = h;
            NewLine();
            return nowFocused;
        }

        public static bool TextInput(Surface surface, string id, ref string text, int width)
            => TextInput(surface, id, ref text, width, currentMouseX, currentMouseY, currentMouseDown);

        public static void NextLine(int pixels = 0)
        {
            if (pixels > 0) cursorY += pixels; else NewLine();
        }

        // Scroll panel helpers (simple clipping region)
        public static void PushScrollPanel(int x, int y, int w, int h)
        {
            clipStack.Push(new ClipRect { X = x, Y = y, W = w, H = h });
            baseX = x + 4;
            cursorX = baseX;
            cursorY = y + 4;
        }

        public static void PopScrollPanel()
        {
            if (clipStack.Count > 0) clipStack.Pop();
        }

        // ------------------------- Tabs Control -------------------------
        private class TabsState { public int SelectedIndex; }
        private static readonly System.Collections.Generic.Dictionary<string, TabsState> tabsStates = new();

        // Draws a horizontal tab bar. Returns currently selected tab index.
        // labels: list of tab names. id: stable identifier for persistent selection.
        public static int Tabs(Surface surface, string id, string[] labels, int mouseX, int mouseY, bool mouseDown)
        {
            if (labels == null || labels.Length == 0) return -1;
            if (!tabsStates.TryGetValue(id, out var state))
                tabsStates[id] = state = new TabsState { SelectedIndex = 0 };

            int idx = NextWidgetIndex(); // treat entire tabs bar as one navigable widget
            lastItemStartY = cursorY;
            bool focused = IsFocused(idx);

            // Keyboard navigation (Left/Right when focused)
            if (focused)
            {
                if (keyLeftEdge) state.SelectedIndex = (state.SelectedIndex - 1 + labels.Length) % labels.Length;
                if (keyRightEdge) state.SelectedIndex = (state.SelectedIndex + 1) % labels.Length;
            }

            int xCursor = cursorX;
            int tabHeight = 22;
            int spacing = 4;
            int totalWidth = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                string t = labels[i];
                int textW = t.Length * 7;
                int tabW = textW + 20; // padding
                int tabX = xCursor;
                int tabY = cursorY;
                bool hovered = mouseX >= tabX && mouseX < tabX + tabW && mouseY >= tabY && mouseY < tabY + tabHeight;
                bool mousePressed = mouseDown && !prevMouseDown;
                if (hovered && mousePressed)
                {
                    state.SelectedIndex = i;
                    RequestFocus(idx);
                }
                bool selected = (i == state.SelectedIndex);
                // Colors
                Color bg = selected ? theme.ButtonActive : hovered ? theme.ButtonHovered : theme.Button;
                Color border = selected ? theme.BorderFocus : theme.Border;
                // Fill tab rectangle
                for (int py = 0; py < tabHeight; py++)
                    for (int px = 0; px < tabW; px++)
                        surface.SetPixel(tabX + px, tabY + py, bg);
                surface.DrawOutlinedRect(tabX, tabY, tabW, tabHeight, border);
                surface.DrawText(tabX + (tabW - textW) / 2, tabY + 5, t, theme.Text);
                // Active underline accent
                if (selected)
                {
                    for (int ux = 1; ux < tabW - 1; ux++)
                        surface.SetPixel(tabX + ux, tabY + tabHeight - 2, theme.Highlight);
                }
                xCursor += tabW + spacing;
                totalWidth += tabW + spacing;
            }
            if (totalWidth > 0) totalWidth -= spacing;
            lastItemWidth = totalWidth; lastItemHeight = tabHeight;
            // Advance layout to next line below tabs
            cursorY += tabHeight + 6;
            cursorX = baseX + indentLevel * IndentSize;
            return state.SelectedIndex;
        }

        public static int Tabs(Surface surface, string id, string[] labels)
            => Tabs(surface, id, labels, currentMouseX, currentMouseY, currentMouseDown);
    }
}
