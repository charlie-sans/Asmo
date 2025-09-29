using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using CSCore;
using CSCore.SoundOut;
using CSCore.Streams;
using Megadrive; // For VgmSampleSource

namespace Asmo.Sound
{
    // Instrument delegate for custom instruments
    public delegate ISampleSource Instrument(float frequency, float durationSeconds, float amplitude = 0.2f);

    public class ChipTunePlayer
    {
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;
        private readonly ConcurrentQueue<IWaveSource> _oneShotQueue = new();
        private SimpleMixer _mixer;

        public float BasePitch { get; set; } = 440f; // A4

        public ChipTunePlayer()
        {
            _mixer = new SimpleMixer();
        }

        public struct Note
        {
            public float Frequency;
            public float DurationSeconds;
            public Note(float freq, float dur)
            {
                Frequency = freq;
                DurationSeconds = dur;
            }
        }

        // Converts note string (e.g. "c4", "g#3") to frequency
        public float NoteStringToFrequency(string note, float basePitch = 440f)
        {
            string[] noteNames = { "c", "c#", "d", "d#", "e", "f", "f#", "g", "g#", "a", "a#", "b" };
            note = note.ToLower().Trim();
            int octave = int.Parse(note.Last().ToString());
            string noteName = note.Substring(0, note.Length - 1);

            int noteIndex = Array.IndexOf(noteNames, noteName);
            if (noteIndex < 0) throw new ArgumentException("Invalid note name: " + note);

            // MIDI note number for A4 = 69
            int midi = (octave + 1) * 12 + noteIndex;
            int a4Midi = 69;
            return basePitch * (float)Math.Pow(2, (midi - a4Midi) / 12.0);
        }

        // tempoBpm: beats per minute, noteLength: beats per note (e.g. 1 = quarter, 0.5 = eighth)
        public void PlaySequence(IEnumerable<(string note, float noteLength)> notes, float tempoBpm = 120f)
        {
            var sources = new List<IWaveSource>();
            float secondsPerBeat = 60f / tempoBpm;

            foreach (var (noteStr, noteLen) in notes)
            {
                float freq = NoteStringToFrequency(noteStr, BasePitch);
                float dur = noteLen * secondsPerBeat;
                var sampleSource = new SimpleSquareWaveSource(freq, dur, 0.2f);
                var waveSource = new SampleToWaveSource(sampleSource);
                waveSource.SetLength((long)(sampleSource.WaveFormat.SampleRate * dur));
                sources.Add(waveSource);
            }

            var sequence = ConcatenateWaveSources(sources);
            _mixer.AddSource(sequence);
            _soundOut = new WasapiOut();
            _soundOut.Initialize(_mixer);
            _soundOut.Play();
            _waveSource = _mixer;
        }

        // Play a sequence of notes with custom instruments
        public void PlaySequenceWithInstruments(
            IEnumerable<(string note, float noteLength, Instrument instrument)> notes,
            float tempoBpm = 120f,
            Func<string, float, float> noteStringToFrequency = null)
        {
            var sources = new List<IWaveSource>();
            float secondsPerBeat = 60f / tempoBpm;
            noteStringToFrequency ??= NoteStringToFrequency;

            foreach (var (noteStr, noteLen, instrument) in notes)
            {
                float freq = noteStringToFrequency(noteStr, BasePitch);
                float dur = noteLen * secondsPerBeat;
                var sampleSource = instrument(freq, dur, 0.2f);
                var waveSource = new SampleToWaveSource(sampleSource);
                waveSource.SetLength((long)(sampleSource.WaveFormat.SampleRate * dur));
                sources.Add(waveSource);
            }

            var sequence = ConcatenateWaveSources(sources);
            _mixer.AddSource(sequence);
            _soundOut = new WasapiOut();
            _soundOut.Initialize(_mixer);
            _soundOut.Play();
            _waveSource = _mixer;
        }

        public void PlayOneShot(IWaveSource sound)
        {
            _oneShotQueue.Enqueue(sound);
        }

        public void UpdateMixer()
        {
            bool added = false;
            while (_oneShotQueue.TryDequeue(out var sfx))
            {
                _mixer.AddSource(sfx);
                added = true;
            }
            if (added)
            {
                if (_soundOut == null)
                {
                    _soundOut = new WasapiOut();
                    _soundOut.Initialize(_mixer);
                }
                if (_soundOut.PlaybackState != PlaybackState.Playing)
                {
                    _soundOut.Play();
                }
            }
        }

