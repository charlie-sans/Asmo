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

        public void Render(Surface surface)
        {
            if (!_visible) return;
            _sb.Clear();
            _sb.AppendLine($"FPS: {_lastFps:F1} (1% low: {_last1PercentLow:F1})");
            _sb.AppendLine($"Draw Calls: {_drawCallCount}");
            _sb.AppendLine($"Memory: {_lastMemory / 1024 / 1024} MB");

            // Input state panel
            if (_keyboard != null)
            {
                var pressed = typeof(OpenTK.Windowing.GraphicsLibraryFramework.Keys)
                    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                    .Select(f => (OpenTK.Windowing.GraphicsLibraryFramework.Keys)f.GetValue(null)!)
                    .Where(k => _keyboard.IsKeyDown(k))
                    .ToArray();
                if (pressed.Length > 0)
                {
                    _sb.Append("Keys: ");
                    _sb.AppendLine(string.Join(", ", pressed));
                }
                else
                {
                    _sb.AppendLine("Keys: (none)");
                }
            }

            // TODO: Add audio, scene, ECS stats
            surface.DrawText(8, 8, _sb.ToString(), Colors.White);
        }
    }
}
