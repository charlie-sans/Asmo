using System;
using System.Collections.Generic;
// CSCore removed; this class now only supports procedural generation and simple PCM16 WAV loading.
using System.IO;
using System.Buffers.Binary;

namespace Asmo.Audio
{
    /// <summary>
    /// Represents decoded PCM audio data that can be reused for multiple playbacks.
    /// </summary>
    public sealed class AudioClip
    {
        public const int DefaultSampleRate = 44100;
        public const int DefaultChannels = 2;

        private readonly float[] _samples;

        // Internal so procedural synthesis utilities in the same assembly can construct clips.
        internal AudioClip(float[] samples, int sampleRate, int channels)
        {
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));
            if (sampleRate <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRate));

            _samples = samples ?? throw new ArgumentNullException(nameof(samples));
            SampleRate = sampleRate;
            Channels = channels;
        }

        public int SampleRate { get; }
        public int Channels { get; }
    /// <summary>
    /// Total number of samples in the clip (all channels).
    /// </summary>
    public int TotalSamples => _samples.Length;

    // Internal direct access used by mixer / synthesis. Avoid exposing publicly to keep mutability contained.
    internal float[] Samples => _samples;

    /// <summary>
    /// For diagnostics: returns a copy of the sample buffer (do not use for playback).
    /// </summary>
    public float[] GetSampleBuffer() => (float[])_samples.Clone();

        /// <summary>
        /// Loads a 16-bit little-endian PCM WAV file (mono or stereo). Resamples / re-channels if needed.
        /// </summary>
        public static AudioClip Load(string path, int targetSampleRate = DefaultSampleRate, int targetChannels = DefaultChannels)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required", nameof(path));
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);
            // Minimal WAV parser
            if (new string(br.ReadChars(4)) != "RIFF") throw new InvalidDataException("Not RIFF");
            br.ReadInt32(); // file size
            if (new string(br.ReadChars(4)) != "WAVE") throw new InvalidDataException("Not WAVE");
            // fmt chunk
            if (new string(br.ReadChars(4)) != "fmt ") throw new InvalidDataException("Missing fmt");
            int fmtSize = br.ReadInt32();
            short audioFormat = br.ReadInt16();
            short channels = br.ReadInt16();
            int sampleRate = br.ReadInt32();
            br.ReadInt32(); // byte rate
            br.ReadInt16(); // block align
            short bitsPerSample = br.ReadInt16();
            if (fmtSize > 16) br.ReadBytes(fmtSize - 16);
            if (audioFormat != 1 || (bitsPerSample != 16)) throw new InvalidDataException("Only PCM16 supported");
            // find data chunk
            string chunkId;
            int dataSize = 0;
            while (true)
            {
                chunkId = new string(br.ReadChars(4));
                int size = br.ReadInt32();
                if (chunkId == "data") { dataSize = size; break; }
                br.ReadBytes(size);
            }
            int frames = dataSize / (channels * (bitsPerSample / 8));
            short[] pcm = new short[frames * channels];
            byte[] raw = br.ReadBytes(dataSize);
            Buffer.BlockCopy(raw, 0, pcm, 0, dataSize);
            float[] floats = new float[pcm.Length];
            for (int i = 0; i < pcm.Length; i++) floats[i] = pcm[i] / 32768f;
            if (channels != targetChannels) floats = ConvertChannels(floats, channels, targetChannels);
            if (sampleRate != targetSampleRate) floats = Resample(floats, sampleRate, targetSampleRate, targetChannels);
            return new AudioClip(floats, targetSampleRate, targetChannels);
        }

        /// <summary>
        /// Generates a sine wave clip procedurally.
        /// </summary>
        // public static AudioClip CreateSine(double frequency, double durationSeconds, float amplitude = 0.5f, int sampleRate = DefaultSampleRate)
        //     => GenerateProcedural((t, _) => (float)(amplitude * Math.Sin(2 * Math.PI * frequency * t)), durationSeconds, sampleRate, DefaultChannels);

        /// <summary>
        /// Generates a square wave clip procedurally.
        /// </summary>
        // public static AudioClip CreateSquare(double frequency, double durationSeconds, float amplitude = 0.4f, int sampleRate = DefaultSampleRate)
        //     => GenerateProcedural((t, _) => MathF.Sign(MathF.Sin((float)(2 * Math.PI * frequency * t))) * amplitude, durationSeconds, sampleRate, DefaultChannels);

        /// <summary>
        /// Generates white noise clip procedurally.
        /// </summary>
        // public static AudioClip CreateNoise(double durationSeconds, float amplitude = 0.2f, int sampleRate = DefaultSampleRate)
        // {
        //     var random = new Random();
        //     return GenerateProcedural((_, __) => (float)((random.NextDouble() * 2.0 - 1.0) * amplitude), durationSeconds, sampleRate, DefaultChannels);
        // }

        public static AudioClip CreateSine(double frequency, double durationSeconds, float amplitude = 0.5f, int sampleRate = DefaultSampleRate, int channels = DefaultChannels)
            => GenerateProcedural((t, _) => (float)(amplitude * Math.Sin(2 * Math.PI * frequency * t)), durationSeconds, sampleRate, channels);

        public static AudioClip CreateSquare(double frequency, double durationSeconds, float amplitude = 0.4f, int sampleRate = DefaultSampleRate, int channels = DefaultChannels)
            => GenerateProcedural((t, _) => MathF.Sign(MathF.Sin((float)(2 * Math.PI * frequency * t))) * amplitude, durationSeconds, sampleRate, channels);

        public static AudioClip CreateNoise(double durationSeconds, float amplitude = 0.2f, int sampleRate = DefaultSampleRate, int channels = DefaultChannels)
        {
            var random = new Random();
            return GenerateProcedural((_, __) => (float)((random.NextDouble() * 2.0 - 1.0) * amplitude), durationSeconds, sampleRate, channels);
        }

        private static AudioClip GenerateProcedural(Func<double, int, float> generator, double durationSeconds, int sampleRate, int channels)
        {
            if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (channels <= 0) throw new ArgumentOutOfRangeException(nameof(channels));

            int totalFrames = (int)Math.Ceiling(durationSeconds * sampleRate);
            float[] data = new float[totalFrames * channels];
            for (int frame = 0; frame < totalFrames; frame++)
            {
                double t = frame / (double)sampleRate;
                float value = generator(t, frame);
                int leftIndex = frame * channels;
                for (int ch = 0; ch < channels; ch++)
                {
                    data[leftIndex + ch] = value;
                }
            }
            return new AudioClip(data, sampleRate, channels);
        }

        // Removed generic sample source ingestion (CSCore). WAV loader + procedural remain.

        private static float[] ConvertChannels(float[] samples, int inputChannels, int outputChannels)
        {
            if (inputChannels == outputChannels)
                return samples;

            if (inputChannels <= 0)
                throw new ArgumentOutOfRangeException(nameof(inputChannels));

            if (outputChannels <= 0)
                throw new ArgumentOutOfRangeException(nameof(outputChannels));

            int frameCount = samples.Length / inputChannels;
            float[] result = new float[frameCount * outputChannels];

            for (int frame = 0; frame < frameCount; frame++)
            {
                int inputOffset = frame * inputChannels;
                int outputOffset = frame * outputChannels;

                if (outputChannels == 1)
                {
                    // Down-mix by averaging
                    float sum = 0f;
                    for (int ch = 0; ch < inputChannels; ch++)
                    {
                        sum += samples[inputOffset + ch];
                    }
                    result[outputOffset] = sum / inputChannels;
                }
                else if (inputChannels == 1)
                {
                    float value = samples[inputOffset];
                    for (int ch = 0; ch < outputChannels; ch++)
                    {
                        result[outputOffset + ch] = value;
                    }
                }
                else
                {
                    // Copy as many channels as available up to the target channel count.
                    for (int ch = 0; ch < outputChannels; ch++)
                    {
                        float value = samples[inputOffset + Math.Min(ch, inputChannels - 1)];
                        result[outputOffset + ch] = value;
                    }
                }
            }

            return result;
        }

        private static float[] Resample(float[] samples, int sourceSampleRate, int targetSampleRate, int channels)
        {
            if (sourceSampleRate == targetSampleRate)
                return samples;

            if (channels <= 0)
                throw new ArgumentOutOfRangeException(nameof(channels));

            int sourceFrames = samples.Length / channels;
            double ratio = targetSampleRate / (double)sourceSampleRate;
            int targetFrames = Math.Max(1, (int)Math.Round(sourceFrames * ratio));
            float[] resampled = new float[targetFrames * channels];

            for (int frame = 0; frame < targetFrames; frame++)
            {
                double sourcePosition = frame / ratio;
                int index = (int)Math.Floor(sourcePosition);
                double frac = sourcePosition - index;
                int nextIndex = Math.Min(index + 1, sourceFrames - 1);

                for (int ch = 0; ch < channels; ch++)
                {
                    var sampleA = samples[(index * channels) + ch];
                    var sampleB = samples[(nextIndex * channels) + ch];
                    resampled[(frame * channels) + ch] = (float)(sampleA + (sampleB - sampleA) * frac);
                }
            }

            return resampled;
        }
    }
}
