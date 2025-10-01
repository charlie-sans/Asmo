using SharpMASM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpMASM
{
    public class Core_Interpreter
    {
        // Add a stack to store return addresses for CALL instructions
        private static Stack<long> callStack = new Stack<long>();

        public static Instructions Process_File(string[] lines)
        {
            Instructions.ResetInstructionPointer();
            Parsing.ParseInstructions(lines);
            return Instructions.GetInstance();
        }

        public static void Interpret(Instructions instructions)
        {
            // Reset instruction pointer for execution
            Instructions.ResetInstructionPointer();
            
            // Check for main label and jump to it if found
            if (Instructions.Labels.TryGetValue("main", out long mainPosition))
            {
                if (CmdArgs.GetInstance().VeryVerbose)
                {
                    Console.WriteLine($"Starting execution from 'main' label at position {mainPosition}");
                    // Print which instruction is at this position for debugging
                    if (mainPosition < Instructions.instructions.Count)
                    {
                        var mainInstr = Instructions.instructions[(int)mainPosition];
                        Console.WriteLine($"Instruction at main position: {mainInstr.name} {string.Join(" ", mainInstr.args)}");
                    }
                }
                Instructions.GetInstance().instructionPointer = mainPosition;
            }
            else if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine("Warning: No 'main' label found, starting from first instruction");
            }
            
            // Execute instructions
            while (Instructions.GetInstance().instructionPointer < Instructions.GetInstance().instructionCount)
            {
                long currentPos = Instructions.GetInstance().instructionPointer;
                instruction i = Instructions.GetInstance().GetInstruction();
                if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
                {
                    Console.WriteLine($"Executing instruction at position {currentPos}: {i.name} {string.Join(" ", i.args)}");
                }
                
                switch (i.name.ToLower())
                {
                    case "mov":
                        Functions.Mov(i);
                        break;
                    case "add":
                        Functions.Add(i);
                        break;
                    case "sub":
                        Functions.Sub(i);
                        break;
                    case "mul":
                        Functions.Mul(i);
                        break;
                    case "div":
                        Functions.Div(i);
                        break;
                    case "and":
                        Functions.And(i);
                        break;
                    case "or":
                        Functions.Or(i);
                        break;
                    case "xor":
                        Functions.Xor(i);
                        break;
                    case "not":
                        Functions.Not(i);
                        break;
                    case "db":
                        Functions.DB(i);
                        break;
                    case "out":
                        Functions.Out(i);
                        break;
                    case "cout":
                        Functions.Cout(i);
                        break;
                    case "push":
                        Functions.Push(i);
                        break;
                    case "pop":
                        Functions.Pop(i);
                        break;
                    case "inc":
                        Functions.Inc(i);
                        break;
                    case "dec":
                        Functions.Dec(i);
                        break;
                    case "cmp":
                        Functions.Cmp(i);
                        break;
                    case "je":
                        JumpIfEqual(i);
                        break;
                    case "jl":
                        JumpIfLess(i);
                        break;
                    case "jne":
                        JumpIfNotEqual(i);
                        break;
                    case "jmp":
                        Jump(i);
                        break;
                    case "call":
                        Call(i);
                        break;
                    case "ret":
                        Return();
                        break;
                    case "hlt":
                        if (Common.exitOnHLT)
                            return; // End execution
                        break;
                    case "exit":
                        ExitProgram(i);
                        return;
                    case "mni":
                        Functions.MNI(i);
                        break;
                    default:
                        Common.box("Error", "Unknown instruction: " + i.name, "error");
                        throw new MASMException("Unknown instruction: " + i.name);
                }
            }
        }

        private static void Jump(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new MASMException("JMP requires a label argument");
            }

            if (i.args[0].StartsWith("#"))
            {
                string label = i.args[0].Substring(1);
                if (Instructions.Labels.TryGetValue(label, out long position))
                {
                    Instructions.GetInstance().instructionPointer = position;
                }
                else
                {
                    throw new MASMException($"Label not found: {label}");
                }
            }
            else
            {
                throw new MASMException("JMP argument must be a label (prefixed with #)");
            }
        }

        private static void JumpIfEqual(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new MASMException("JE requires at least a target label");
            }

            string targetLabel;
            string elseLabel = null;

            // Get the comparison result from the flags
            bool isEqual = Functions.ComparisonFlags.IsEqual;

            // Handle JE with one or two label arguments
            if (i.args[0].StartsWith("#"))
            {
                targetLabel = i.args[0].Substring(1);
                
                // If there's a second label for the false branch
                if (i.args.Length > 1 && i.args[1].StartsWith("#"))
                {
                    elseLabel = i.args[1].Substring(1);
                }

                if (isEqual)
                {
                    // Jump to target label if equal
                    if (Instructions.Labels.TryGetValue(targetLabel, out long position))
                    {
                        Instructions.GetInstance().instructionPointer = position;
                    }
                    else
                    {
                        throw new MASMException($"Label not found: {targetLabel}");
                    }
                }
                else if (elseLabel != null)
                {
                    // Jump to else label if not equal and an else label is provided
                    if (Instructions.Labels.TryGetValue(elseLabel, out long position))
                    {
                        Instructions.GetInstance().instructionPointer = position;
                    }
                    else
                    {
                        throw new MASMException($"Label not found: {elseLabel}");
                    }
                }
                // If not equal and no else label, continue to next instruction
            }
            else
            {
                throw new MASMException("JE argument must be a label (prefixed with #)");
            }
        }

        private static void JumpIfLess(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new MASMException("JL requires a label argument");
            }

            if (i.args[0].StartsWith("#"))
            {
                string label = i.args[0].Substring(1);
                
                // Check if comparison result was "less than"
                if (Functions.ComparisonFlags.IsLessThan)
                {
                    if (Instructions.Labels.TryGetValue(label, out long position))
                    {
                        Instructions.GetInstance().instructionPointer = position;
                    }
                    else
                    {
                        throw new MASMException($"Label not found: {label}");
                    }
                }
                // If not less, continue to next instruction
            }
            else
            {
                throw new MASMException("JL argument must be a label (prefixed with #)");
            }
        }

        private static void JumpIfNotEqual(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new MASMException("JNE requires a label argument");
            }

            if (i.args[0].StartsWith("#"))
            {
                string label = i.args[0].Substring(1);
                
                // Check if comparison result was "not equal"
                if (!Functions.ComparisonFlags.IsEqual)
                {
                    if (Instructions.Labels.TryGetValue(label, out long position))
                    {
                        Instructions.GetInstance().instructionPointer = position;
                    }
                    else
                    {
                        throw new MASMException($"Label not found: {label}");
                    }
                }
                // If equal, continue to next instruction
            }
            else
            {
                throw new MASMException("JNE argument must be a label (prefixed with #)");
            }
        }

        private static void Call(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new MASMException("CALL requires a label argument");
            }

            if (i.args[0].StartsWith("#"))
            {
                string label = i.args[0].Substring(1);
                if (Instructions.Labels.TryGetValue(label, out long position))
                {
                    if (CmdArgs.GetInstance().VeryVerbose)
                    {
                        Console.WriteLine($"Calling function at label '{label}' (position {position})");
                    }
                    
                    // Save the current instruction pointer on the call stack
                    callStack.Push(Instructions.GetInstance().instructionPointer);
                    
                    // Jump to the function
                    Instructions.GetInstance().instructionPointer = position;
                }
                else
                {
                    throw new MASMException($"Label not found: {label}");
                }
            }
            else if (i.args[0].StartsWith("$"))
            {
                // External function call handling if needed
                throw new MASMException("External function calls not implemented yet");
            }
            else
            {
                throw new MASMException("CALL argument must be a label (prefixed with #)");
            }
        }

        private static void Return()
        {
            if (callStack.Count == 0)
            {
                throw new MASMException("RET instruction without a matching CALL");
            }

            // Pop the return address and jump back
            long returnAddress = callStack.Pop();
            
            if (CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"Returning to position {returnAddress}");
            }
            
            Instructions.GetInstance().instructionPointer = returnAddress;
        }

        private static void ExitProgram(instruction i)
        {
            int exitCode = 0;
            if (i.args.Length > 0)
            {
                // Get exit code from register or literal
                if (Common.Registers.Contains(i.args[0]))
                {
                    exitCode = (int)MappedMemoryFile.GetInstance(Common.MappedFile).Read(i.args[0]);
                }
                else if (int.TryParse(i.args[0], out int code))
                {
                    exitCode = code;
                }
            }
            
            Common.VerbosePrint($"Program exited with code: {exitCode}");
            Environment.Exit(exitCode);
        }

        public class Instructions
        {
            public static MappedMemoryFile Memory = MappedMemoryFile.GetInstance(Common.MappedFile);
            public static Dictionary<string, long> Labels = new Dictionary<string, long>();
            public static List<instruction> instructions = new List<instruction>();
            public long instructionPointer = 0;
            public long instructionCount = 0;
            public static string[] Supported_Instructions = Common.Instructions;
            public static void AddInstruction(string name, string[] args)
            {
                instructions.Add(new instruction(name, args));
            }
            public instruction GetInstruction()
            {
                if (instructionPointer >= instructionCount || instructions.Count == 0)
                {
                    return new instruction("HLT", new string[] { });
                }
                // Store the current instruction
                instruction instr = instructions[(int)instructionPointer];
                // Increment the pointer for next time
                instructionPointer++;
                
                return instr;
            }

            public static Instructions GetInstance()
            {
                return Common.InstructionInstance;
            }
            
            public static void ResetInstructionPointer()
            {
                if (Common.InstructionInstance != null)
                {
                    Common.InstructionInstance.instructionPointer = 0;
                }
            }
            
            public static void ResetInstructions()
            {
                instructions.Clear();
            }
            public static void PrintInstructions_plus_DebuggingInfo()
            {
                Console.WriteLine("Instructions:");
                foreach (instruction i in instructions)
                {
                    Console.Write(i.name + " ");
                    foreach (string arg in i.args)
                    {
                        Console.Write(arg + " ");
                    }
                    Console.WriteLine();
                }
            }
            public static void ClearInstructions()
            {
                instructions.Clear();
            }
        }
    }
}
