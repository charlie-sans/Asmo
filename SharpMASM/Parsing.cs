using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharpMASM.Core_Interpreter;

namespace SharpMASM
{

    /*
     * Parsing.cs
     * 
     * Description: Parsing class for the SharpMASM project.
     *
     * (C) Finite, 2025
     */


    /*
     * Micro-Assembly has some great features like labels, comments, and instructions.
     * this class helps parse out the instructions and labels from the source code.
     * 
     * labels aare defined by 
     * lbl <name>
     * 
     * and are added to the label map.
     * these are preprocess steps before the actual execution of each instruction.
     * 
     * we also have include statements
     * #include "file"
     * 
     * lets say we want a stdio function right?
     * #include "stdio.print"
     * this will include the <root>/stdio/print.masm file from Common.root
     * if common.root is not set, it will look in the current directory that the program is running in.
     * 
     */

    // instruction class
    public class Parsing
    {
        // label map
        public static Dictionary<string, int> labelMap = new Dictionary<string, int>();

        // include map
        public static Dictionary<string, string> includeMap = new Dictionary<string, string>();

        // parse the source code

        private static bool HasIncludes(string content)
        {
            return content.Contains("#include");
        }

        // Internal include handler
        private static string HandleInclude(string path, string originalLine)
        {
            try
            {
            // Convert dot notation to directory path and add .masm extension
            string convertedPath = path.Replace(".", "/") + ".masm";
            
            string rootDir = Path.GetDirectoryName(CmdArgs.GetInstance().FileName) ?? ".";
            string fullPath = Path.Combine(rootDir, convertedPath);
            Console.WriteLine(fullPath);
            
            // Look in the current directory if not found in the relative path
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(Directory.GetCurrentDirectory(), convertedPath);
            }

            // Look in the root/stdio directory
            if (!File.Exists(fullPath))
            {
                string rootPath = Path.Combine(Directory.GetCurrentDirectory(), "root");
                if (Directory.Exists(rootPath))
                {
                fullPath = Path.Combine(rootPath, convertedPath);
                }
            }

            if (!File.Exists(fullPath))
            {
                throw new MASMException($"Include file not found: {convertedPath}");
            }

            return File.ReadAllText(fullPath) + "\n";
            }
            catch (Exception ex) when (!(ex is MASMException))
            {
            throw new MASMException($"Error including file {path}: {ex.Message}");
            }
        }

        private static string Preprocess(string[] lines)
        {
            StringBuilder processed = new StringBuilder();
            foreach (string line in lines)
            {
                processed.Append(line).Append("\n");
            }

            string result = processed.ToString();
            int maxPasses = 10; // Prevent infinite recursion
            int currentPass = 0;

            while (HasIncludes(result) && currentPass < maxPasses)
            {
                StringBuilder newProcessed = new StringBuilder();
                string[] currentLines = result.Split('\n');

                foreach (string line in currentLines)
                {
                    string trimmedLine = line.Trim();
                    if (trimmedLine.ToLower().StartsWith("#include"))
                    {
                        int start = trimmedLine.IndexOf("\"");
                        int end = trimmedLine.LastIndexOf("\"");
                        if (start != -1 && end != -1 && start != end)
                        {
                            string path = trimmedLine.Substring(start + 1, end - start - 1);
                            newProcessed.Append(HandleInclude(path, line));
                        }
                    }
                    else if (trimmedLine.StartsWith(";"))
                    {
                        continue;
                    }
                    else
                    {
                        int commentStart = trimmedLine.IndexOf(';');
                        if (commentStart != -1)
                        {
                            trimmedLine = trimmedLine.Substring(0, commentStart).Trim();
                        }
                        if (!string.IsNullOrEmpty(trimmedLine))
                        {
                            newProcessed.Append(trimmedLine).Append("\n");
                        }
                    }
                }

                result = newProcessed.ToString();
                currentPass++;
            }

            if (currentPass >= maxPasses)
            {
                throw new MASMException("Too many include passes", 0, "", "Error in instruction: #include");
            }

            return result;
        }

        public static void ParseInstructions(string[] ops)
        {
            // Preprocess includes first
            string preprocessed = Preprocess(ops);
            string[] processedOps = preprocessed.Split('\n');
            
            if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine(preprocessed);
            }

            // Get the Instructions instance and clear existing instructions
            if (Common.InstructionInstance == null)
            {
                Common.InstructionInstance = new Core_Interpreter.Instructions();
            }
            Instructions.ClearInstructions();
            
            // Clear existing labels before collection
            Instructions.Labels.Clear();
            
