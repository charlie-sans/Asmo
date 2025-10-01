using SharpMASM;
using SharpMASM.MNI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SharpMASM.Core_Interpreter;

namespace SharpMASM
{
    public class Functions
    {
        /*
         * Functions.cs
         *
         * the various functions that are used in the SharpMASM project.
         *
         * (C) Finite, 2025
         */


        /*
         * Functions, the basic building blocks of the Micro-Assembly language.
         *
         * we have things like:
         * mov <dest> <src>
         * add <dest> <src>
         * pop <dest>
         * push <src>
         *
         * you can do things like mov RAX 5 or mov RAX RBX
         *
         * however, instead of moving various registers, we can also move memory addresses.
         *
         * memory is addresssed with a dolor sign, like so:
         *
         * mov $100 RAX, this will move the value in RAX to the memory address 100.
         * you can also move memory addresses to registers.
         * mov RAX $100, this will move the value at memory address 100 to RAX.
         */
        // Replace the direct reference to MappedMemoryFile with the interface
        public static IMemoryManager Long_memory;

        // Add a method to initialize the memory manager
        public static void InitializeMemory()
        {
            Long_memory = Common.InitializeMemory();
        }

        // Add comparison flags to track comparison results
        public static class ComparisonFlags
        {
            public static bool IsEqual { get; set; }
            public static bool IsLessThan { get; set; }
            public static bool IsGreaterThan { get; set; }

            public static void Reset()
            {
                IsEqual = false;
                IsLessThan = false;
                IsGreaterThan = false;
            }
        }

        // Modify each function to accept the instruction as a parameter
        public static void Mov(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for mov, Expected 2 min/max but didn't get enough");
            }

            // get the first argument
            string arg1 = instruction.args[0];
            // get the second argument
            string arg2 = instruction.args[1];

