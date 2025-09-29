using System;
using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Scenes.Transitions
{
    /// <summary>
    /// Simple fade transition that overlays a solid color while lerping towards the target scene.
    /// </summary>
    public class FadeTransition : ISceneTransition
    {
        private readonly Color _fadeColor;
        private readonly double _duration;
        private readonly bool _bidirectional;
        private double _elapsed;

        public FadeTransition(Color? fadeColor = null, double duration = 0.5, bool bidirectional = true)
        {
            if (duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");

            _fadeColor = fadeColor ?? Colors.Black;
            _duration = duration;
            _bidirectional = bidirectional;
        }

        public void Begin(SceneTransitionContext context)
        {
            _elapsed = 0;
        }

        public bool Update(SceneTransitionContext context)
        {
            _elapsed = Math.Min(_duration, _elapsed + context.DeltaTime);
            return _elapsed >= _duration;
        }

        public void Draw(SceneTransitionContext context, Surface surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));

            if (_elapsed <= 0)
                return;

            var progress = Math.Clamp(_elapsed / _duration, 0, 1);
            float strength = _bidirectional
                ? (float)(progress <= 0.5 ? progress * 2 : (1 - progress) * 2)
                : (float)progress;

            if (strength <= 0)
                return;

            ApplyFade(surface, strength);
        }

        public void Complete(SceneTransitionContext context)
        {
            // no-op, transitions can override for cleanup if needed
        }

        private void ApplyFade(Surface surface, float strength)
        {
            for (int x = 0; x < surface.Width; x++)
            {
                var column = surface.Pixels[x];
                for (int y = 0; y < surface.Height; y++)
                {
                    var src = column[y];
                    var r = (int)(src.R * (1 - strength) + _fadeColor.R * strength);
                    var g = (int)(src.G * (1 - strength) + _fadeColor.G * strength);
                    var b = (int)(src.B * (1 - strength) + _fadeColor.B * strength);
                    var a = (int)(src.A * (1 - strength) + _fadeColor.A * strength);
                    column[y] = new Color(ClampToByte(r), ClampToByte(g), ClampToByte(b), ClampToByte(a));
                }
            }
        }

        private static int ClampToByte(int value) => Math.Clamp(value, 0, 255);
    }
}
