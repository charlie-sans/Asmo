using System;
using System.Collections.Generic;
using Asmo.Gfx;

namespace Asmo.Gui
{
    /// <summary>
    /// Represents a rectangular UI zone for layout.
    /// </summary>
    public class Zone
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public Zone(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public (int x, int y) GetAnchor(ZoneAnchor anchor)
        {
            return anchor switch
            {
                ZoneAnchor.TopLeft => (X, Y),
                ZoneAnchor.TopCenter => (X + Width / 2, Y),
                ZoneAnchor.TopRight => (X + Width, Y),
                ZoneAnchor.CenterLeft => (X, Y + Height / 2),
                ZoneAnchor.Center => (X + Width / 2, Y + Height / 2),
                ZoneAnchor.CenterRight => (X + Width, Y + Height / 2),
                ZoneAnchor.BottomLeft => (X, Y + Height),
                ZoneAnchor.BottomCenter => (X + Width / 2, Y + Height),
                ZoneAnchor.BottomRight => (X + Width, Y + Height),
                _ => (X, Y)
            };
        }
    }

    public enum ZoneAnchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        CenterLeft,
        Center,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    /// <summary>
    /// Manages named layout zones for a UI surface.
    /// </summary>
    public class ZoneManager
    {
        private readonly Dictionary<string, Zone> _zones = new();
        private readonly Surface _surface;

        public ZoneManager(Surface surface)
        {
            _surface = surface;
            // Default zones (can be customized)
            int w = surface.Width, h = surface.Height;
            _zones["TopLeft"] = new Zone(0, 0, w / 3, h / 3);
            _zones["TopCenter"] = new Zone(w / 3, 0, w / 3, h / 3);
            _zones["TopRight"] = new Zone(2 * w / 3, 0, w / 3, h / 3);
            _zones["CenterLeft"] = new Zone(0, h / 3, w / 3, h / 3);
            _zones["Center"] = new Zone(w / 3, h / 3, w / 3, h / 3);
            _zones["CenterRight"] = new Zone(2 * w / 3, h / 3, w / 3, h / 3);
            _zones["BottomLeft"] = new Zone(0, 2 * h / 3, w / 3, h / 3);
            _zones["BottomCenter"] = new Zone(w / 3, 2 * h / 3, w / 3, h / 3);
            _zones["BottomRight"] = new Zone(2 * w / 3, 2 * h / 3, w / 3, h / 3);
        }

        public Zone GetZone(string name)
        {
            return _zones.TryGetValue(name, out var zone) ? zone : null;
        }

        public void SetZone(string name, Zone zone)
        {
            _zones[name] = zone;
        }

        public IEnumerable<string> ZoneNames => _zones.Keys;
    }
}
