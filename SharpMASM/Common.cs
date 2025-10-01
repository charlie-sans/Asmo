/*
 * Common.cs
 * 
 * Description: Common class for the SharpMASM project.
 *
 * (C) Finite, 2025
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using static SharpMASM.Core_Interpreter;


namespace SharpMASM
{
   

    public class instruction
    {
        public string name;
        public string[] args;
        public instruction(string name, string[] args)
        {
            this.name = name;
            this.args = args;
        }
    }

    

    public class Common
    {
        public static string Version = "1.0.0";
        public static string Author = "Finite";
        // Increased to 256 MiB to support large framebuffer
        public static ulong MemorySize = 256 * 1024 * 1024;
        public static ulong MemoryUsed = 0;
        public static ulong MemoryFree = 0;
        public static bool IsMemoryFull = false;
        public static bool exitOnHLT = true;

        public static string[] Registers = new string[] { "RAX", "RBX", "RCX", "RDX", "RBP", "RSP", "RDI", "RSI","RFLAGS", "R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15" };
        public static string[] Instructions = new string[] { 
            "MOV", "ADD", "SUB", "MUL", "DIV", "JMP", "JZ", "JNZ", "JG", "JL", "JGE", "JLE","JNE",
            "CMP", "CALL", "RET", "PUSH", "POP", "AND", "OR", "XOR", "NOT", "SHL", "SHR",
            "INC", "DEC", "NEG", "NOP", "HLT", "INT", "IRET", "DB", "DW", "DD", "OUT", "MNI"
        };
        public static string MappedFile = "FiniteSharpMasmMemory";
        public static IMemoryManager Memory;
        public static Dictionary<string, long> Labels = new Dictionary<string, long>();
        public static List<instruction> instructions = new List<instruction>();
        public static Instructions? InstructionInstance { get; set; }
        

        public static Common? Instance { get; set; }

        public static void AddInstruction(string name, string[] args)
        {
            instructions.Add(new instruction(name, args));
        }

        public static IMemoryManager InitializeMemory()
        {
            Console.WriteLine($"Memory initialization: UseBuiltInArrays flag is set to: {CmdArgs.GetInstance().UseBuiltInArrays}");
            // because this is runnning in a different process, we want to use arrays always
            // if (CmdArgs.GetInstance().UseBuiltInArrays)
            // {
                if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
                {
                    Console.WriteLine("Using array-based memory implementation");
                }
                return ArrayMemoryManager.GetInstance(null); // Pass null instead of MappedFile
            // }
            // else
            // {
                // if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
                // {
                //     Console.WriteLine("Using memory-mapped file implementation");
                // }
                // return MappedMemoryFile.GetInstance(MappedFile);
            // }
        }

        /// <summary>
        /// VerbosePrint
        /// </summary>
        /// <param name="message"></param>
        public static void VerbosePrint(string message)
        {
            if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine(message);
            }
        }


        public static void box(string title, string message, string type)
        {
            string color;
            bool isError = false;
            switch (type.ToLower())
            {
                case "error":
                    isError = true;
                    color = "\u001B[31m"; // Red
                    break;
                case "info":
                default:
                    color = "\u001B[34m"; // Blue
                    break;
            }

            string reset = "\u001B[0m";
            string[] lines = message.Split('\n');
            if (isError)
            {
                title = "Error: " + title;
                int maxLength = title.Length;
                foreach (string line in lines)
                {
                    if (line.Length > maxLength)
                    {
                        maxLength = line.Length;
                    }
                }

                string border = "+" + new string('-', maxLength + 2) + "+";
                Console.WriteLine(color + border);
                Console.WriteLine("| " + title + new string(' ', maxLength - title.Length) + " |");
                Console.WriteLine(border);

                foreach (string line in lines)
                {
                    Console.WriteLine("| " + line + new string(' ', maxLength - line.Length) + " |");
                }

                Console.WriteLine(border + reset);
            }
            else
            {
                int maxLength = title.Length;
                foreach (string line in lines)
                {
                    if (line.Length > maxLength)
                    {
                        maxLength = line.Length;
                    }
                }

                string border = "+" + new string('-', maxLength + 2) + "+";
                Console.WriteLine(color + border);
                Console.WriteLine("| " + title + new string(' ', maxLength - title.Length) + " |");
                Console.WriteLine(border);

                foreach (string line in lines)
                {
                    Console.WriteLine("| " + line + new string(' ', maxLength - line.Length) + " |");
                }

                Console.WriteLine(border + reset);
            }
        }

        // Overloaded method for backward compatibility
        public static void box(string title, string message)
        {
            box(title, message, "info");
        }

        public static Common GetInstance()
        {
            if (Instance == null)
            {
                Instance = new Common();
            }
            return Instance;
        }

        public static string[] Split(string input)
        {
            return input.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
