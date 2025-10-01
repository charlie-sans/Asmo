using System;
using System.IO;
using SharpMASM.MNI;

namespace SharpMASM.MNI.Modules
{
    [MNIClass("FileOperations")]
    public class FileOperationsModule
    {
        [MNIFunction(name: "readFile", module: "FileOperations")]
        public static void ReadFile(MNIMethodObject obj)
        {
            try
            {
                // Get filename from memory
                string filename = obj.readString(obj.arg1);
                Console.WriteLine(filename);
                Console.WriteLine(obj.arg1);
                Console.WriteLine(obj.arg2);
                // Destination memory address
                string destination = obj.arg2;
                
                // Read the file content
                string content = File.ReadAllText(filename);
                
                // Write to memory
                obj.writeString(destination, content);
                
                // Set success flag
                obj.setRegister("RFLAGS", 1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error reading file: {ex.Message}");
                obj.setRegister("RFLAGS", 0);
            }
        }
        
        [MNIFunction(name: "writeFile", module: "FileOperations")]
        public static void WriteFile(MNIMethodObject obj)
        {
            try
            {
                // Get filename from memory
                string filename = obj.readString(obj.arg1);
                
                // Get content to write
                string content = obj.readString(obj.arg2);
                
                // Write the content to file
                File.WriteAllText(filename, content);
                
                // Set success flag
                obj.setRegister("RFLAGS", 1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error writing file: {ex.Message}");
                obj.setRegister("RFLAGS", 0);
            }
        }
        
        // MNI function to write to a file with a start and end address in memory
        [MNIFunction(name: "writeMemFile", module: "FileOperations")]
        public static void WriteMemFile(MNIMethodObject obj)
        {
            try
            {
                // Get filename from memory
                string filename = obj.readString(obj.arg1);

                // Get start and end memory addresses
                string start = obj.arg2;
                string end = obj.arg3;

                // Parse and validate memory range
                long startAddress = obj.readInteger(start);
                long endAddress = obj.readInteger(end);
                if (startAddress > endAddress)
                {
                    throw new ArgumentException($"Invalid memory range: start ({startAddress}) is greater than end ({endAddress})");
                }

                // Read strings from memory
                string content = string.Empty;
                for (long address = startAddress; address <= endAddress; address += 8) // Assuming strings are stored in 8-byte blocks
                {
                    string data = obj.readString($"${address}");
                    if (!string.IsNullOrEmpty(data))
                    {
                        content += data;
                    }
                }

                // Debug: Log the final content
                Console.WriteLine($"Final content to write: {content}");

                // Write the content to the file
                File.WriteAllText(filename, content);

                // Set success flag
                obj.setRegister("RFLAGS", 1);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in WriteMemFile: {ex.Message}");
                obj.setRegister("RFLAGS", 0);
            }
        }
        [MNIFunction(name: "fileExists", module: "FileOperations")]
        public static void FileExists(MNIMethodObject obj)
        {
            try
            {
                // Get filename from memory
                string filename = obj.readString(obj.arg1);
                
                // Check if file exists
                bool exists = File.Exists(filename);
                
                // Set result flag
                obj.setRegister("RFLAGS", exists ? 1 : 0);
            }
            catch (Exception)
            {
                obj.setRegister("RFLAGS", 0);
            }
        }
    }
}
