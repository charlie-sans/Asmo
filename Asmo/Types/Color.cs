using System;
using System.Collections.Generic;
using System.Text;

namespace Asmo.Types
{
   public class Color
    {
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public int A { get; set; }
        public string Hex { get; set; }
        public Color(int r, int g, int b, int a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            Hex = $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
    }
}
