using System;
using SharpMASM.MNI;

namespace SharpMASM.MNI.Modules
{
    [MNIClass("Graphics")]
    public class GraphicsModule
    {
        private static MappedMemoryFile _memory;
        private static long _framebufferAddress;
        private static int _width;
        private static int _height;

        public static void Initialize(MappedMemoryFile memory, long framebufferAddress, int width, int height)
        {
            _memory = memory;
            _framebufferAddress = framebufferAddress;
            _width = width;
            _height = height;
        }

        [MNIFunction("setPixel", "Graphics")]
        public static void SetPixel(MNIMethodObject methodObject)
        {
            // args: x, y, color
            if (int.TryParse(methodObject.arg1, out int x) &&
                int.TryParse(methodObject.arg2, out int y) &&
                int.TryParse(methodObject.arg3, out int color))
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    long address = _framebufferAddress + (y * _width + x) * 4; // assuming 4 bytes per pixel
                    _memory.Write(address.ToString(), color);
                }
            }
        }

        [MNIFunction("getPixel", "Graphics")]
        public static string GetPixel(MNIMethodObject methodObject)
        {
            // args: x, y
            if (int.TryParse(methodObject.arg1, out int x) &&
                int.TryParse(methodObject.arg2, out int y))
            {
                if (x >= 0 && x < _width && y >= 0 && y < _height)
                {
                    long address = _framebufferAddress + (y * _width + x) * 4;
                    return _memory.Read(address.ToString()).ToString();
                }
            }
            return "0";
        }

        [MNIFunction("clear", "Graphics")]
        public static void Clear(MNIMethodObject methodObject)
        {
            // arg: color
            if (int.TryParse(methodObject.arg1, out int color))
            {
                for (int i = 0; i < _width * _height; i++)
                {
                    long address = _framebufferAddress + i * 4;
                    _memory.Write(address.ToString(), color);
                }
            }
        }
    }
}