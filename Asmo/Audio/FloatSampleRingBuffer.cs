using System;
using CSCore;
using CSCore.Streams;

namespace Asmo.Audio
{
    // Simple ring buffer for float samples (per channel)
    public class FloatSampleRingBuffer
    {
        private readonly float[] _buffer;
        private int _writePos;
        private int _readPos;
        private int _count;
        private readonly int _channels;

        public int Capacity { get; }
        public int Count => _count;

        public FloatSampleRingBuffer(int capacity, int channels)
        {
            Capacity = capacity;
            _channels = channels;
            _buffer = new float[capacity * channels];
        }

        public int Write(float[] src, int offset, int count)
        {
            int written = 0;
            for (int i = 0; i < count; i++)
            {
                if (_count >= Capacity * _channels) break;
                _buffer[_writePos] = src[offset + i];
                _writePos = (_writePos + 1) % _buffer.Length;
                _count++;
                written++;
            }
            return written;
        }

        public int Read(float[] dst, int offset, int count)
        {
            int read = 0;
            for (int i = 0; i < count; i++)
            {
                if (_count == 0) break;
                dst[offset + i] = _buffer[_readPos];
                _readPos = (_readPos + 1) % _buffer.Length;
                _count--;
                read++;
            }
            return read;
        }
    }
}
