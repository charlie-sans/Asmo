using System;
using CSCore;

public class SimpleSquareWaveSource : ISampleSource
{
    private readonly int _sampleRate;
    private readonly float _frequency;
    private readonly float _amplitude;
    private readonly long _totalSamples;
    private long _position;

    public SimpleSquareWaveSource(float frequency, float durationSeconds, float amplitude = 0.2f, int sampleRate = 44100)
    {
        _frequency = frequency;
        _amplitude = amplitude;
        _sampleRate = sampleRate;
        _totalSamples = (long)(durationSeconds * sampleRate);
        _position = 0;
        WaveFormat = new WaveFormat(sampleRate, 32, 1, AudioEncoding.IeeeFloat);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = 0;
        for (int n = 0; n < count && _position < _totalSamples; n++, _position++)
        {
            float t = (float)_position / _sampleRate;
            float value = MathF.Sign(MathF.Sin(2 * MathF.PI * _frequency * t)) * _amplitude;
            buffer[offset + n] = value;
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
