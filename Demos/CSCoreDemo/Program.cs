using System;
using CSCore;
using CSCore.SoundOut;
using CSCore.Streams;

namespace CSCoreDemo
{
    class Program
    {
    static void Main(string[] args)
        {
            int sampleRate = 44100;
            int channels = 2;
            int seconds = 2;
            int totalSamples = sampleRate * seconds;
            float frequency = 440f; // A4

            // Generate a simple sine wave
            float[] buffer = new float[totalSamples * channels];
            for (int i = 0; i < totalSamples; i++)
            {
                float sample = (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
                for (int ch = 0; ch < channels; ch++)
                {
                    buffer[i * channels + ch] = sample;
                }
            }

                // 1. Play direct (reference)
                var waveSource = new FloatToPcm16WaveSource(buffer, sampleRate, channels);
                using (var soundOut = new WasapiOut())
                {
                    soundOut.Initialize(waveSource);
                    soundOut.Play();
                    Console.WriteLine("Playing 2s sine wave at 440Hz (direct)...");
                    while (soundOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

                // 2. Play through a ring buffer and background thread (engine simulation)
                int ringBufferSize = buffer.Length * 2; // 2x the buffer for safety
                var ringBuffer = new FloatRingBuffer(ringBufferSize);
                var ringWaveSource = new FloatToPcm16WaveSourceRing(ringBuffer, sampleRate, channels);
                bool threadRunning = true;
                var fillThread = new System.Threading.Thread(() =>
                {
                    int pos = 0;
                    while (threadRunning && pos < buffer.Length)
                    {
                        int chunk = Math.Min(4096, buffer.Length - pos);
                        ringBuffer.Write(buffer, pos, chunk);
                        pos += chunk;
                        System.Threading.Thread.Sleep(2);
                    }
                });
                fillThread.IsBackground = true;
                fillThread.Start();

                using (var soundOut2 = new WasapiOut())
                {
                    soundOut2.Initialize(ringWaveSource);
                    soundOut2.Play();
                    Console.WriteLine("Playing 2s sine wave at 440Hz (engine pipeline)...");
                    while (soundOut2.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                threadRunning = false;
                fillThread.Join();
        }
    }
    // Simple float ring buffer for demo
    class FloatRingBuffer
    {
        private readonly float[] _buffer;
        private int _write;
        private int _read;
        private int _count;
        public int Capacity => _buffer.Length;
        public int Count => _count;
        public FloatRingBuffer(int size) { _buffer = new float[size]; }
        public int Write(float[] src, int offset, int count)
        {
            int written = 0;
            for (int i = 0; i < count && _count < _buffer.Length; i++)
            {
                _buffer[_write] = src[offset + i];
                _write = (_write + 1) % _buffer.Length;
                _count++;
                written++;
            }
            return written;
        }
        public int Read(float[] dst, int offset, int count)
        {
            int read = 0;
            for (int i = 0; i < count && _count > 0; i++)
            {
                dst[offset + i] = _buffer[_read];
                _read = (_read + 1) % _buffer.Length;
                _count--;
                read++;
            }
            return read;
        }
    }

    // IWaveSource that reads from a FloatRingBuffer
    class FloatToPcm16WaveSourceRing : IWaveSource
    {
        private readonly FloatRingBuffer _buffer;
        private readonly int _channels;
        public WaveFormat WaveFormat { get; }
        public long Length => 0;
        public long Position { get => 0; set { } }
        public bool CanSeek => false;
        public FloatToPcm16WaveSourceRing(FloatRingBuffer buffer, int sampleRate, int channels)
        {
            _buffer = buffer;
            _channels = channels;
            WaveFormat = new WaveFormat(sampleRate, 16, channels, AudioEncoding.Pcm);
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesToRead = count / 2;
            float[] temp = new float[samplesToRead];
            int samples = _buffer.Read(temp, 0, samplesToRead);
            for (int i = 0; i < samples; i++)
            {
                short pcm = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, temp[i] * 32767f));
                buffer[offset + i * 2] = (byte)(pcm & 0xFF);
                buffer[offset + i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }
            return samples * 2;
        }
        public void Dispose() { }
    }

    // Simple ISampleSource for a float[] buffer
    // Converts a float[] buffer to 16-bit PCM for CSCore playback
    class FloatToPcm16WaveSource : IWaveSource
    {
        private readonly float[] _buffer;
        private int _position;
        public WaveFormat WaveFormat { get; }
        public long Length { get; }
        public long Position { get => _position * 2; set => _position = (int)(value / 2); }
        public bool CanSeek => true;
        public FloatToPcm16WaveSource(float[] buffer, int sampleRate, int channels)
        {
            _buffer = buffer;
            WaveFormat = new WaveFormat(sampleRate, 16, channels, AudioEncoding.Pcm);
            Length = buffer.Length * 2; // 2 bytes per sample
        }
        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesToRead = Math.Min(count / 2, _buffer.Length - _position);
            for (int i = 0; i < samplesToRead; i++)
            {
                short pcm = (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, _buffer[_position + i] * 32767f));
                buffer[offset + i * 2] = (byte)(pcm & 0xFF);
                buffer[offset + i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
            }
            _position += samplesToRead;
            return samplesToRead * 2;
        }
        public void Dispose() { }
    }
}
