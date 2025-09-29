using System;
using System.Collections.Generic;
using CSCore;
using CSCore.SoundOut;
using Asmo.Sound;

namespace Asmo.Audio
{
    public sealed class AudioEngine : IDisposable
    {
    private readonly AudioMixerSource _mixer;
    private readonly SampleToWaveSource _waveSource;
    private readonly WasapiOut _output;
        private readonly Dictionary<string, AudioBus> _buses = new(StringComparer.OrdinalIgnoreCase);
        private bool _disposed;
    private bool _outputInitialized;

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

        public AudioEngine(int sampleRate = AudioClip.DefaultSampleRate, int channels = AudioClip.DefaultChannels)
        {
            _mixer = new AudioMixerSource(sampleRate, channels);
            _waveSource = new SampleToWaveSource(_mixer);
            _output = new WasapiOut();
            MasterBus = new AudioBus(this, "master");
            _mixer.AddRootBus(MasterBus);
            _buses[MasterBus.Name] = MasterBus;
            AudioDiagnostics.Log($"AudioEngine created (sampleRate={sampleRate}, channels={channels}).");
        }

        public AudioBus MasterBus { get; }

        /// <summary>
        /// Retrieves an existing bus or creates it if it does not exist. Buses are parented to the master bus by default.
        /// </summary>
        public AudioBus GetOrCreateBus(string name, AudioBus? parent = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bus name must be provided", nameof(name));

            if (_buses.TryGetValue(name, out var existing))
            {
                AudioDiagnostics.Log($"AudioEngine returning existing bus '{name}'.");
                return existing;
            }

            var owner = parent ?? MasterBus;
            var bus = owner.CreateChildBus(name);
            _buses[name] = bus;
            AudioDiagnostics.Log($"AudioEngine registered new bus '{name}' under '{owner.Name}'.");
            return bus;
        }

        /// <summary>
        /// Plays an audio clip on the specified bus (defaults to master).
        /// </summary>
        public AudioHandle PlayClip(AudioClip clip, string busName = "master", AudioPlaybackSettings? settings = null)
        {
            AudioDiagnostics.Log($"AudioEngine PlayClip request on bus '{busName}'.");
            var bus = GetOrCreateBus(busName);
            var handle = bus.Play(clip, settings);
            EnsureOutputRunning();
            return handle;
        }

        public void Update(double deltaTime)
        {
            if (_disposed) return;

            MasterBus.Update(deltaTime);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            AudioDiagnostics.Log("AudioEngine disposing.");
            if (_outputInitialized)
            {
                AudioDiagnostics.Log("Stopping audio output.");
                _output.Stop();
            }
            _output.Dispose();
            _waveSource.Dispose();
            _mixer.Dispose();
            AudioDiagnostics.Log("AudioEngine disposed.");
        }

        internal void EnsureOutputRunning()
        {
            if (!_outputInitialized)
            {
                AudioDiagnostics.Log("Initialising audio output device (WasapiOut).");
                _output.Initialize(_waveSource);
                _outputInitialized = true;
            }

            if (_output.PlaybackState != PlaybackState.Playing)
            {
                AudioDiagnostics.Log("Starting audio output playback.");
                _output.Play();
            }
        }
    }
}
