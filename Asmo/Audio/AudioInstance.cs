using System;
using System.Threading;

namespace Asmo.Audio
{
    internal sealed class AudioInstance
    {
    private readonly AudioClip _clip;
    private int _samplePosition;
    private bool _isPlaying = true;
    private float _volume;
    private float _pan;
    private float _baseVolume;
    private float _targetVolume;
    private float _fadeStartVolume;
        private double _fadeTimeRemaining;
        private double _fadeDuration;
        private bool _loggedStop;

        public AudioInstance(AudioClip clip, bool loop, float volume, float pan)
        {
            _clip = clip ?? throw new ArgumentNullException(nameof(clip));
            Loop = loop;
            Volume = volume;
            _baseVolume = volume;
            _targetVolume = volume;
            _fadeStartVolume = volume;
            Pan = pan;
            AudioDiagnostics.Log($"Instance created (loop={loop}, volume={volume:0.00}, pan={pan:0.00}, samples={clip.TotalSamples}).");
        }

        public bool Loop { get; }

        public float Volume
        {
            get => Volatile.Read(ref _volume);
            private set => Volatile.Write(ref _volume, value);
        }

        public float Pan
        {
            get => Volatile.Read(ref _pan);
            private set => Volatile.Write(ref _pan, value);
        }

        public bool IsPlaying => Volatile.Read(ref _isPlaying);

        public void Stop()
        {
            Volatile.Write(ref _isPlaying, false);
            if (!_loggedStop)
            {
                AudioDiagnostics.Log("Instance stopped.");
                _loggedStop = true;
            }
        }

        public void SetVolume(float volume)
        {
            _baseVolume = Math.Clamp(volume, 0f, 4f);
            Volume = _baseVolume;
            _targetVolume = _baseVolume;
            _fadeTimeRemaining = 0;
            AudioDiagnostics.Log($"Instance volume set to {Volume:0.00}.");
        }

        public void SetPan(float pan)
        {
            Pan = Math.Clamp(pan, -1f, 1f);
            AudioDiagnostics.Log($"Instance pan set to {Pan:0.00}.");
        }

        public void FadeTo(float targetVolume, double durationSeconds)
        {
            _targetVolume = Math.Clamp(targetVolume, 0f, 4f);
            _fadeDuration = Math.Max(0.0001, durationSeconds);
            _fadeTimeRemaining = _fadeDuration;
            _fadeStartVolume = Volume;
            AudioDiagnostics.Log($"Instance fading to {_targetVolume:0.00} over {_fadeDuration:0.000}s.");
        }

        public void Update(double deltaTime)
        {
            if (!IsPlaying)
                return;

            if (_fadeTimeRemaining > 0)
            {
                _fadeTimeRemaining -= deltaTime;
                double progress = 1.0 - Math.Max(0, _fadeTimeRemaining) / _fadeDuration;
                Volume = (float)(_fadeStartVolume + (_targetVolume - _fadeStartVolume) * progress);
                if (_fadeTimeRemaining <= 0)
                {
                    _fadeTimeRemaining = 0;
                    Volume = _targetVolume;
                    AudioDiagnostics.Log($"Instance fade target reached ({Volume:0.00}).");
                }
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            if (!IsPlaying)
            {
                Array.Clear(buffer, offset, count);
                return 0;
            }

            int samplesWritten = 0;
            while (samplesWritten < count)
            {
                int remainingSamples = _clip.TotalSamples - _samplePosition;
                if (remainingSamples <= 0)
                {
                    if (Loop)
                    {
                        _samplePosition = 0;
                        remainingSamples = _clip.TotalSamples;
                        AudioDiagnostics.Log("Instance loop restart.");
                    }
                    else
                    {
                        Volatile.Write(ref _isPlaying, false);
                        if (!_loggedStop)
                        {
                            AudioDiagnostics.Log("Instance reached end of clip.");
                            _loggedStop = true;
                        }
                        Array.Clear(buffer, offset + samplesWritten, count - samplesWritten);
                        break;
                    }
                }

                int toCopy = Math.Min(remainingSamples, count - samplesWritten);
                Array.Copy(_clip.SampleBuffer, _samplePosition, buffer, offset + samplesWritten, toCopy);
                _samplePosition += toCopy;
                samplesWritten += toCopy;
            }

            if (samplesWritten > 0)
            {
                float localMin = float.MaxValue;
                float localMax = float.MinValue;
                for (int i = 0; i < samplesWritten; i++)
                {
                    float s = buffer[offset + i];
                    if (s < localMin) localMin = s;
                    if (s > localMax) localMax = s;
                }
                AudioDiagnostics.Log($"Instance read {samplesWritten} samples (loop={Loop}, position={_samplePosition}, volume={Volume:0.00}, min={localMin:0.0000}, max={localMax:0.0000}).");
            }
            else
            {
                AudioDiagnostics.Log($"Instance read returned 0 samples (loop={Loop}, isPlaying={IsPlaying}, position={_samplePosition}, total={_clip.TotalSamples}).");
            }

            return samplesWritten;
        }
    }
}