            // First pass: collect labels and their positions
            int instructionIndex = 0;
            foreach (string line in processedOps)
            {
                string trimmedLine = line.Trim();
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;
                    
                if (trimmedLine.ToLower().StartsWith("lbl "))
                {
                    string labelName = trimmedLine.Substring(4).Trim();
                    
                    // Store label with current instruction index
                    Instructions.Labels[labelName] = instructionIndex;
                    
                    if (CmdArgs.GetInstance().VeryVerbose)
                    {
                        Console.WriteLine($"Label '{labelName}' points to instruction index {instructionIndex}");
                    }
                    
                    // Don't increment instruction index for label declarations
                    continue;
                }
                
                // Only increment for actual instructions
                instructionIndex++;
            }

            // Second pass: read instructions (excluding label declarations)
            foreach (string line in processedOps)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;
                    
                // Skip label declarations - they're not instructions
                if (trimmedLine.ToLower().StartsWith("lbl "))
                    continue;

                string name;
                string[] args;

                // Special handling for DB instruction
                if (trimmedLine.ToUpper().StartsWith("DB "))
                {
                    name = "DB";
                    // We need special parsing for DB because the second argument might be a string with spaces
                    string restOfLine = trimmedLine.Substring(2).Trim(); // Skip "DB"
                    
                    // Find the first space to get the memory address
                    int firstSpace = restOfLine.IndexOf(' ');
                    if (firstSpace > 0)
                    {
                        string address = restOfLine.Substring(0, firstSpace).Trim();
                        
                        // Everything after the first space is potentially the data argument
                        string dataArg = restOfLine.Substring(firstSpace).Trim();
                        
                        // For quoted strings, we need to keep the entire string as one argument
                        if (dataArg.StartsWith("\"") && dataArg.EndsWith("\"") || 
                            dataArg.StartsWith("'") && dataArg.EndsWith("'"))
                        {
                            // This is a quoted string - keep it as is
                            args = new string[] { address, dataArg };
                        }
                        else
                        {
                            // Check if it starts with a quote but doesn't end with one
                            // This means the string contains spaces and we need to handle it specially
                            if (dataArg.StartsWith("\"") || dataArg.StartsWith("'"))
                            {
                                // This is a quoted string that was not properly parsed
                                // Just use the rest of the line as the second argument
                                args = new string[] { address, dataArg };
                            }
                            else
                            {
                                // This is a number or other data
                                args = new string[] { address, dataArg };
                            }
                        }
                        
                        if (CmdArgs.GetInstance().VeryVerbose)
                        {
                            Console.WriteLine($"DB instruction parsed as: DB {address} {dataArg}");
                        }
                    }
                    else
                    {
                        // Missing second argument
                        args = new string[] { restOfLine };
                        if (CmdArgs.GetInstance().VeryVerbose)
                        {
                            Console.WriteLine($"Warning: DB instruction missing second argument: {trimmedLine}");
                        }
                    }
                }
                else
                {
                    // Normal instruction parsing
                    string[] parts = trimmedLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    name = parts[0];
                    args = parts.Length > 1 ? parts.Skip(1).ToArray() : new string[0];
                    
                    // Remove any inline comments from the operands
                    for (int i = 0; i < args.Length; i++)
                    {
                        int commentStart = args[i].IndexOf(';');
                        if (commentStart != -1)
                        {
                            args[i] = args[i].Substring(0, commentStart).Trim();
                        }
                    }
                }

                // Add instruction to the Instructions class
                Instructions.AddInstruction(name, args);
            }

            // Set instruction count for execution
            Instructions.GetInstance().instructionCount = Instructions.instructions.Count;
            
            if (CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine("\nCollected labels:");
                foreach (var label in Instructions.Labels)
                {
                    Console.WriteLine($"{label.Key}: {label.Value}");
                    if (label.Value < Instructions.instructions.Count)
                    {
                        var instr = Instructions.instructions[(int)label.Value];
                        Console.WriteLine($"  -> Points to: {instr.name} {string.Join(" ", instr.args)}");
                    }
                }
                
                Console.WriteLine("\nFull instruction listing with positions:");
                for (int i = 0; i < Instructions.instructions.Count; i++)
                {
                    var instr = Instructions.instructions[i];
                    Console.WriteLine($"{i}: {instr.name} {string.Join(" ", instr.args)}");
                }
                Console.WriteLine();
            }
            
            // Initialize memory if needed
            if (Common.Memory == null)
            {
                Common.Memory = MappedMemoryFile.GetInstance(Common.MappedFile);
            }
            
            PrintAllInstructions(Instructions.instructions);
        }

        public static void PrintAllInstructions(List<instruction> instructions)
        {

            if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                foreach (instruction i in instructions)
                {
                    Console.Write("Instr: " + i.name + " ");
                    foreach (string arg in i.args)
                    {
                        Console.Write(arg + " ");
                    }
                    Console.WriteLine();
                }
            }

        }
    }
}
