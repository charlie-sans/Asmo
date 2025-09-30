using System;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Asmo.Gfx;
using Asmo.Gui;
using Asmo.IO;
using System.Collections.Generic;

namespace Asmo
{
    public class DebugOverlay
    {
        public static DebugOverlay? Current { get; private set; }
        private bool _visible = false;
        private Stopwatch _frameTimer = new Stopwatch();
        private Queue<double> _frameTimes = new Queue<double>();
        private const int MaxSamples = 120;
        private double _lastFps = 0;
        private double _last1PercentLow = 0;
        private int _drawCallCount = 0;
        private long _lastMemory = 0;
        private StringBuilder _sb = new StringBuilder();
        private Asmo.Window.input.Keyboard? _keyboard;
        
        public void SetKeyboard(Asmo.Window.input.Keyboard keyboard)
        {
            _keyboard = keyboard;
        }

        public DebugOverlay()
        {
            Current = this;
        }

        public void Toggle() => _visible = !_visible;
    public void Show() => _visible = true;
        public bool IsVisible => _visible;

        public void BeginFrame()
        {
            _frameTimer.Restart();
            _drawCallCount = 0;
        }

        public void EndFrame()
        {
            _frameTimer.Stop();
            double ms = _frameTimer.Elapsed.TotalMilliseconds;
            _frameTimes.Enqueue(ms);
            if (_frameTimes.Count > MaxSamples)
                _frameTimes.Dequeue();
            CalculateFps();
            _lastMemory = GC.GetTotalMemory(false);
        }

        private void CalculateFps()
        {
            if (_frameTimes.Count == 0) return;
            double avg = 0;
            var arr = _frameTimes.ToArray();
            foreach (var ms in arr) avg += ms;
            avg /= arr.Length;
            _lastFps = 1000.0 / avg;
            Array.Sort(arr);
            int idx = (int)(arr.Length * 0.01);
            _last1PercentLow = 1000.0 / arr[Math.Min(idx, arr.Length - 1)];
        }

        public void RegisterDrawCall() => _drawCallCount++;

        public double GetAverageFrameTime() {
            if (_frameTimes.Count == 0) return 0;
            return _frameTimes.Average();
        }
        public double GetMinFrameTime() {
            if (_frameTimes.Count == 0) return 0;
            return _frameTimes.Min();
        }
        public double GetMaxFrameTime() {
            if (_frameTimes.Count == 0) return 0;
            return _frameTimes.Max();
        }

        public void DrawFrameTimeGraph(Asmo.Gfx.Surface surface, int x, int y, int width, int height) {
            if (_frameTimes.Count == 0) return;
            double maxMs = Math.Max(33.3, GetMaxFrameTime());
            var arr = _frameTimes.ToArray();
            int n = Math.Min(width, arr.Length);
            for (int i = 0; i < n; i++) {
                int idx = (arr.Length + _frameTimes.Count - n + i) % arr.Length;
                double ms = arr[idx];
                int barH = (int)Math.Min((ms / maxMs) * (height - 4), height - 4);
                int barY = y + (height - 4 - barH) + 2;
                int barX = x + i;
                var color = ms < 16.7 ? Asmo.Gfx.Colors.Green : (ms < 25 ? Asmo.Gfx.Colors.Yellow : Asmo.Gfx.Colors.Red);
                surface.DrawRect(barX, barY, 1, barH, color);
            }
            surface.DrawRect(x, y + height - 2, width, 1, Asmo.Gfx.Colors.Gray);
            surface.DrawText(x + 4, y + 4, $"Frame ms", Asmo.Gfx.Colors.White);
            surface.DrawText(x + 4, y + 16, $"16.7ms (60fps)", Asmo.Gfx.Colors.Green);
            surface.DrawText(x + 4, y + 28, $"33.3ms (30fps)", Asmo.Gfx.Colors.Red);
        }

        public void DrawFpsGraph(Asmo.Gfx.Surface surface, int x, int y, int width, int height) {
            if (_frameTimes.Count == 0) return;
            double maxFps = 120.0;
            var arr = _frameTimes.ToArray();
            int n = Math.Min(width, arr.Length);
            for (int i = 0; i < n; i++) {
                int idx = (arr.Length + _frameTimes.Count - n + i) % arr.Length;
                double ms = arr[idx];
                double fps = ms > 0.01 ? 1000.0 / ms : maxFps;
                int barH = (int)Math.Min((fps / maxFps) * (height - 4), height - 4);
                int barY = y + (height - 4 - barH) + 2;
                int barX = x + i;
                var color = fps > 60 ? Asmo.Gfx.Colors.Green : (fps > 30 ? Asmo.Gfx.Colors.Yellow : Asmo.Gfx.Colors.Red);
                surface.DrawRect(barX, barY, 1, barH, color);
            }
            surface.DrawRect(x, y + height - 2, width, 1, Asmo.Gfx.Colors.Gray);
            surface.DrawText(x + 4, y + 4, $"FPS", Asmo.Gfx.Colors.White);
            surface.DrawText(x + 4, y + 16, $"60 FPS", Asmo.Gfx.Colors.Green);
            surface.DrawText(x + 4, y + 28, $"30 FPS", Asmo.Gfx.Colors.Red);
        }

        public void Render(Asmo.Gfx.Surface surface)
        {
            if (!_visible) return;
            _sb.Clear();
            _sb.AppendLine($"FPS: {_lastFps:F1} (1% low: {_last1PercentLow:F1})");
            _sb.AppendLine($"Draw Calls: {_drawCallCount}");
            _sb.AppendLine($"Memory: {_lastMemory / 1024 / 1024} MB");
            // Input state panel
            if (_keyboard != null)
            {
                // _sb.AppendLine("Keys: " + string.Join(", ", _keyboard.GetPressedKeys()));
            }
            surface.DrawText(8, 8, _sb.ToString(), Asmo.Gfx.Colors.White);
            // Draw frame time and FPS graphs below the text
            int graphX = 8;
            int graphY = 64;
            int graphWidth = 120;
            int graphHeight = 40;
            DrawFrameTimeGraph(surface, graphX, graphY, graphWidth, graphHeight);
            DrawFpsGraph(surface, graphX, graphY + graphHeight + 8, graphWidth, graphHeight);
        }
    }
}
