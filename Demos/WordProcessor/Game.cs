using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
using System.Collections.Generic;
using System.Text;

namespace WordProcessorDemo
{
    // Minimal word processor demo for Asmo UI showcase
    public class Game : IConsoleGame
    {
    private Keyboard? kb;
    private StringBuilder textBuffer = new StringBuilder();
    private int cursorIndex = 0;
    private int scrollOffset = 0;
    private int selectionStart = -1; // -1 means no selection
    private bool bold = false, italic = false, underline = false;
    private string status = "Ready";
    // For multi-line navigation
    private int desiredCursorX = -1;

        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
            textBuffer.Append("Welcome to the Asmo Word Processor demo!\nType here...");
            cursorIndex = textBuffer.Length;
        }

        public void Update(double deltaTime)
        {
            if (kb == null) return;
            // Multi-line navigation helpers
            string[] lines = textBuffer.ToString().Split('\n');
            int lineIdx = 0, colIdx = 0, charCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (cursorIndex <= charCount + lines[i].Length)
                {
                    lineIdx = i;
                    colIdx = cursorIndex - charCount;
                    break;
                }
                charCount += lines[i].Length + 1; // +1 for \n
            }

            // Basic navigation
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left) && cursorIndex > 0)
            {
                cursorIndex--;
                desiredCursorX = -1;
            }
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right) && cursorIndex < textBuffer.Length)
            {
                cursorIndex++;
                desiredCursorX = -1;
            }

            // Up arrow: move cursor to previous line, same X if possible
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up) && lineIdx > 0)
            {
                int prevLineLen = lines[lineIdx - 1].Length;
                if (desiredCursorX == -1) desiredCursorX = colIdx;
                int newCol = Math.Min(prevLineLen, desiredCursorX);
                // Find new cursorIndex
                int newIdx = 0;
                for (int i = 0; i < lineIdx - 1; i++)
                    newIdx += lines[i].Length + 1;
                newIdx += newCol;
                cursorIndex = newIdx;
            }
            // Down arrow: move cursor to next line, same X if possible
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down) && lineIdx < lines.Length - 1)
            {
                int nextLineLen = lines[lineIdx + 1].Length;
                if (desiredCursorX == -1) desiredCursorX = colIdx;
                int newCol = Math.Min(nextLineLen, desiredCursorX);
                // Find new cursorIndex
                int newIdx = 0;
                for (int i = 0; i < lineIdx + 1; i++)
                    newIdx += lines[i].Length + 1;
                newIdx += newCol;
                cursorIndex = newIdx;
            }
            // Home: move to start of line
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Home))
            {
                int newIdx = 0;
                for (int i = 0; i < lineIdx; i++)
                    newIdx += lines[i].Length + 1;
                cursorIndex = newIdx;
                desiredCursorX = 0;
            }
            // End: move to end of line
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.End))
            {
                int newIdx = 0;
                for (int i = 0; i < lineIdx; i++)
                    newIdx += lines[i].Length + 1;
                cursorIndex = newIdx + lines[lineIdx].Length;
                desiredCursorX = lines[lineIdx].Length;
            }
            // Reset desiredCursorX if not moving vertically
            if (!kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up) && !kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down))
                desiredCursorX = -1;

            // Simple typing (A-Z, 0-9, space, backspace)
            var pressedChars = new List<char>(kb.GetPressedChars());
            for (int i = 0; i < pressedChars.Count; i++)
            {
                char c = pressedChars[i];
                if (c != '\r' && c != '\n')
                {
                    textBuffer.Insert(cursorIndex, c);
                    cursorIndex++;
                }
            }
            // Insert new line if Enter key is pressed
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
            {
                textBuffer.Insert(cursorIndex, '\n');
                cursorIndex++;
            }
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace) && cursorIndex > 0)
            {
                textBuffer.Remove(cursorIndex - 1, 1);
                cursorIndex--;
            }
            // clear the pressed chars buffer
            kb.Update();
            // Toolbar toggles
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.B)) bold = !bold;
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.I)) italic = !italic;
            if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.U)) underline = !underline;
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);
            // Toolbar
            surface.DrawRect(0, 0, surface.Width, 24, Colors.DarkGray);
            surface.DrawText(8, 4, $"[N] New  [O] Open  [S] Save  [B] Bold({(bold ? "ON" : "off")})  [I] Italic({(italic ? "ON" : "off")})  [U] Underline({(underline ? "ON" : "off")})", Colors.White);
            // Text area
            int textY = 32;
            string[] lines = textBuffer.ToString().Split('\n');
            int charCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                surface.DrawText(8, textY + i * 18, lines[i], Colors.White);
                // Draw cursor if on this line
                if (cursorIndex >= charCount && cursorIndex <= charCount + lines[i].Length)
                {
                    // Use 6 pixels per character to match font width
                    int charsBeforeCursor = Math.Max(0, cursorIndex - charCount);
                    int cx = 8 + charsBeforeCursor * 6;
                    surface.DrawRect(cx, textY + i * 18, 2, 16, Colors.Yellow);
                }
                charCount += lines[i].Length + 1; // +1 for \n
            }
            // Status bar
            surface.DrawRect(0, surface.Height - 20, surface.Width, 20, Colors.DarkGray);
            surface.DrawText(8, surface.Height - 16, $"Words: {WordCount()}  Chars: {textBuffer.Length}  Status: {status}", Colors.White);
        }

        private int WordCount()
        {
            var txt = textBuffer.ToString();
            if (string.IsNullOrWhiteSpace(txt)) return 0;
            return txt.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}