            // check if the first argument is a register
            if (Common.Registers.Contains(arg1))
            {
                // check if the second argument is a register
                if (Common.Registers.Contains(arg2))
                {
                    // get the value of the second argument
                    long value = Long_memory.Read(arg2);
                    // write the value to the first argument
                    Long_memory.Write(arg1, value);
                }
                else
                {
                    // check if the second argument is a memory address
                    if (arg2.StartsWith("$"))
                    {
                        // get the value of the memory address
                        long value = Long_memory.Read(arg2);
                        // write the value to the first argument
                        Long_memory.Write(arg1, value);
                    }
                    else
                    {
                        // check if the second argument is a number
                        if (long.TryParse(arg2, out long value))
                        {
                            // write the value to the first argument
                            Long_memory.Write(arg1, value);
                        }
                        else
                        {
                            throw new MASMException("Invalid argument for mov, Expected a register, memory address or number but got something else");
                        }
                    }
                }
            }
            else
            {
                // check if the first argument is a memory address
                if (arg1.StartsWith("$"))
                {
                    // check if the second argument is a register
                    if (Common.Registers.Contains(arg2))
                    {
                        // get the value of the second argument
                        long value = Long_memory.Read(arg2);
                        // write the value to the memory address
                        Long_memory.Write(arg1, value);
                    }
                    else
                    {
                        // check if the second argument is a memory address
                        if (arg2.StartsWith("$"))
                        {
                            // get the value of the memory address
                            long value = Long_memory.Read(arg2);
                            // write the value to the memory address
                            Long_memory.Write(arg1, value);
                        }
                        else
                        {
                            // check if the second argument is a number
                            if (long.TryParse(arg2, out long value))
                            {
                                // write the value to the memory address
                                Long_memory.Write(arg1, value);
                            }
                            else
                            {
                                throw new MASMException("Invalid argument for mov, Expected a register, memory address or number but got something else");
                            }
                        }
                    }
                }
                else
                {
                    throw new MASMException("Invalid argument for mov, Expected a register, memory address or number but got something else");



                }
            }
        }
        public static void Add(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for add, Expected 2 min/max but didn't get enough");
            }
            // get the first argument
            string arg1 = instruction.args[0];
            // get the second argument
            string arg2 = instruction.args[1];
            // check if the first argument is a register
            if (Common.Registers.Contains(arg1))
            {
                // check if the second argument is a register
                if (Common.Registers.Contains(arg2))
                {
                    // get the value of the second argument
                    long value = Long_memory.Read(arg2);
                    // add the value to the first argument
                    Long_memory.Write(arg1, Long_memory.Read(arg1) + value);
                }
                else
                {
                    // check if the second argument is a memory address
                    if (arg2.StartsWith("$"))
                    {
                        // get the value of the memory address
                        long value = Long_memory.Read(arg2);
                        // add the value to the first argument
                        Long_memory.Write(arg1, Long_memory.Read(arg1) + value);
                    }
                    else
                    {
                        // check if the second argument is a number
                        if (long.TryParse(arg2, out long value))
                        {
                            // add the value to the first argument
                            Long_memory.Write(arg1, Long_memory.Read(arg1) + value);
                        }
                        else
                        {
                            throw new MASMException("Invalid argument for add, Expected a register, memory address or number but got something else");
                        }
                    }
                }
            }
            else
            {
                // check if the first argument is a memory address
                if (arg1.StartsWith("$"))
                {
                    // check if the second argument is a register
                    if (Common.Registers.Contains(arg2))
                    {
                        // get the value of the second argument
                        long value = Long_memory.Read(arg2);
                        // add the value to the memory address
                        Long_memory.Write(arg1, Long_memory.Read(arg1) + value);
                    }
                    else
                    {
                        // check if the second argument is a memory address
                        if (arg2.StartsWith("$"))
                        {
                            // get the value of the memory address
                            long value = Long_memory.Read(arg2);
                            // add the value to the memory address
                            Long_memory.Write(arg1, Long_memory.Read(arg1) + value);

                        }

                    }

                }
                else
                {
                    throw new MASMException("Invalid argument for add, Expected a register, memory address or number but got something else");
                }
            }
        }
        public static void Out(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for out, Expected 2 min/max but didn't get enough");
            }
            // get the first argument
            string arg1 = instruction.args[0];
            // get the second argument
            string arg2 = instruction.args[1];

            // Get the output port (first argument)
            long port;
            if (arg1.StartsWith("$"))
            {
                string addressOrRegister = arg1.Substring(1);
                if (Common.Registers.Contains(addressOrRegister))
                {
                    // Register-based addressing: $RAX means "use value in RAX as the address"
                    port = Long_memory.Read(addressOrRegister);
                }
                else if (long.TryParse(addressOrRegister, out long address))
                {
                    // Direct memory addressing
                    port = Long_memory.Read(arg1);
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {arg1}");
                }
            }
            else if (Common.Registers.Contains(arg1))
            {
                port = Long_memory.Read(arg1);
            }
            else if (long.TryParse(arg1, out port))
            {
                // Direct value
            }
            else
            {
                throw new MASMException("Invalid first argument for out, Expected a register, memory address or number");
            }

            // Check if we're outputting a string from memory
            if (arg2.StartsWith("$"))
            {
                long startAddress;
                string addressOrRegister = arg2.Substring(1);

                if (Common.Registers.Contains(addressOrRegister))
                {
                    // Register-based addressing: $RAX means "use value in RAX as the address"
                    startAddress = Long_memory.Read(addressOrRegister);
                }
                else if (long.TryParse(addressOrRegister, out startAddress))
                {
                    // Direct memory addressing
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {arg2}");
                }

                // Read string from memory until null terminator (0)
                StringBuilder sb = new StringBuilder();
                long currentAddress = startAddress;
                long charValue;
                const int MAX_STRING_LENGTH = 10000; // Safety limit
                int charCount = 0;

                while (charCount < MAX_STRING_LENGTH)
                {
                    charValue = Long_memory.ReadLong(currentAddress * sizeof(long));
                    if (charValue == 0) break; // Null terminator

                    sb.Append((char)charValue);
                    currentAddress++;
                    charCount++;
                }
                if (CmdArgs.GetInstance().VeryVerbose)
                {
                    // time to drop all the data XD
                    Console.WriteLine($"OUT instruction executing with port {port} and string '{sb.ToString()}'");
                    Console.WriteLine($"String length: {sb.Length} characters");
                    Console.WriteLine($"String data: {sb}");
                    Console.WriteLine($"String data (hex): {BitConverter.ToString(Encoding.ASCII.GetBytes(sb.ToString()))}");
                }

                // Output the string
                if (port == 1)
                {
                    Console.Write(sb.ToString());
                }
                else if (port == 2)
                {
                    Console.Error.Write(sb.ToString());
                }
                else
                {
                    throw new MASMException($"Invalid port number for OUT: {port}");
                }
            }
            else
            {
                // Handle single character output (original behavior)
                long outputValue;
                if (Common.Registers.Contains(arg2))
                {
                    outputValue = Long_memory.Read(arg2);
                }
                else if (long.TryParse(arg2, out outputValue))
                {
                    // Direct value
                }
                else
                {
                    throw new MASMException("Invalid second argument for out, Expected a register, memory address or number");
                }

                if (CmdArgs.GetInstance().VeryVerbose)
                {
                    // time to drop all the data XD
                    Console.WriteLine($"OUT instruction executing with port {port} and string '{(char)outputValue}'");
                    Console.WriteLine($"Character data: {(char)outputValue}");
                    Console.WriteLine($"Character data (hex): {BitConverter.ToString(BitConverter.GetBytes((char)outputValue))}");

                }

                if (port == 1)
                {
                    Console.Write(outputValue);
                }
                else if (port == 2)
                {
                    Console.Error.Write(outputValue);
                }
                else
                {
                    throw new MASMException($"Invalid port number for OUT: {port}");
                }
            }
        }
        public static void Sub(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for sub, Expected 2 min/max but didn't get enough");
            }
            // get the first argument
            string arg1 = instruction.args[0];
            // get the second argument
            string arg2 = instruction.args[1];
            // check if the first argument is a register
            if (Common.Registers.Contains(arg1))
            {
                // check if the second argument is a register
                if (Common.Registers.Contains(arg2))
                {
                    // get the value of the second argument
                    long value = Long_memory.Read(arg2);
                    // add the value to the first argument
                    Long_memory.Write(arg1, Long_memory.Read(arg1) - value);
                }
                else
                {
                    // check if the second argument is a memory address
                    if (arg2.StartsWith("$"))
                    {
                        // get the value of the memory address
                        long value = Long_memory.Read(arg2);
                        // add the value to the first argument
                        Long_memory.Write(arg1, Long_memory.Read(arg1) - value);
                    }
                    else
                    {
                        // check if the second argument is a number
                        if (long.TryParse(arg2, out long value))
                        {
                            // add the value to the first argument
                            Long_memory.Write(arg1, Long_memory.Read(arg1) - value);
                        }
                        else
                        {
                            throw new MASMException("Invalid argument for sub, Expected a register, memory address or number but got something else");
                        }
                    }
                }
            }
            else
            {
                // check if the first argument is a memory address
                if (arg1.StartsWith("$"))
                {
                    // check if the second argument is a register
                    if (Common.Registers.Contains(arg2))
                    {
                        // get the value of the second argument
                        long value = Long_memory.Read(arg2);
                        // add the value to the memory address
                        Long_memory.Write(arg1, Long_memory.Read(arg1) - value);
                    }
                    else
                    {
                        // check if the second argument is a memory address
                        if (arg2.StartsWith("$"))
                        {
                            // get the value of the memory address
                            long value = Long_memory.Read(arg2);
                            // add the value to
                        }
                    }
                }
                else
                {
                    throw new MASMException("Invalid argument for sub, Expected a register, memory address or number but got something else");

                }
            }
        }
        public static void DB(instruction instruction)
        {
            if (instruction.args.Length < 2)
            {
                throw new MASMException("Invalid number of arguments for DB, expected at least a memory address and a value");
            }

            // get the first argument (memory address)
            string arg1 = instruction.args[0];

            if (!arg1.StartsWith("$"))
            {
                throw new MASMException($"Invalid argument for DB, expected a memory address but got {arg1}");
            }

            // get the second argument (data)
            string arg2 = instruction.args[1];

            if (CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"DB instruction executing with args: '{arg1}' and '{arg2}'");
            }

            // Check if the second argument is a string
            if ((arg2.StartsWith("\"") && arg2.EndsWith("\"")) || (arg2.StartsWith("'") && arg2.EndsWith("'")))
            {
                // Remove the quotes
                arg2 = arg2.Substring(1, arg2.Length - 2);

                // Replace escape sequences
                arg2 = arg2.Replace("\\n", "\n")
                           .Replace("\\r", "\r")
                           .Replace("\\t", "\t")
                           .Replace("\\\"", "\"")
                           .Replace("\\'", "'");

                // Convert the memory address string to a numeric value
                if (!long.TryParse(arg1.Substring(1), out long memoryAddress))
                {
                    throw new MASMException($"Invalid memory address format: {arg1}");
                }

                // Write each character of the string to consecutive memory addresses
                for (int i = 0; i < arg2.Length; i++)
                {
                    Long_memory.WriteLong((memoryAddress + i) * sizeof(long), arg2[i]);
                }

                // Add null terminator
                Long_memory.WriteLong((memoryAddress + arg2.Length) * sizeof(long), 0);

                if (CmdArgs.GetInstance().VeryVerbose)
                {
                    Console.WriteLine($"DB: Wrote string '{arg2}' to memory address {arg1}");
                }
            }
            else if (long.TryParse(arg2, out long value))
            {
                // Parse numeric value
                if (!long.TryParse(arg1.Substring(1), out long memoryAddress))
                {
                    throw new MASMException($"Invalid memory address format: {arg1}");
                }

                Long_memory.WriteLong(memoryAddress * sizeof(long), value);
            }
            else
            {
                throw new MASMException($"Invalid argument for DB, expected a string or number but got {arg2}");
            }
        }

        public static void Cmp(instruction instruction)
        {
            // Reset comparison flags
            ComparisonFlags.Reset();

            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for CMP, expected 2");
            }

            // Get operand values
            long value1 = GetOperandValue(instruction.args[0]);
            long value2 = GetOperandValue(instruction.args[1]);

            // Set comparison flags based on result
            ComparisonFlags.IsEqual = value1 == value2;
            ComparisonFlags.IsLessThan = value1 < value2;
            ComparisonFlags.IsGreaterThan = value1 > value2;

            if (CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"CMP {value1} and {value2}: Equal={ComparisonFlags.IsEqual}, Less={ComparisonFlags.IsLessThan}, Greater={ComparisonFlags.IsGreaterThan}");
            }
        }

        private static long GetOperandValue(string operand)
        {
            // Get value from register, memory address, or literal
            if (Common.Registers.Contains(operand))
            {
                return Long_memory.Read(operand);
            }
            else if (operand.StartsWith("$"))
            {
                return Long_memory.Read(operand);
            }
            else if (long.TryParse(operand, out long value))
            {
                return value;
            }
            else
            {
                throw new MASMException($"Invalid operand: {operand}");
            }
        }

        // Add placeholder methods for other instructions mentioned in v1instructions.md
        public static void Mul(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for MUL, expected 2");
            }

            string dest = instruction.args[0];
            long value1 = GetOperandValue(dest);
            long value2 = GetOperandValue(instruction.args[1]);

            Long_memory.Write(dest, value1 * value2);
        }

        public static void Div(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for DIV, expected 2");
            }

            string dest = instruction.args[0];
            long value1 = GetOperandValue(dest);
            long value2 = GetOperandValue(instruction.args[1]);

            if (value2 == 0)
            {
                throw new MASMException("Division by zero");
            }

            Long_memory.Write(dest, value1 / value2);
        }

        public static void And(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for AND, expected 2");
            }

            string dest = instruction.args[0];
            long value1 = GetOperandValue(dest);
            long value2 = GetOperandValue(instruction.args[1]);

            Long_memory.Write(dest, value1 & value2);
        }

        public static void Or(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for OR, expected 2");
            }

            string dest = instruction.args[0];
            long value1 = GetOperandValue(dest);
            long value2 = GetOperandValue(instruction.args[1]);

            Long_memory.Write(dest, value1 | value2);
        }

        public static void Xor(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for XOR, expected 2");
            }

            string dest = instruction.args[0];
            long value1 = GetOperandValue(dest);
            long value2 = GetOperandValue(instruction.args[1]);

            Long_memory.Write(dest, value1 ^ value2);
        }

        public static void Not(instruction instruction)
        {
            if (instruction.args.Length != 1)
            {
                throw new MASMException("Invalid number of arguments for NOT, expected 1");
            }

            string dest = instruction.args[0];
            long value = GetOperandValue(dest);

            Long_memory.Write(dest, ~value);
        }

        public static void Inc(instruction instruction)
        {
            if (instruction.args.Length != 1)
            {
                throw new MASMException("Invalid number of arguments for INC, expected 1");
            }

            string dest = instruction.args[0];
            long value = GetOperandValue(dest);

            Long_memory.Write(dest, value + 1);
        }

        public static void Dec(instruction instruction)
        {
            if (instruction.args.Length != 1)
            {
                throw new MASMException("Invalid number of arguments for DEC, expected 1");
            }

            string dest = instruction.args[0];
            long value = GetOperandValue(dest);

            Long_memory.Write(dest, value - 1);
        }

        public static void Push(instruction instruction)
        {
            if (instruction.args.Length != 1)
            {
                throw new MASMException("Invalid number of arguments for PUSH, expected 1");
            }

            // Get RSP value
            long rsp = Long_memory.Read("RSP");

            // Push value onto stack
            long value = GetOperandValue(instruction.args[0]);
            Long_memory.Write($"{rsp}", value);

            // Decrement stack pointer
            Long_memory.Write("RSP", rsp - 8); // Assuming 64-bit values
        }

        public static void Pop(instruction instruction)
        {
            if (instruction.args.Length != 1)
            {
                throw new MASMException("Invalid number of arguments for POP, expected 1");
            }

            // Get RSP value and increment it
            long rsp = Long_memory.Read("RSP");
            rsp += 8; // Assuming 64-bit values

            // Pop value from stack
            long value = Long_memory.Read($"{rsp}");

            // Store value in destination
            string dest = instruction.args[0];
            Long_memory.Write(dest, value);

            // Update stack pointer
            Long_memory.Write("RSP", rsp);
        }

        public static void Cout(instruction instruction)
        {
            if (instruction.args.Length != 2)
            {
                throw new MASMException("Invalid number of arguments for COUT, expected 2");
            }

            // Get the port (1 for stdout, 2 for stderr)
            long port = GetOperandValue(instruction.args[0]);

            // Get the character value
            long charValue = GetOperandValue(instruction.args[1]);

            // Output the character
            if (port == 1)
            {
                Console.Write((char)charValue);
            }
            else if (port == 2)
            {
                Console.Error.Write((char)charValue);
            }
            else
            {
                throw new MASMException($"Invalid port number for COUT: {port}");
            }
        }

        // Add MNI execution function
        public static void MNI(instruction instruction)
        {
            MNIModuleManager manager = MNIModuleManager.GetInstance();
            bool success = manager.ExecuteMNIFunction(instruction);
            
            if (!success)
            {
                Console.Error.WriteLine($"Failed to execute MNI instruction: {instruction.name} {string.Join(" ", instruction.args)}");
            }
        }
    }
}
