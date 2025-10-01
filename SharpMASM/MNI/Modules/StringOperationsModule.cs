using System;
using SharpMASM.MNI;

namespace SharpMASM.MNI.Modules
{
    [MNIClass("StringOperations")]
    public class StringOperationsModule
    {
        [MNIFunction(name: "cmp", module: "StringOperations")]
        public static void Cmp(MNIMethodObject obj)
        {
            string str1 = obj.readString(obj.arg1);
            string str2 = obj.readString(obj.arg2);
            
            if (str1.Equals(str2))
            {
                obj.setRegister("RFLAGS", 1);
            }
            else
            {
                obj.setRegister("RFLAGS", 0);
            }
        }
        
        [MNIFunction(name: "concat", module: "StringOperations")]
        public static void Concat(MNIMethodObject obj)
        {
            string str1 = obj.readString(obj.arg1);
            string str2 = obj.readString(obj.arg2);
            string result = str1 + str2;
            
            obj.writeString(obj.arg3, result);
        }
        
        [MNIFunction(name: "length", module: "StringOperations")]
        public static void Length(MNIMethodObject obj)
        {
            string str = obj.readString(obj.arg1);
            int length = str.Length;
            
            // Store in register specified in arg2
            obj.setRegister(obj.arg2, length);
        }
        
        [MNIFunction(name: "split", module: "StringOperations")]
        public static void Split(MNIMethodObject obj)
        {
            string str = obj.readString(obj.arg1);
            string delimiter = obj.readString(obj.arg2);
            string destination = obj.arg3;
            
            // Remove $ if present in destination
            if (destination.StartsWith("$"))
            {
                destination = destination.Substring(1);
            }
            
            if (!long.TryParse(destination, out long destAddress))
            {
                throw new ArgumentException($"Invalid destination address: {destination}");
            }
            
            string[] parts = str.Split(new[] { delimiter }, StringSplitOptions.None);
            
            // Store the count at the first position
            MappedMemoryFile.WriteMemory(destAddress, parts.Length);
            
            // Store each part sequentially in memory
            long currentAddress = destAddress + 8; // Move past the count
            foreach (string part in parts)
            {
                var memory = MappedMemoryFile.GetInstance(Common.MappedFile);
                memory.Write_String(currentAddress.ToString(), part);
                currentAddress += part.Length + 16; // Move to next slot with padding
            }
        }
        
        [MNIFunction(name: "replace", module: "StringOperations")]
        public static void Replace(MNIMethodObject obj)
        {
            string source = obj.readString(obj.arg1);
            string oldValue = obj.readString(obj.arg2);
            string newValue = obj.readString(obj.arg3);
            
            string result = source.Replace(oldValue, newValue);
            obj.writeString(obj.arg4, result);
        }
    }
}
