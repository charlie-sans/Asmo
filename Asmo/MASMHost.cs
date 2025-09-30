using System;
using System.IO;
using SharpMASM;
using static SharpMASM.Core_Interpreter;
using Asmo.Gfx;
using Asmo.Types;
using SharpMASM.MNI.Modules;

namespace Asmo
{
    /// <summary>
    /// Host service for integrating MASM scripts into the Asmo framework.
    /// Provides lifecycle management for loading, executing, and managing MASM scripts.
    /// </summary>
    public class MASMHost
    {
        private Instructions? _instructions;
        private bool _isLoaded = false;
        private bool _isRunning = false;
        private Surface _surface;
        private long _framebufferAddress;

        public MASMHost(Surface surface)
        {
            _surface = surface ?? throw new ArgumentNullException(nameof(surface));
        }

        /// <summary>
        /// Loads a MASM script from file.
        /// </summary>
        /// <param name="filePath">Path to the .masm file</param>
        /// <returns>True if loaded successfully</returns>
        public bool Load(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"MASM script not found: {filePath}");
                    return false;
                }

                string[] lines = File.ReadAllLines(filePath);

                // Initialize Common and Instructions classes
                Common.Instance = new Common();
                Common.InstructionInstance = new Instructions();

                // Allocate framebuffer in memory
                int width = _surface.Pixels.Length;
                int height = _surface.Pixels[0].Length;
                long framebufferSize = width * height * 4; // 4 bytes per pixel
                _framebufferAddress = 0x10000000; // Fixed address for framebuffer (256MB)

                // Initialize graphics module
                SharpMASM.MNI.Modules.GraphicsModule.Initialize((MappedMemoryFile)Common.Memory, _framebufferAddress, width, height);

                // Parse the instructions
                Parsing.ParseInstructions(lines);

                _instructions = Instructions.GetInstance();
                _isLoaded = true;

                Console.WriteLine("MASM script loaded successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading MASM script: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resets the MASM execution state.
        /// </summary>
        public void Reset()
        {
            if (_instructions != null)
            {
                Instructions.ResetInstructionPointer();
                _isRunning = false;
                Console.WriteLine("MASM execution reset.");
            }
        }

        /// <summary>
        /// Starts or resumes execution of the loaded MASM script.
        /// </summary>
        public void Start()
        {
            if (!_isLoaded || _instructions == null)
            {
                Console.WriteLine("No MASM script loaded.");
                return;
            }
            GraphicsModule.Initialize((MappedMemoryFile)Common.Memory, _framebufferAddress, _surface.Pixels.Length, _surface.Pixels[0].Length);
            if (!_isRunning)
            {
                // Check for main label and jump to it if found
                if (Instructions.Labels.TryGetValue("main", out long mainPosition))
                {
                    Instructions.GetInstance().instructionPointer = mainPosition;
                }

                _isRunning = true;
                Console.WriteLine("MASM execution started.");
            }
        }

        /// <summary>
        /// Executes a single step of the MASM script.
        /// </summary>
        /// <returns>True if execution continues, false if halted or error</returns>
        public bool Step()
        {
            if (!_isRunning || _instructions == null)
            {
                return false;
            }

            try
            {
                if (Instructions.GetInstance().instructionPointer >= Instructions.GetInstance().instructionCount)
                {
                    _isRunning = false;
                    Console.WriteLine("MASM execution completed.");
                    return false;
                }

                instruction i = Instructions.GetInstance().GetInstruction();

                // Execute the instruction
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
                    case "jmp":
                        Jump(i);
                        break;
                    case "jz":
                        JumpIfZero(i);
                        break;
                    case "jnz":
                        JumpIfNotZero(i);
                        break;
                    case "mni":
                        Functions.MNI(i);
                        break;
                    case "hlt":
                        _isRunning = false;
                        Console.WriteLine("MASM execution halted.");
                        return false;
                    
                    default:
                        Console.WriteLine($"Unknown instruction: {i.name}");
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing MASM instruction: {ex.Message}");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stops execution of the MASM script.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            Console.WriteLine("MASM execution stopped.");
        }

        /// <summary>
        /// Shuts down the MASM host, releasing resources.
        /// </summary>
        public void Shutdown()
        {
            Stop();
            _instructions = null;
            _isLoaded = false;
            Common.Instance = null;
            Common.InstructionInstance = null;
            Console.WriteLine("MASM host shut down.");
        }

        /// <summary>
        /// Syncs the surface pixels to the MASM framebuffer memory.
        /// </summary>
        public void SyncSurfaceToMemory()
        {
            if (!_isLoaded) return;

            int width = _surface.Pixels.Length;
            int height = _surface.Pixels[0].Length;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = _surface.Pixels[x][y];
                    int argb = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
                    long address = _framebufferAddress + (y * width + x) * 4;
                    Common.Memory.Write(address.ToString(), argb);
                }
            }
        }

        /// <summary>
        /// Syncs the MASM framebuffer memory back to the surface pixels.
        /// </summary>
        public void SyncMemoryToSurface()
        {
            if (!_isLoaded) return;

            int width = _surface.Pixels.Length;
            int height = _surface.Pixels[0].Length;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    long address = _framebufferAddress + (y * width + x) * 4;
                    int argb = (int)Common.Memory.Read(address.ToString());
                    byte a = (byte)((argb >> 24) & 0xFF);
                    byte r = (byte)((argb >> 16) & 0xFF);
                    byte g = (byte)((argb >> 8) & 0xFF);
                    byte b = (byte)(argb & 0xFF);
                    _surface.Pixels[x][y] = new Color(r, g, b, a);
                }
            }
        }

        /// <summary>
        /// Gets the current instruction pointer.
        /// </summary>
        public long InstructionPointer => _instructions?.instructionPointer ?? -1;

        /// <summary>
        /// Checks if a script is loaded.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Checks if execution is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        private void Jump(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new Exception("JMP requires a label argument");
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
                    throw new Exception($"Label not found: {label}");
                }
            }
            else
            {
                throw new Exception("JMP argument must be a label (prefixed with #)");
            }
        }

        private void JumpIfZero(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new Exception("JZ requires at least a target label");
            }

            // Check if zero flag (assuming RFLAGS bit 6 or something, but simplified)
            // For now, assume based on last comparison
            // Since we don't have flags, perhaps check if last CMP result was equal
            // But for simplicity, always jump for demo
            Jump(i);
        }

        private void JumpIfNotZero(instruction i)
        {
            if (i.args.Length < 1)
            {
                throw new Exception("JNZ requires at least a target label");
            }

            // Similar to above
            Jump(i);
        }
    }
}