        private IWaveSource ConcatenateWaveSources(IEnumerable<IWaveSource> sources)
        {
            var enumerator = sources.GetEnumerator();
            if (!enumerator.MoveNext())
                throw new ArgumentException("No sources to concatenate.");

            var waveSources = sources.ToList();
            if (waveSources.Count == 1)
                return waveSources[0];

            // Manually concatenate by chaining the sources together
            return new SequentialWaveSource(waveSources);
        }

        // Helper class to play multiple IWaveSource sequentially
        private class SequentialWaveSource : IWaveSource
        {
            private readonly List<IWaveSource> _sources;
            private int _currentIndex;
            private long _position;

            public SequentialWaveSource(List<IWaveSource> sources)
            {
                _sources = sources;
                _currentIndex = 0;
                _position = 0;
                WaveFormat = _sources.Count > 0 ? _sources[0].WaveFormat : new WaveFormat(44100, 32, 1, AudioEncoding.IeeeFloat);
            }

            public WaveFormat WaveFormat { get; }

            public long Position
            {
                get => _sources.Take(_currentIndex).Sum(s => s.Length) + (_currentIndex < _sources.Count ? _sources[_currentIndex].Position : 0);
                set
                {
                    long pos = value;
                    for (int i = 0; i < _sources.Count; i++)
                    {
                        if (pos < _sources[i].Length)
                        {
                            _currentIndex = i;
                            _sources[i].Position = pos;
                            for (int j = i + 1; j < _sources.Count; j++)
                                _sources[j].Position = 0;
                            break;
                        }
                        pos -= _sources[i].Length;
                    }
                }
            }

            public long Length => _sources.Sum(s => s.Length);

            public bool CanSeek => false;

            public int Read(byte[] buffer, int offset, int count)
            {
                int read = 0;
                while (read < count && _currentIndex < _sources.Count)
                {
                    int r = _sources[_currentIndex].Read(buffer, offset + read, count - read);
                    if (r == 0)
                    {
                        _currentIndex++;
                    }
                    else
                    {
                        read += r;
                    }
                }
                return read;
            }

            public void Dispose()
            {
                foreach (var s in _sources)
                    s.Dispose();
            }
        }

        public void Stop()
        {
            _soundOut?.Stop();
            _waveSource?.Dispose();
            _soundOut?.Dispose();
        }

        private ISampleSource _currentVgm;
        //private VgmSampleSource _vgmSource;

        // Play a VGM file
        public void PlayVgm(string vgmPath)
        {
            StopVgm();
            //_vgmSource = new VgmSampleSource(vgmPath);
            //_currentVgm = _vgmSource;
            Console.WriteLine($"Playing VGM: {vgmPath}");
            
            _mixer.AddSource(new SampleToWaveSource(_currentVgm));
            Console.WriteLine("VGM added to mixer.");
            if (_soundOut == null)
            {
                _soundOut = new WasapiOut();
                _soundOut.Initialize(_mixer);
            }
            Console.WriteLine("SoundOut initialized.");
            _soundOut.Play();
        }

        // Stop VGM playback
        public void StopVgm()
        {
            _currentVgm?.Dispose();
            _currentVgm = null;
            //_vgmSource = null;
            // Optionally clear mixer or stop soundOut
        }

        // Pause VGM playback
        public void PauseVgm()
        {
            _soundOut?.Pause();
        }

        // Resume VGM playback
        public void ResumeVgm()
        {
            _soundOut?.Play();
        }

        // Query if VGM is playing
        public bool IsVgmPlaying => _soundOut?.PlaybackState == PlaybackState.Playing && _currentVgm != null;

        // Play a one-shot sound from an instrument
        public void PlayOneShotFromInstrument(float freq, float dur, float amp, Instrument instrument)
        {
            var sampleSource = instrument(freq, dur, amp);
            var waveSource = new SampleToWaveSource(sampleSource);
            waveSource.SetLength((long)(sampleSource.WaveFormat.SampleRate * dur));
            PlayOneShot(waveSource);
        }

