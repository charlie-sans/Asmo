using System;
using SharpMASM.MNI;

namespace SharpMASM.MNI.Modules
{
    [MNIClass("IO")]
    public class IOModule
    {
        [MNIFunction(name: "write", module: "IO")]
        public static void Write(MNIMethodObject obj)
        {
            int target = obj.readInteger(obj.arg1);
            string content = obj.readString(obj.arg2);
            
            switch (target)
            {
                case 1: // stdout
                    Console.Write(content);
                    break;
                case 2: // stderr
                    Console.Error.Write(content);
                    break;
                default:
                    throw new ArgumentException($"Invalid target: {target}. Must be 1 (stdout) or 2 (stderr)");
            }
        }
        
        [MNIFunction(name: "flush", module: "IO")]
        public static void Flush(MNIMethodObject obj)
        {
            int target = obj.readInteger(obj.arg1);
            
            switch (target)
            {
                case 1: // stdout
                    Console.Out.Flush();
                    break;
                case 2: // stderr
                    Console.Error.Flush();
                    break;
                default:
                    throw new ArgumentException($"Invalid target: {target}. Must be 1 (stdout) or 2 (stderr)");
            }
        }
    }
}
