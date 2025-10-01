using SharpMASM;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SharpMASM
{
    public class MappedMemoryFile : IMemoryManager
    {
        private MemoryMappedFile _mappedFile;
        private MemoryMappedViewAccessor _accessor;
        private long _fileSize;
        private bool _disposed = false;
        public static MappedMemoryFile Instance { get; set; }

        public static MappedMemoryFile GetInstance(string filename)
        {
            if (Instance == null)
            {
                Instance = new MappedMemoryFile(filename);
            }
            return Instance;
        }

        // memory map a file to act as a long array
        public MappedMemoryFile(string filename)
        {
            // Define a minimum size for the memory-mapped file (2MB to ensure enough space)
            long minSize = 2 * 1024 * 1024; // 2MB

            // Open file and get its size
            using (var fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                _fileSize = fileStream.Length;
                if (_fileSize < minSize)
                {
                    // Resize the file to ensure it's large enough
                    fileStream.SetLength(minSize);
                    _fileSize = minSize;
                }

                // Create a memory-mapped file with the same size
                _mappedFile = MemoryMappedFile.CreateFromFile(
                    fileStream,
                    null, // No name
                    _fileSize,
                    MemoryMappedFileAccess.ReadWrite,
                    HandleInheritability.None,
                    false); // Leave the file open
            }

            // Create an accessor to read/write
            _accessor = _mappedFile.CreateViewAccessor(0, _fileSize);
            
            if (CmdArgs.GetInstance().VeryVerbose)
            {
                Console.WriteLine($"Memory mapped file initialized with size: {_fileSize} bytes");
            }
        }

        // Read a long at the specified position
        public long ReadLong(long position)
        {
            if (position < 0 || position + sizeof(long) > _fileSize)
                throw new ArgumentOutOfRangeException(nameof(position));

            _accessor.Read(position, out long value);
            return value;
        }

        // Write a long at the specified position
        public void WriteLong(long position, long value)
        {
            if (position < 0 || position + sizeof(long) > _fileSize)
                throw new ArgumentOutOfRangeException(nameof(position));

            _accessor.Write(position, value);
        }

        public static void WriteMemory(long position, long value)
        {
            // Change to use Common.Memory instead of direct Instance
            if (Common.Memory is MappedMemoryFile)
                ((MappedMemoryFile)Common.Memory).WriteLong(position, value);
            else
                Common.Memory.WriteLong(position, value);
        }

        public static long ReadMemory(long position)
        {
            // Change to use Common.Memory instead of direct Instance
            if (Common.Memory is MappedMemoryFile)
                return ((MappedMemoryFile)Common.Memory).ReadLong(position);
            else
                return Common.Memory.ReadLong(position);
        }
   

        // Improve Read method to handle non-register cases better
        public long Read(string register)
        {
            if (Common.Registers.Contains(register))
            {
                // Use this instead of Instance
                return this.ReadLong(Array.IndexOf(Common.Registers, register) * sizeof(long));
            }
            else if (register.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(register.Substring(1), out long address))
                {
                    // Ensure address is valid (convert to byte position)
                    long bytePosition = address * sizeof(long);
                    if (bytePosition >= 0 && bytePosition < _fileSize)
                    {
                        // Use this instead of Instance
                        return this.ReadLong(bytePosition);
                    }
                    else
                    {
                        throw new MASMException($"Memory address out of range: {register} (byte position {bytePosition}, file size {_fileSize})");
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

        // Improve Write method to handle memory addresses properly
        public void Write(string register, long value)
        {
            if (Common.Registers.Contains(register))
            {
                long position = Array.IndexOf(Common.Registers, register) * sizeof(long);
                // Use this instead of Instance
                this.WriteLong(position, value);
            }
            else if (register.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(register.Substring(1), out long address))
                {
                    // Ensure address is valid (convert to byte position)
                    long bytePosition = address * sizeof(long);
                    if (bytePosition >= 0 && bytePosition < _fileSize)
                    {
                        // Use this instead of Instance
                        this.WriteLong(bytePosition, value);
                    }
                    else
                    {
                        throw new MASMException($"Memory address out of range: {register} (byte position {bytePosition}, file size {_fileSize})");
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

        public void Write_String(string startingRegister, string value)
        {
            if (startingRegister.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(startingRegister.Substring(1), out long address))
                {
                    // Write each character of the string to memory
                    for (int i = 0; i < value.Length; i++)
                    {
                        WriteLong(address + i, value[i]);
                    }

                    // Add null terminator
                    WriteLong(address + value.Length, 0);
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

        public string Read_String(string startingRegister)
        {
            if (startingRegister.StartsWith("$"))
            {
                // Parse memory address without the $ sign
                if (long.TryParse(startingRegister.Substring(1), out long address))
                {
                    StringBuilder sb = new StringBuilder();
                    long currentAddress = address;

                    // Read characters until null terminator is encountered
                    while (true)
                    {
                        long charValue = ReadLong(currentAddress);
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

        // Indexer to use the file as an array
        public long this[long index]
        {
            get => ReadLong(index * sizeof(long));
            set => WriteLong(index * sizeof(long), value);
        }

        // Get number of longs that can fit in the file
        public long Length => _fileSize / sizeof(long);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _accessor?.Dispose();
                    _mappedFile?.Dispose();
                }
                _disposed = true;
            }
        }

        ~MappedMemoryFile()
        {
            Dispose(false);
        }
    }
}
