using System;
using System.Buffers;

namespace Asmo.Window;

public enum PixelBufferBackend
{
    LegacyCpuCopy,
    PersistentMapped
}

/// <summary>
/// Abstraction over a frame-sized RGBA pixel buffer upload backend.
/// Level 1 includes a CPU copy backend and (later) a persistent mapped GPU backend.
/// </summary>
public interface IPixelBufferBackend : IDisposable
{
    int Width { get; }
    int Height { get; }
    PixelBufferBackend Mode { get; }

    /// <summary>
    /// Writable RGBA8 buffer (length = Width * Height * 4).
    /// </summary>
    Memory<byte> FrameMemory { get; }

    /// <summary>
    /// Convenience accessor for hot paths.
    /// </summary>
    Span<byte> GetSpan();

    /// <summary>
    /// Mark / upload a dirty rectangle to the GPU. For the CPU backend this will perform the
    /// existing conversion + glTex(Sub)Image2D calls. For the GPU persistent backend this will
    /// bind the PBO and issue glTexSubImage2D with an offset pointer.
    /// </summary>
    void CommitDirty(int x, int y, int w, int h);

    /// <summary>
    /// Resize buffer; guarantees FrameMemory reallocated/remapped and contents undefined after.
    /// </summary>
    void Resize(int width, int height);
}
