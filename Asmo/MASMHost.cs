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
    private IMemoryManager? _memory;

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

                // Initialize command args singleton if needed
                if (CmdArgs.Instance == null)
                {
                    CmdArgs.Instance = new CmdArgs();
                }

                // Set up Common & memory (ArrayMemoryManager by default)
                Common.Instance = new Common();
                Common.Memory = Common.InitializeMemory();
                Functions.Long_memory = Common.Memory;
                _memory = Common.Memory;

                // Prepare instruction container
                Common.InstructionInstance = new Instructions();

                // Allocate framebuffer base address (choose a safe offset; using 16MB)
                int width = _surface.Pixels.Length;
                int height = _surface.Pixels[0].Length;
                // Choose a base element index in memory for the framebuffer.
                // Using element indices (each element is a long) so we store one pixel per element.
                _framebufferAddress = 1_000_000; // leaves room for registers & other data at lower addresses

                // Initialize graphics module using generic memory manager
                GraphicsModule.Initialize(_memory, _framebufferAddress, width, height);

                // Parse and load program
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
            // Re-initialize graphics module in case surface dimensions changed
            if (_memory != null)
            {
                GraphicsModule.Initialize(_memory, _framebufferAddress, _surface.Pixels.Length, _surface.Pixels[0].Length);
            }
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
                bool cont = Core_Interpreter.Step(_instructions);
                if (!cont)
                {
                    _isRunning = false;
                    Console.WriteLine("MASM execution completed or halted.");
                }
                return cont;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing MASM instruction: {ex.Message}");
                _isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Runs the MASM script to completion using the full interpreter loop.
        /// </summary>
        public void Run()
        {
            if (!_isLoaded || _instructions == null)
            {
                Console.WriteLine("No MASM script loaded.");
                return;
            }
            // Re-initialize graphics module in case surface dimensions changed
            if (_memory != null)
            {
                GraphicsModule.Initialize(_memory, _framebufferAddress, _surface.Pixels.Length, _surface.Pixels[0].Length);
            }
            _isRunning = true;
            Console.WriteLine("MASM execution started (full interpreter loop).");
            try
            {
                Core_Interpreter.Interpret(_instructions);
                _isRunning = false;
                Console.WriteLine("MASM execution completed or halted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MASM execution: {ex.Message}");
                _isRunning = false;
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

            if (_memory == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color color = _surface.Pixels[x][y];
                    int argb = (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
                    long address = _framebufferAddress + (y * width + x);
                    _memory.Write("$" + address.ToString(), argb);
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

            if (_memory == null) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    long address = _framebufferAddress + (y * width + x);
                    int argb = (int)_memory.Read("$" + address.ToString());
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