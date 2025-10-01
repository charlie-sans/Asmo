using System;

namespace SharpMASM
{
    /// <summary>
    /// Interface defining memory operations for SharpMASM
    /// </summary>
    public interface IMemoryManager : IDisposable
    {
        // Basic read/write operations
        long ReadLong(long position);
        void WriteLong(long position, long value);
        
        // Register/address operations
        long Read(string register);
        void Write(string register, long value);
        
        // String operations
        string Read_String(string startingRegister);
        void Write_String(string startingRegister, string value);
        
        // Array-like access
        long this[long index] { get; set; }
        
        // Size information
        long Length { get; }
    }
}
