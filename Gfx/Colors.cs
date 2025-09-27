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
 
        // DColors: Default palette
        public static readonly Color[] DColors = new Color[]
        {
            Black, White, Red, Green, Blue,
            
        };
    }
}
