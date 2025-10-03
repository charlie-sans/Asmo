using System;
using System.Collections.Generic;
using System.Text;

namespace Asmo.Audio
{
    internal sealed class AudioMixerSource
    {
        private readonly List<AudioBus> _rootBuses = new();
        private readonly object _lock = new();
        public int SampleRate { get; }
        public int Channels { get; }

        public AudioMixerSource(int sampleRate, int channels)
        {
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
            SampleRate = sampleRate;
            Channels = channels;
            AudioDiagnostics.Log($"AudioMixerSource created (sampleRate={sampleRate}, channels={channels}).");
        }
        
        public bool CanSeek => false;

        public void AddRootBus(AudioBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            lock (_lock)
            {
                _rootBuses.Add(bus);
            }
            AudioDiagnostics.Log($"AudioMixerSource added root bus '{bus.Name}'.");
        }

        public int Read(float[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            lock (_lock)
            {
                foreach (var bus in _rootBuses)
                {
                    bus.Mix(buffer, offset, count, Channels, 1f, 0f);
                }
            }
            // Light headroom (prevent hard clipping); scale instead of non-linear soft clip to reduce added harmonics.
            const float headroom = 0.9f;
            for (int i = 0; i < count; i++)
            {
                float s = buffer[offset + i] * headroom;
                if (s > 1f) s = 1f; else if (s < -1f) s = -1f;
                buffer[offset + i] = s;
            }
            LogBuffer(buffer, offset, count, Channels);
            return count;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _rootBuses.Clear();
            }
            AudioDiagnostics.Log("AudioMixerSource disposed.");
        }

        private static void LogBuffer(float[] buffer, int offset, int count, int channels)
        {
            if (!AudioDiagnostics.Enabled || count <= 0)
                return;

            float min = float.MaxValue;
            float max = float.MinValue;
            double sumSquares = 0;

            for (int i = 0; i < count; i++)
            {
                float sample = buffer[offset + i];
                if (sample < min) min = sample;
                if (sample > max) max = sample;
                sumSquares += sample * sample;
            }

            double rms = count > 0 ? Math.Sqrt(sumSquares / count) : 0;

            var sb = new StringBuilder();
            int frames = channels > 0 ? count / channels : count;
            int framesToShow = Math.Min(4, frames);
            for (int frame = 0; frame < framesToShow; frame++)
            {
                if (frame > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append('[');
                for (int ch = 0; ch < channels; ch++)
                {
                    if (ch > 0)
                        sb.Append(", ");
                    int index = offset + frame * channels + ch;
                    if (index < buffer.Length)
                    {
                        sb.Append(buffer[index].ToString("0.0000"));
                    }
                }
                sb.Append(']');
            }

            AudioDiagnostics.Log($"Mixer buffer summary: count={count}, channels={channels}, min={min:0.0000}, max={max:0.0000}, rms={rms:0.0000}, firstFrames={sb}.");
        }
    }
}
