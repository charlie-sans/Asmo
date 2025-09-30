using System;
using CSCore;
using CSCore.Streams;

namespace Asmo.Audio
{
    // A streaming sample source that pulls audio from a callback (e.g., your mixer)
    public class StreamingSampleSource : ISampleSource
    {
        private readonly Func<float[], int, int, int> _fillBuffer;
        public WaveFormat WaveFormat { get; }
        public bool CanSeek => false;
        public long Position { get => 0; set { } }
        public long Length => 0; // Unknown/streaming
        public bool CanPause => false;

        // fillBuffer: (buffer, offset, count) => samples written
        public StreamingSampleSource(WaveFormat format, Func<float[], int, int, int> fillBuffer)
        {
            WaveFormat = format;
            _fillBuffer = fillBuffer;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            return _fillBuffer(buffer, offset, count);
        }

        public void Dispose() { }
    }
}
