using System;
using System.Text;

namespace SharpMASM
{
    /// <summary>
    /// Implementation of IMemoryManager using standard arrays
    /// </summary>
    public class ArrayMemoryManager : IMemoryManager
    {
        private static ArrayMemoryManager? _instance;
        private long[] _memory;
        private long _size;
        private bool _disposed = false;

        private ArrayMemoryManager(string? mappedFile)
        {
            // Initialize with default memory size
            _size = (long)Common.MemorySize / sizeof(long);
            _memory = new long[_size];
            if (CmdArgs.GetInstance().Verbose || CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"Initialized ArrayMemoryManager with {_size} elements");
            }
        }

        public static ArrayMemoryManager GetInstance(string? mappedFile)
        {
            if (_instance == null)
            {
                _instance = new ArrayMemoryManager(mappedFile);
            }
            return _instance;
        }
        
        public long ReadLong(long position)
        {
            long index = position / sizeof(long);
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(position));
                
            return _memory[index];
        }
        
        public void WriteLong(long position, long value)
        {
            long index = position / sizeof(long);
            if (index < 0 || index >= _size)
                throw new ArgumentOutOfRangeException(nameof(position));
                
            _memory[index] = value;
        }
        
        public long Read(string register)
        {
            if (Common.Registers.Contains(register))
            {
                return _memory[Array.IndexOf(Common.Registers, register)];
            }
            else if (register.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(register.Substring(1), out long address))
                {
                    if (address >= 0 && address < _size)
                    {
                        return _memory[address];
                    }
                    else
                    {
                        throw new MASMException($"Memory address out of range: {register}");
                    }
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {register}");
                }
            }
            else if (long.TryParse(register, out long value))
            {
                return value;
            }
            else
            {
                throw new MASMException($"Invalid register or memory address: {register}");
            }
        }
        
        public void Write(string register, long value)
        {
            if (Common.Registers.Contains(register))
            {
                _memory[Array.IndexOf(Common.Registers, register)] = value;
            }
            else if (register.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(register.Substring(1), out long address))
                {
                    if (address >= 0 && address < _size)
                    {
                        _memory[address] = value;
                    }
                    else
                    {
                        throw new MASMException($"Memory address out of range: {register}");
                    }
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {register}");
                }
            }
            else
            {
                throw new MASMException($"Invalid register or memory address: {register}");
            }
        }
        
        public string Read_String(string startingRegister)
        {
            if (startingRegister.StartsWith("$"))
            {
                if (long.TryParse(startingRegister.Substring(1), out long address))
                {
                    StringBuilder sb = new StringBuilder();
                    long currentAddress = address;
                    
                    while (true)
                    {
                        if (currentAddress >= _size) break;
                        long charValue = _memory[currentAddress];
                        if (charValue == 0) break; // Null terminator
                        sb.Append((char)charValue);
                        currentAddress++;
                    }
                    
                    return sb.ToString();
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {startingRegister}");
                }
            }
            else
            {
                throw new MASMException($"Invalid register or memory address: {startingRegister}");
            }
        }
        
        public void Write_String(string startingRegister, string value)
        {
            if (startingRegister.StartsWith("$"))
            {
                if (long.TryParse(startingRegister.Substring(1), out long address))
                {
                    // Write each character of the string to memory
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (address + i >= _size) break;
                        _memory[address + i] = value[i];
                    }
                    
                    // Add null terminator
                    if (address + value.Length < _size)
                    {
                        _memory[address + value.Length] = 0;
                    }
                }
                else
                {
                    throw new MASMException($"Invalid memory address format: {startingRegister}");
                }
            }
            else
            {
                throw new MASMException($"Invalid register or memory address: {startingRegister}");
            }
        }
        
        public long this[long index]
        {
            get => _memory[index];
            set => _memory[index] = value;
        }
        
        public long Length => _size;
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
        
        ~ArrayMemoryManager()
        {
            Dispose(false);
        }
    }
}
