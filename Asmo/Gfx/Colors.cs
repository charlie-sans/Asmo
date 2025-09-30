using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Asmo.Types;

namespace Asmo.Gfx
{
    public static class Colors
    {
        public static readonly Color Black = new Color(0, 0, 0, 255);
        public static readonly Color White = new Color(255, 255, 255, 255);
        public static readonly Color Red = new Color(255, 0, 0, 255);
        public static readonly Color Green = new Color(0, 255, 0, 255);
        public static readonly Color Blue = new Color(0, 0, 255, 255);
        public static readonly Color Transparent = new Color(0, 0, 0, 0);
        public static readonly Color Yellow = new Color(255, 255, 0, 255);
        public static readonly Color Cyan = new Color(0, 255, 255, 255);
        public static readonly Color Magenta = new Color(255, 0, 255, 255);
        public static readonly Color Gray = new Color(128, 128, 128, 255);
        public static readonly Color Orange = new Color(255, 165, 0, 255);
        public static readonly Color Purple = new Color(128, 0, 128, 255);

        // dark variants
        public static readonly Color DarkBlue = new Color(0, 0, 139, 255);
        public static readonly Color DarkRed = new Color(139, 0, 0, 255);
        public static readonly Color DarkGreen = new Color(0, 100, 0, 255);
        public static readonly Color DarkGray = new Color(64, 64, 64, 255);
        public static readonly Color DarkCyan = new Color(0, 139, 139, 255);
        public static readonly Color DarkMagenta = new Color(139, 0, 139, 255);
        public static readonly Color DarkYellow = new Color(139, 139, 0, 255);
        public static readonly Color DarkOrange = new Color(255, 140, 0, 255);
        public static readonly Color DarkPurple = new Color(85, 0, 85, 255);
        internal static Color Lime = new Color(0, 255, 0, 255);
    }
}
