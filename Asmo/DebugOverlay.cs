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
            var arr = _frameTimes.ToArray();
            double maxObserved = arr.Max();
            // Allow dynamic scaling if all frames are well below 16.7ms ( >60fps ), but keep a minimum for readability
            double maxMs = maxObserved < 16.7 ? Math.Max(maxObserved * 1.25, 5.0) : Math.Max(33.3, maxObserved);
            int n = Math.Min(width, arr.Length);
            for (int i = 0; i < n; i++) {
                int idx = (arr.Length + _frameTimes.Count - n + i) % arr.Length;
                double ms = arr[idx];
                int barH = (int)Math.Min((ms / maxMs) * (height - 4), height - 4);
                int barY = y + (height - 4 - barH) + 2;
                int barX = x + i;
                var color = ms < 8.0 ? Asmo.Gfx.Colors.Cyan : (ms < 16.7 ? Asmo.Gfx.Colors.Green : (ms < 25 ? Asmo.Gfx.Colors.Yellow : Asmo.Gfx.Colors.Red));
                surface.DrawRect(barX, barY, 1, barH, color);
            }
            surface.DrawRect(x, y + height - 2, width, 1, Asmo.Gfx.Colors.Gray);
            surface.DrawText(x + 4, y + 4, $"Frame ms (max:{maxMs:F1})", Asmo.Gfx.Colors.White);
            surface.DrawText(x + 4, y + 16, $"8.3ms (~120fps)", Asmo.Gfx.Colors.Cyan);
            surface.DrawText(x + 4, y + 28, $"16.7ms (60fps)", Asmo.Gfx.Colors.Green);
        }

        public void DrawFpsGraph(Asmo.Gfx.Surface surface, int x, int y, int width, int height) {
            if (_frameTimes.Count == 0) return;
            var arr = _frameTimes.ToArray();
            double maxFpsObserved = arr.Max(ms => ms > 0.01 ? 1000.0 / ms : 1000.0);
            // Choose an upper bucket just above observed value
            double[] buckets = { 30, 60, 90, 120, 144, 165, 180, 200, 240, 300, 360, 480 }; // extendable
            double maxFps = buckets.First(b => b >= maxFpsObserved * 1.05);
            int n = Math.Min(width, arr.Length);
            for (int i = 0; i < n; i++) {
                int idx = (arr.Length + _frameTimes.Count - n + i) % arr.Length;
                double ms = arr[idx];
                double fps = ms > 0.01 ? 1000.0 / ms : maxFps;
                int barH = (int)Math.Min((fps / maxFps) * (height - 4), height - 4);
                int barY = y + (height - 4 - barH) + 2;
                int barX = x + i;
                var color = fps >= 120 ? Asmo.Gfx.Colors.Cyan : (fps >= 60 ? Asmo.Gfx.Colors.Green : (fps >= 30 ? Asmo.Gfx.Colors.Yellow : Asmo.Gfx.Colors.Red));
                surface.DrawRect(barX, barY, 1, barH, color);
            }
            surface.DrawRect(x, y + height - 2, width, 1, Asmo.Gfx.Colors.Gray);
            surface.DrawText(x + 4, y + 4, $"FPS (≤{maxFps:F0})", Asmo.Gfx.Colors.White);
            surface.DrawText(x + 4, y + 16, $"60", Asmo.Gfx.Colors.Green);
            if (maxFps >= 120) surface.DrawText(x + 40, y + 16, $"120", Asmo.Gfx.Colors.Cyan);
            surface.DrawText(x + 4, y + 28, $"30", Asmo.Gfx.Colors.Yellow);
        }

        // --- Custom FrameGraph support ---
        private readonly List<Asmo.Debug.FrameGraph> _customGraphs = new();
        /// <summary>
        /// Register a custom FrameGraph to be rendered in the overlay.
        /// </summary>
        public void RegisterFrameGraph(Asmo.Debug.FrameGraph graph) => _customGraphs.Add(graph);
        /// <summary>
        /// Remove a custom FrameGraph from the overlay.
        /// </summary>
        public void UnregisterFrameGraph(Asmo.Debug.FrameGraph graph) => _customGraphs.Remove(graph);

        private int _renderCount = 0;
        private long _lastTouchedPixels = 0;
        private long _lastTotalPixels = 0;
        public void UpdatePixelStats(long touched, long total)
        {
            _lastTouchedPixels = touched;
            _lastTotalPixels = total;
        }
        public void Render(Asmo.Gfx.Surface surface)
        {
            // Console.WriteLine($"[DebugOverlay] Render called. Visible={_visible}");
            if (!_visible) return;
            _renderCount++;
            _sb.Clear();
            _sb.AppendLine($"FPS: {_lastFps:F1} (1% low: {_last1PercentLow:F1})");
            _sb.AppendLine($"Draw Calls: {_drawCallCount}");
            _sb.AppendLine($"Memory: {_lastMemory / 1024 / 1024} MB");
            _sb.AppendLine($"Overlay Render Count: {_renderCount}");
            if (_lastTotalPixels > 0)
            {
                double pct = (_lastTouchedPixels / (double)_lastTotalPixels) * 100.0;
                _sb.AppendLine($"Dirty Pixels: {_lastTouchedPixels}/{_lastTotalPixels} ({pct:F1}%)");
            }
            // Input state panel
            if (_keyboard != null)
            {
                // _sb.AppendLine("Keys: " + string.Join(", ", _keyboard.GetPressedKeys()));
            }
            surface.DrawText(8, 8, _sb.ToString(), Asmo.Gfx.Colors.White);
            // Draw a visible watermark in the corner
            surface.DrawText(surface.Width - 180, 8, "DEBUG OVERLAY ACTIVE", Asmo.Gfx.Colors.Magenta);
            // Draw frame time and FPS graphs below the text
            int graphX = 8;
            int graphY = 64;
            int graphWidth = 120;
            int graphHeight = 40;
            DrawFrameTimeGraph(surface, graphX, graphY, graphWidth, graphHeight);
            DrawFpsGraph(surface, graphX, graphY + graphHeight + 8, graphWidth, graphHeight);

            // Draw custom user graphs (stacked vertically)
            int customY = graphY + 2 * (graphHeight + 8) + 8;
            foreach (var graph in _customGraphs)
            {
                graph.Draw(surface, graphX, customY, graphWidth, graphHeight);
                customY += graphHeight + 8;
            }
            // Console.WriteLine($"[DebugOverlay] Rendered overlay frame {_renderCount}");
        }
    }
}
