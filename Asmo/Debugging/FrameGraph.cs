using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Debug
{
    /// <summary>
    /// Buffers and renders a simple framegraph (or metric graph) onto a Surface for debugging.
    /// </summary>
    public class FrameGraph
    {
        private readonly float[] _samples;
        private int _index = 0;
        private int _count = 0;
        private readonly int _capacity;
        private float _min = float.MaxValue, _max = float.MinValue;
        public string Label { get; set; } = "FrameGraph";
        public Color GraphColor { get; set; } = Colors.Cyan;
        public Color BgColor { get; set; } = new Color(16, 16, 32, 180);

        public FrameGraph(int capacity = 120)
        {
            _capacity = capacity;
            _samples = new float[capacity];
        }

        public void AddSample(float value)
        {
            _samples[_index] = value;
            _index = (_index + 1) % _capacity;
            if (_count < _capacity) _count++;
            if (value < _min) _min = value;
            if (value > _max) _max = value;
        }

        public void Draw(Surface surface, int x, int y, int width = 120, int height = 40)
        {
            // Draw background
            surface.DrawRect(x, y, width, height, BgColor);
            if (_count == 0) return;
            float min = _min, max = _max;
            if (max - min < 1e-3f) max = min + 1f;
            int prevX = -1, prevY = -1;
            for (int i = 0; i < _count; i++)
            {
                int idx = (_index + i) % _capacity;
                float v = _samples[idx];
                int gx = x + i * width / _capacity;
                int gy = y + height - (int)(((v - min) / (max - min)) * (height - 2)) - 1;
                if (prevX >= 0 && prevY >= 0)
                    surface.DrawLine(prevX, prevY, gx, gy, GraphColor);
                prevX = gx; prevY = gy;
            }
            // Draw label and min/max
            surface.DrawText(x + 2, y + 2, $"{Label} [{min:F1}-{max:F1}]", Colors.White);
        }
    }
}
