using System;
using System.Buffers;
using System.Collections.Generic;

namespace Asmo.Audio
{
    public sealed class AudioBus
    {
        private readonly List<AudioInstance> _instances = new();
        private readonly List<AudioBus> _children = new();
        private readonly object _lock = new();
        private readonly AudioEngine _engine;

        internal AudioBus(AudioEngine engine, string name)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Name = name;
            AudioDiagnostics.Log($"Bus '{name}' created.");
        }

        public string Name { get; }
        public float Volume { get; set; } = 1f;
        public float Pan { get; set; } = 0f;

        public AudioBus CreateChildBus(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Bus name must be provided", nameof(name));

            var child = new AudioBus(_engine, name);
            lock (_lock)
            {
                _children.Add(child);
            }
            AudioDiagnostics.Log($"Bus '{Name}' created child bus '{name}'.");
            return child;
        }

        internal void AddChild(AudioBus bus)
        {
            if (bus == null) throw new ArgumentNullException(nameof(bus));
            lock (_lock)
            {
                _children.Add(bus);
            }
            AudioDiagnostics.Log($"Bus '{Name}' added existing child bus '{bus.Name}'.");
        }

        public AudioHandle Play(AudioClip clip, AudioPlaybackSettings? settings = null)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));
            var opts = settings ?? AudioPlaybackSettings.Default;
            var instance = new AudioInstance(clip, opts.Loop, Math.Clamp(opts.Volume, 0f, 4f), Math.Clamp(opts.Pan, -1f, 1f));

            if (opts.FadeInSeconds > 0)
            {
                instance.SetVolume(0f);
                instance.FadeTo(opts.Volume, opts.FadeInSeconds);
            }

            AudioDiagnostics.Log($"Bus '{Name}' starting clip (loop={opts.Loop}, volume={Math.Clamp(opts.Volume, 0f, 4f):0.00}, pan={Math.Clamp(opts.Pan, -1f, 1f):0.00}, fade={opts.FadeInSeconds:0.00}s).");
            lock (_lock)
            {
                _instances.Add(instance);
            }

            _engine.EnsureOutputRunning();

            return new AudioHandle(instance, this);
        }

        public void StopAll()
        {
            int instanceCount;
            lock (_lock)
            {
                instanceCount = _instances.Count;
                foreach (var instance in _instances)
                {
                    instance.Stop();
                }
                foreach (var child in _children)
                {
                    child.StopAll();
                }
            }
            AudioDiagnostics.Log($"Bus '{Name}' StopAll invoked (instances={instanceCount}).");
        }

        internal void Update(double deltaTime)
        {
            lock (_lock)
            {
                for (int i = _instances.Count - 1; i >= 0; i--)
                {
                    var instance = _instances[i];
                    instance.Update(deltaTime);
                    if (!instance.IsPlaying)
                    {
                        AudioDiagnostics.Log($"Bus '{Name}' removing completed instance (loop={instance.Loop}).");
                        _instances.RemoveAt(i);
                    }
                }

                foreach (var child in _children)
                {
                    child.Update(deltaTime);
                }
            }
        }

        internal void Mix(float[] destination, int offset, int count, int channels, float inheritedVolume, float inheritedPan)
        {
            float busVolume = inheritedVolume * Volume;
            float busPan = Math.Clamp(inheritedPan + Pan, -1f, 1f);
            List<AudioInstance>? finishedInstances = null;

            lock (_lock)
            {
                if (_instances.Count > 0)
                {
                    var scratch = ArrayPool<float>.Shared.Rent(count);
                    try
                    {
                        foreach (var instance in _instances)
                        {
                            Array.Clear(scratch, 0, count);
                            int read = instance.Read(scratch, 0, count);
                            if (read == 0)
                            {
                                finishedInstances ??= new List<AudioInstance>();
                                finishedInstances.Add(instance);
                                AudioDiagnostics.Log($"Bus '{Name}' instance produced no samples (read=0, volume={instance.Volume:0.00}).");
                                Console.WriteLine($"[AudioBus] Removing instance for bus '{Name}' due to read=0");
                                continue;
                            }

                            float localMin = float.MaxValue;
                            float localMax = float.MinValue;
                            for (int i = 0; i < read; i++)
                            {
                                float s = scratch[i];
                                if (s < localMin) localMin = s;
                                if (s > localMax) localMax = s;
                            }
                            AudioDiagnostics.Log($"Bus '{Name}' mixing {read} samples (busVol={busVolume:0.00}, instVol={instance.Volume:0.00}, pan={instance.Pan:0.00}, min={localMin:0.0000}, max={localMax:0.0000}).");

                            Accumulate(destination, offset, scratch, read, channels, busVolume * instance.Volume, Math.Clamp(busPan + instance.Pan, -1f, 1f));
                        }
                    }
                    finally
                    {
                        ArrayPool<float>.Shared.Return(scratch, true);
                    }

                    if (finishedInstances != null)
                    {
                        foreach (var instance in finishedInstances)
                        {
                            Console.WriteLine($"[AudioBus] Actually removing instance for bus '{Name}'");
                            _instances.Remove(instance);
                        }
                    }
                }

                foreach (var child in _children)
                {
                    child.Mix(destination, offset, count, channels, busVolume, busPan);
                }
            }
        }

        private static void Accumulate(float[] destination, int offset, float[] source, int count, int channels, float gain, float pan)
        {
            gain = Math.Clamp(gain, 0f, 4f);
            pan = Math.Clamp(pan, -1f, 1f);

            if (channels == 1)
            {
                for (int i = 0; i < count; i++)
                {
                    destination[offset + i] += source[i] * gain;
                }
                return;
            }

            // Stereo panning
            float leftScale = gain * (float)(Math.Sqrt((1 - pan) * 0.5));
            float rightScale = gain * (float)(Math.Sqrt((1 + pan) * 0.5));

            for (int i = 0; i < count; i += channels)
            {
                destination[offset + i] += source[i] * leftScale;
                if (channels > 1 && i + 1 < count)
                {
                    destination[offset + i + 1] += source[i + 1] * rightScale;
                }
            }
        }

        internal void RemoveInstance(AudioInstance instance)
        {
            lock (_lock)
            {
                _instances.Remove(instance);
            }
            AudioDiagnostics.Log($"Bus '{Name}' instance removed manually.");
        }
    }
}
