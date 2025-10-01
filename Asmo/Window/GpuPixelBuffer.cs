using System;
using System.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace Asmo.Window;

/// <summary>
/// Persistent mapped PBO backend for pixel buffer uploads (Level 1, Phase P1: full-frame upload only).
/// </summary>
public sealed class GpuPixelBuffer : IPixelBufferBackend
{
    private int _width, _height;
    private int _pbo = 0;
    private int _texture;
    private IntPtr _mappedPtr = IntPtr.Zero;
    private Memory<byte> _frameMemory;
    private int _bufferSize;

    public int Width => _width;
    public int Height => _height;
    public PixelBufferBackend Mode => PixelBufferBackend.PersistentMapped;
    public Memory<byte> FrameMemory => _frameMemory;
    public Span<byte> GetSpan() => _frameMemory.Span;

    public GpuPixelBuffer(int width, int height, int textureHandle)
    {
        _width = width;
        _height = height;
        _texture = textureHandle;
        AllocatePbo();
    }

    private void AllocatePbo()
    {
        _bufferSize = _width * _height * 4;
        _pbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
        GL.BufferStorage(BufferTarget.PixelUnpackBuffer, _bufferSize, IntPtr.Zero,
            BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit);
        // Use the non-obsolete overload for MapBufferRange
        _mappedPtr = GL.MapBufferRange(BufferTarget.PixelUnpackBuffer, IntPtr.Zero, _bufferSize,
            BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);
        // Wrap mapped pointer in Memory<byte> using MemoryManager
        _frameMemory = new UnmanagedMemoryManager(_mappedPtr, _bufferSize).Memory;
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
    }

    public void CommitDirty(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        // Clamp (defensive)
        if (x < 0) { w += x; x = 0; }
        if (y < 0) { h += y; y = 0; }
        if (x + w > _width) w = _width - x;
        if (y + h > _height) h = _height - y;
        if (w <= 0 || h <= 0) return;

        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);

        // Full frame fast path
        if (x == 0 && y == 0 && w == _width && h == _height)
        {
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _width, _height, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        }
        else
        {
            // Set row length to full texture width so we can use pointer offset into mapped buffer
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, _width);
            int byteOffset = (y * _width + x) * 4;
            IntPtr ptr = IntPtr.Add(IntPtr.Zero, byteOffset); // with PBO bound, this acts as offset
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            // Reset row length to default
            GL.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
        }
        GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
    }

    public void Resize(int width, int height)
    {
        if (width == _width && height == _height) return;
        Dispose();
        _width = width;
        _height = height;
        AllocatePbo();
    }

    public void Dispose()
    {
        if (_pbo != 0)
        {
            if (_mappedPtr != IntPtr.Zero)
            {
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
                GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                _mappedPtr = IntPtr.Zero;
            }
            GL.DeleteBuffer(_pbo);
            _pbo = 0;
        }
    }

    // Helper: wraps an unmanaged pointer as Memory<byte>
    // Requires project to be built with /unsafe
    private sealed unsafe class UnmanagedMemoryManager : MemoryManager<byte>
    {
        private readonly IntPtr _ptr;
        private readonly int _length;
        public UnmanagedMemoryManager(IntPtr ptr, int length) { _ptr = ptr; _length = length; }
        public override Span<byte> GetSpan() => new Span<byte>((void*)_ptr, _length);
        public override MemoryHandle Pin(int elementIndex = 0) => new MemoryHandle(((byte*)_ptr) + elementIndex);
        public override void Unpin() { }
        protected override void Dispose(bool disposing) { }
    }
}
