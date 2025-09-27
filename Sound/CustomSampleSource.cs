using System;
using CSCore;

public class CustomSampleSource : ISampleSource
{
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly long _totalSamples;
    private long _position;
    private readonly Func<long, float> _sampleFunc;

    public CustomSampleSource(Func<long, float> sampleFunc, float durationSeconds, int sampleRate = 44100, int channels = 1)
    {
        _sampleFunc = sampleFunc;
        _sampleRate = sampleRate;
        _channels = channels;
        _totalSamples = (long)(durationSeconds * sampleRate);
        _position = 0;
        WaveFormat = new WaveFormat(sampleRate, 32, channels, AudioEncoding.IeeeFloat);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = 0;
        for (int n = 0; n < count && _position < _totalSamples; n++, _position++)
        {
            buffer[offset + n] = _sampleFunc(_position);
            samplesRead++;
        }
        return samplesRead;
    }

    public bool CanSeek => false;
    public WaveFormat WaveFormat { get; }
    public long Position { get => _position; set => _position = value; }
    public long Length => _totalSamples;
    public void Dispose() { }
}

public static class SoundSynth
{
    public static CustomSampleSource WhiteNoise(float durationSeconds, float amplitude = 0.2f, int sampleRate = 44100)
    {
        var rand = new Random();
        return new CustomSampleSource(
            _ => (float)(amplitude * (2.0 * rand.NextDouble() - 1.0)),
            durationSeconds, sampleRate);
    }

    public static CustomSampleSource DrumKick(float durationSeconds, float startFreq = 120.0f, float endFreq = 40.0f, float amplitude = 0.5f, int sampleRate = 44100)
    {
        // Simple exponential pitch drop and exponential decay (ADSR)
        return new CustomSampleSource(
            pos =>
            {
                float t = pos / sampleRate;
                float freq = startFreq * (float)Math.Pow(endFreq / startFreq, t / durationSeconds);
                float env = (float)Math.Exp(-4.0 * t / durationSeconds); // Fast decay
                return (float)(Math.Sin(2 * Math.PI * freq * t) * env * amplitude);
            },
            durationSeconds, sampleRate);
    }

    public static CustomSampleSource FromArray(float[] samples, int sampleRate = 44100)
    {
        return new CustomSampleSource(
            pos => pos < samples.Length ? samples[pos] : 0f,
            samples.Length / (float)sampleRate, sampleRate);
    }
}