        // Play a beep sound (default: square wave)
        public void PlayBeep(float freq = 880f, float dur = 0.12f, float amp = 0.5f, Instrument instrument = null)
        {
            instrument ??= (f, d, a) => new SimpleSquareWaveSource(f, d, a);
            PlayOneShotFromInstrument(freq, dur, amp, instrument);
        }

        // Play a boop sound (default: drum kick)
        public void PlayBoop(float freq = 440f, float dur = 0.12f, float amp = 0.5f, Instrument instrument = null)
        {
            instrument ??= (f, d, a) => SoundSynth.DrumKick(1f, 440f, 40f, 1f);
            PlayOneShotFromInstrument(freq, dur, amp, instrument);
        }

        // Stop all currently playing notes (monophonic)
        public void StopAll()
        {
            _mixer?.ClearSources();
            _oneShotQueue.Clear();
            _soundOut?.Stop();
        }
    }

    public class SampleToWaveSource : IWaveSource
    {
        private readonly ISampleSource _source;
        private readonly WaveFormat _waveFormat;
        private long? _customLengthBytes = null;
        private long _bytesRead = 0;

        public SampleToWaveSource(ISampleSource source)
        {
            _source = source;
            _waveFormat = new WaveFormat(source.WaveFormat.SampleRate, 32, source.WaveFormat.Channels, AudioEncoding.IeeeFloat);
        }

        public WaveFormat WaveFormat => _waveFormat;

        public long Position
        {
            get => _source.Position * _waveFormat.BlockAlign;
            set
            {
                _source.Position = value / _waveFormat.BlockAlign;
                _bytesRead = value;
            }
        }

        public long Length => _customLengthBytes ?? (_source.Length * _waveFormat.BlockAlign);

        public bool CanSeek => _source.CanSeek;

        public int Read(byte[] buffer, int offset, int count)
        {
            long bytesLeft = (_customLengthBytes ?? long.MaxValue) - _bytesRead;
            if (bytesLeft <= 0)
                return 0;

            int bytesToRead = (int)Math.Min(count, bytesLeft);
            int samplesRequired = bytesToRead / 4;
            float[] floatBuffer = new float[samplesRequired];
            int samplesRead = _source.Read(floatBuffer, 0, samplesRequired);
            int bytesReadNow = samplesRead * 4;
            Buffer.BlockCopy(floatBuffer, 0, buffer, offset, bytesReadNow);
            _bytesRead += bytesReadNow;
            return bytesReadNow;
        }

        public void Dispose()
        {
            _source.Dispose();
        }

        public void SetLength(long bytes)
        {
            _customLengthBytes = bytes;
        }
    }

    public class SimpleMixer : IWaveSource
    {
        private readonly List<IWaveSource> _sources = new();
        public WaveFormat WaveFormat { get; }

        public SimpleMixer(int sampleRate = 44100, int channels = 1)
        {
            WaveFormat = new WaveFormat(sampleRate, 32, channels, AudioEncoding.IeeeFloat);
        }

        public void AddSource(IWaveSource source)
        {
            lock (_sources)
            {
                _sources.Add(source);
            }
        }

        public void ClearSources()
        {
            lock (_sources)
            {
                foreach (var source in _sources)
                {
                    source.Dispose();
                }
                _sources.Clear();
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            Array.Clear(buffer, offset, count);
            float[] mixBuffer = new float[count / 4];
            lock (_sources)
            {
                for (int i = _sources.Count - 1; i >= 0; i--)
                {
                    var src = _sources[i];
                    byte[] tempBytes = new byte[mixBuffer.Length * 4];
                    int readBytes = src.Read(tempBytes, 0, tempBytes.Length);
                    int samplesRead = readBytes / 4;
                    float[] temp = new float[samplesRead];
                    Buffer.BlockCopy(tempBytes, 0, temp, 0, readBytes);
                    for (int j = 0; j < samplesRead; j++)
                        mixBuffer[j] += temp[j];
                    if (samplesRead < mixBuffer.Length)
                        _sources.RemoveAt(i); // Remove finished sources
                }
            }
            Buffer.BlockCopy(mixBuffer, 0, buffer, offset, mixBuffer.Length * 4);
            return count;
        }

        public long Position { get => 0; set { } }
        public long Length => 0;
        public bool CanSeek => false;
        public void Dispose()
        {
            lock (_sources)
                foreach (var s in _sources) s.Dispose();
            _sources.Clear();
        }
    }
}
