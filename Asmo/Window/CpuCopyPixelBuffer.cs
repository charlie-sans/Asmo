using System;
using OpenTK.Graphics.OpenGL4;
using Asmo.Gfx;
using Asmo.Types;

namespace Asmo.Window;

/// <summary>
/// Legacy CPU path implementing IPixelBufferBackend. Uses a managed byte[] staging buffer and
/// per-dirty-rect conversion from the engine's Color[][] surface layout into packed RGBA8.
/// This preserves current behavior while conforming to the new abstraction.
/// </summary>
public sealed class CpuCopyPixelBuffer : IPixelBufferBackend
{
    private byte[] _buffer; // packed RGBA8
    private readonly int _texture; // GL texture handle owned by Window (passed in)
    private Color[][]? _sourcePixels; // optional source binding (Surface.Pixels)

    public int Width { get; private set; }
    public int Height { get; private set; }
    public PixelBufferBackend Mode => PixelBufferBackend.LegacyCpuCopy;

    public CpuCopyPixelBuffer(int width, int height, int textureHandle)
    {
        Width = width;
        Height = height;
        _texture = textureHandle;
        _buffer = new byte[width * height * 4];
    }

    /// <summary>
    /// Attach the current frame's source pixel matrix. This allows avoiding re-wiring Surface yet.
    /// Must be called each frame before CommitDirty if using Surface.Pixels.
    /// </summary>
    public void BindSource(Color[][] pixels)
    {
        _sourcePixels = pixels;
    }

    public Memory<byte> FrameMemory => _buffer;
    public Span<byte> GetSpan() => _buffer.AsSpan();

    public void CommitDirty(int x, int y, int w, int h)
    {
        if (w <= 0 || h <= 0) return;
        if (_sourcePixels == null) return; // nothing to upload

        // Clamp
        if (x < 0) { w += x; x = 0; }
        if (y < 0) { h += y; y = 0; }
        if (x + w > Width) w = Width - x;
        if (y + h > Height) h = Height - y;
        if (w <= 0 || h <= 0) return;

        // Convert dirty rect into packed linear RGBA rows (top-left origin expected by GL here)
        // Current engine uses Pixels[x][y] indexing with origin top-left? The upload in Window flips vertically.
        // For now we replicate existing flip behavior by writing rows in the same order Window currently copies.
        for (int py = 0; py < h; py++)
        {
            int srcY = y + py;
            int flippedY = Height - 1 - srcY; // replicate existing vertical flip
            if (flippedY < 0 || flippedY >= Height) continue;
            for (int px = 0; px < w; px++)
            {
                int srcX = x + px;
                if (srcX < 0 || srcX >= Width) continue;
                var c = _sourcePixels![srcX][flippedY];
                int dstIndex = (py * w + px) * 4;
                _buffer[dstIndex + 0] = (byte)c.R;
                _buffer[dstIndex + 1] = (byte)c.G;
                _buffer[dstIndex + 2] = (byte)c.B;
                _buffer[dstIndex + 3] = (byte)c.A;
            }
        }

        // Upload dirty region
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        var handle = System.Runtime.InteropServices.GCHandle.Alloc(_buffer, System.Runtime.InteropServices.GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }
        finally
        {
            handle.Free();
        }
    }

    public void Resize(int width, int height)
    {
        if (width == Width && height == Height) return;
        Width = width;
        Height = height;
        _buffer = new byte[width * height * 4];
        // Resize texture storage (full frame reallocation)
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
    }

    public void Dispose()
    {
        // Nothing to dispose besides clearing references; texture lifetime managed by Window
        _sourcePixels = null;
    }
}
