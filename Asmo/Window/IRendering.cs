using System;
using System.Numerics;
using Asmo.Gfx;

namespace Asmo.Window.Backend
{
    /// <summary>
    /// Abstracts the presentation of a software-populated framebuffer to the screen.
    /// </summary>
    public interface IRenderBackend : IDisposable
    {
        int FrameWidth { get; }
        int FrameHeight { get; }
        /// <summary>
        /// If the backend exposes a writable linear RGBA8 buffer (width*height*4), return it; otherwise null.
        /// </summary>
        Memory<byte>? TryGetFrameMemory();
        /// <summary>
        /// Resize underlying resources (texture, buffers) retaining content optionally (not required for first pass).
        /// </summary>
        void Resize(int width, int height);
        /// <summary>
        /// Upload the specified dirty rectangle from a CPU pixel provider to the GPU/texture.
        /// Provider returns a ReadOnlyMemory of the entire surface pixels in row-major RGBA form and stride (bytes per row).
        /// </summary>
        void CommitDirtyRect(int x, int y, int w, int h, Func<(ReadOnlyMemory<byte> data, int strideBytes)> pixelProvider);
        /// <summary>
        /// Present the current frame to the display (handles scaling, letterboxing, vsync semantics).
        /// </summary>
        void Present();
    }

    public interface IPlatformWindow : IDisposable
    {
        bool ShouldClose { get; }
        (int Width, int Height) ClientSize { get; }
        double GetFrameDeltaSeconds();
        void PollEvents();
        void SetTitle(string title);
    }
}
