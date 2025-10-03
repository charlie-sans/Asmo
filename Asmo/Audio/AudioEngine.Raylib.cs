using System;
using System.Collections.Generic;
using System.Threading;
using Raylib_cs;
// CSCore removed; using internal mixer only

namespace Asmo.Audio
{
#if USE_RAYLIB_AUDIO
    // Raylib-backed implementation replacing WasapiOut/CSCore output while reusing existing mixing classes.
    public sealed class AudioEngine : IDisposable
    {
    private readonly AudioMixerSource _mixer;
    private readonly int _framesPerUpdate;
        private Thread? _audioThread;
        private bool _audioThreadRunning;
        private bool _disposed;
        private readonly Dictionary<string, AudioBus> _buses = new(StringComparer.OrdinalIgnoreCase);
        public AudioBus MasterBus { get; }
        private readonly int _sampleRate;
        private readonly int _channels;
        private AudioStream _stream;
        private bool _streamStarted;

        public static bool DiagnosticsEnabled
        {
            get => AudioDiagnostics.Enabled;
            set => AudioDiagnostics.Enabled = value;
        }

        public static Action<string> DiagnosticsSink
        {
            get => AudioDiagnostics.LogSink;
            set => AudioDiagnostics.LogSink = value;
        }

        public AudioEngine(int sampleRate = AudioClip.DefaultSampleRate, int channels = AudioClip.DefaultChannels, int framesPerUpdate = 0)
        {
            _sampleRate = sampleRate;
            _channels = channels;
            if (framesPerUpdate <= 0)
            {
                // Allow override via env; default to 2048 for lower callback pressure (fewer underruns) now that latency isn't mission critical for tracker playback.
                if (!int.TryParse(Environment.GetEnvironmentVariable("ASMO_AUDIO_FRAMES"), out framesPerUpdate) || framesPerUpdate <= 0)
                    framesPerUpdate = 2048;
            }
            _framesPerUpdate = framesPerUpdate;
            if (!Raylib.IsAudioDeviceReady()) Raylib.InitAudioDevice();
            try { Raylib.SetAudioStreamBufferSizeDefault(framesPerUpdate); } catch { }
            _mixer = new AudioMixerSource(sampleRate, channels);
            MasterBus = new AudioBus(this, "master");
            _mixer.AddRootBus(MasterBus);
            _buses[MasterBus.Name] = MasterBus;
            _stream = Raylib.LoadAudioStream((uint)sampleRate, 16, (uint)channels); // 16-bit stream for broad support
            Raylib.PlayAudioStream(_stream);
            _streamStarted = true;
            AudioDiagnostics.Log($"[RaylibAudio] Created stream sr={sampleRate} ch={channels}.");
            _audioThreadRunning = true;
            _audioThread = new Thread(AudioThreadLoop){ IsBackground = true, Priority = ThreadPriority.AboveNormal};
            _audioThread.Start();
        }

        public AudioBus GetOrCreateBus(string name, AudioBus? parent = null)
        {
            if (_buses.TryGetValue(name, out var existing)) return existing;
            var owner = parent ?? MasterBus;
            var bus = owner.CreateChildBus(name);
            _buses[name] = bus;
            return bus;
        }

        public AudioHandle PlayClip(AudioClip clip, string busName = "master", AudioPlaybackSettings? settings = null)
        {
            var bus = GetOrCreateBus(busName);
            var handle = bus.Play(clip, settings);
            return handle;
        }

        public void Update(double deltaTime)
        {
            // Stream pumping handled in audio thread; ensure RaylibStream processed
            if (_disposed) return;
        }

        private void AudioThreadLoop()
        {
            int samplesPerUpdate = _framesPerUpdate * _channels;
            float[] temp = new float[samplesPerUpdate];
            short[] tempShort = new short[samplesPerUpdate];
            double chunkDuration = (double)_framesPerUpdate / _sampleRate;
            int idleSpins = 0;
            while (_audioThreadRunning)
            {
                MasterBus.Update(chunkDuration);
                int samples = _mixer.Read(temp, 0, samplesPerUpdate);
                if (samples > 0)
                {
                    for (int i = 0; i < samples; i++)
                    {
                        float v = temp[i];
                        if (v > 1f) v = 1f; else if (v < -1f) v = -1f;
                        tempShort[i] = (short)(v * short.MaxValue);
                    }
                    if (Raylib.IsAudioStreamProcessed(_stream))
                    {
                        int frameCount = samples / _channels;
                        if (frameCount < _framesPerUpdate)
                        {
                            // Pad remainder with silence to expected frame size
                            for (int i = samples; i < samplesPerUpdate; i++) tempShort[i] = 0;
                            frameCount = _framesPerUpdate;
                        }
                        unsafe
                        {
                            fixed (short* ptr = tempShort)
                            {
                                Raylib.UpdateAudioStream(_stream, ptr, frameCount);
                            }
                        }
                    }
                }
                // Adaptive small sleep to relinquish time slice without risking long stalls.
                if (!Raylib.IsAudioStreamProcessed(_stream))
                {
                    idleSpins++;
                    if (idleSpins < 8)
                        Thread.Yield();
                    else
                        Thread.Sleep(1);
                }
                else
                {
                    idleSpins = 0; // reset when we successfully push/are ready
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _audioThreadRunning = false;
            try { _audioThread?.Join(100); } catch { }
            if (_streamStarted)
            {
                Raylib.StopAudioStream(_stream);
                Raylib.UnloadAudioStream(_stream);
            }
            // Do not close device globally; let application decide.
            _mixer.Dispose();
        }
    }
#endif
}