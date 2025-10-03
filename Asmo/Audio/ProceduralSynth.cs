using System;
using System.Collections.Generic;
using System.Globalization;

namespace Asmo.Audio
{
    /// <summary>
    /// Simple procedural synthesis utilities (post-CSCore) that generate in-memory AudioClips
    /// for tones, noise, and note sequences with a lightweight ADSR envelope.
    /// </summary>
    public static class ProceduralSynth
    {
        public enum Waveform { Sine, Square, Saw, Triangle, Noise }

        /// <summary>
        /// ADSR envelope definition (seconds + sustain level 0..1). Any zero segment is skipped.
        /// </summary>
        public readonly struct ADSR
        {
            public readonly float Attack;      // Seconds
            public readonly float Decay;       // Seconds
            public readonly float SustainLevel;// 0..1
            public readonly float Release;     // Seconds
            public ADSR(float attack, float decay, float sustainLevel, float release)
            {
                Attack = Math.Max(0, attack);
                Decay = Math.Max(0, decay);
                SustainLevel = Math.Clamp(sustainLevel, 0f, 1f);
                Release = Math.Max(0, release);
            }
        }

        private static readonly ADSR DefaultEnvelope = new(0.005f, 0.05f, 0.8f, 0.08f);

        /// <summary>
        /// Generate a single tone clip.
        /// </summary>
        public static AudioClip GenerateTone(double frequency, double durationSeconds, float amplitude = 0.5f,
            Waveform waveform = Waveform.Sine, ADSR? envelope = null,
            int sampleRate = AudioClip.DefaultSampleRate, int channels = AudioClip.DefaultChannels)
        {
            if (durationSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (frequency <= 0 && waveform != Waveform.Noise) throw new ArgumentOutOfRangeException(nameof(frequency));
            if (channels != 1 && channels != 2) throw new ArgumentException("Only mono or stereo supported", nameof(channels));

            int totalSamplesPerChannel = (int)Math.Ceiling(durationSeconds * sampleRate);
            float[] data = new float[totalSamplesPerChannel * channels];
            var env = envelope ?? DefaultEnvelope;
            double twoPi = Math.PI * 2.0;
            double invSampleRate = 1.0 / sampleRate;
            double nyquist = sampleRate / 2.0;

            // Basic anti-harshness: if frequency above Nyquist, fold it back (very naive)
            if (frequency > nyquist && waveform != Waveform.Noise)
            {
                frequency = nyquist - (frequency - nyquist);
                if (frequency < 50) frequency = 50; // avoid subsonic nonsense
            }
            var rng = new Random();

            for (int i = 0; i < totalSamplesPerChannel; i++)
            {
                double t = i * invSampleRate;
                float envGain = ComputeAdsrGain((float)t, (float)durationSeconds, env);
                double phase = frequency * t;
                double sample;
                switch (waveform)
                {
                    case Waveform.Sine:
                        sample = Math.Sin(twoPi * phase);
                        break;
                    case Waveform.Square:
                        sample = Math.Sign(Math.Sin(twoPi * phase));
                        break;
                    case Waveform.Saw:
                        sample = 2.0 * (phase - Math.Floor(phase + 0.5)); // bipolar -1..1
                        break;
                    case Waveform.Triangle:
                        sample = 2.0 * Math.Abs(2.0 * (phase - Math.Floor(phase + 0.5))) - 1.0;
                        break;
                    case Waveform.Noise:
                        sample = (rng.NextDouble() * 2.0) - 1.0;
                        break;
                    default:
                        sample = 0;
                        break;
                }
                float final = (float)(sample * amplitude * envGain);
                // Clamp for safety
                if (final > 1f) final = 1f; else if (final < -1f) final = -1f;
                if (channels == 2)
                {
                    int idx = i * 2;
                    data[idx] = final;
                    data[idx + 1] = final;
                }
                else
                {
                    data[i] = final;
                }
            }
            return new AudioClip(data, sampleRate, channels);
        }

        /// <summary>
        /// Generate a concatenated sequence of notes given (noteName, beats) pairs at a BPM.
        /// Note names: c,c#,d,d#,e,f,f#,g,g#,a,a#,b plus octave (e.g. c4, a#3). Case-insensitive.
        /// </summary>
        public static AudioClip GenerateNoteSequence(IEnumerable<(string note, float beats)> notes, float bpm,
            Waveform waveform = Waveform.Sine, float amplitude = 0.5f, ADSR? envelope = null,
            int sampleRate = AudioClip.DefaultSampleRate, int channels = AudioClip.DefaultChannels)
        {
            if (bpm <= 0) throw new ArgumentOutOfRangeException(nameof(bpm));
            var env = envelope ?? DefaultEnvelope;
            double secondsPerBeat = 60.0 / bpm;
            var clips = new List<AudioClip>();
            foreach (var (note, beats) in notes)
            {
                if (beats <= 0) continue;
                double dur = beats * secondsPerBeat;
                double freq = NoteToFrequency(note);
                clips.Add(GenerateTone(freq, dur, amplitude, waveform, env, sampleRate, channels));
            }
            return Concatenate(clips, sampleRate, channels);
        }

        /// <summary>
        /// Immediately plays a procedural tone through the engine.
        /// </summary>
        public static AudioHandle PlayTone(AudioEngine engine, double frequency, double durationSeconds, float amplitude = 0.5f,
            Waveform waveform = Waveform.Sine, ADSR? envelope = null, string busName = "master")
        {
            var clip = GenerateTone(frequency, durationSeconds, amplitude, waveform, envelope);
            return engine.PlayClip(clip, busName);
        }

        /// <summary>
        /// Play a note sequence directly (convenience).
        /// </summary>
        public static AudioHandle PlayNoteSequence(AudioEngine engine, IEnumerable<(string note, float beats)> notes, float bpm,
            Waveform waveform = Waveform.Sine, float amplitude = 0.5f, ADSR? envelope = null, string busName = "master")
        {
            var clip = GenerateNoteSequence(notes, bpm, waveform, amplitude, envelope);
            return engine.PlayClip(clip, busName);
        }

        public static double NoteToFrequency(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) throw new ArgumentException("Empty note", nameof(note));
            note = note.Trim().ToLowerInvariant();
            // Extract trailing digits for octave
            int octaveStart = note.Length - 1;
            while (octaveStart >= 0 && char.IsDigit(note[octaveStart])) octaveStart--;
            octaveStart++;
            if (octaveStart == 0 || octaveStart >= note.Length) throw new FormatException("Invalid note format (expected name+octave)");
            string name = note.Substring(0, octaveStart);
            string octaveStr = note.Substring(octaveStart);
            if (!int.TryParse(octaveStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int octave))
                throw new FormatException("Invalid octave in note");

            string[] names = {"c","c#","d","d#","e","f","f#","g","g#","a","a#","b"};
            int idx = Array.IndexOf(names, name);
            if (idx < 0) throw new ArgumentException("Unknown note name: " + name);
            int midi = (octave + 1) * 12 + idx; // MIDI mapping: C-1 = 0
            int a4 = 69;
            return 440.0 * Math.Pow(2.0, (midi - a4) / 12.0);
        }

        private static float ComputeAdsrGain(float t, float totalDuration, ADSR e)
        {
            float attackEnd = e.Attack;
            float decayEnd = attackEnd + e.Decay;
            float releaseStart = Math.Max(totalDuration - e.Release, decayEnd); // ensure order

            if (t < 0) return 0f;
            if (t < attackEnd && attackEnd > 0)
            {
                return t / attackEnd; // ramp 0->1
            }
            if (t < decayEnd && e.Decay > 0)
            {
                float dT = (t - attackEnd) / e.Decay; // 0..1
                return 1f - (1f - e.SustainLevel) * dT;
            }
            if (t < releaseStart)
            {
                return e.SustainLevel;
            }
            if (t < totalDuration && e.Release > 0)
            {
                float rT = (t - releaseStart) / Math.Max(1e-6f, e.Release);
                return e.SustainLevel * (1f - rT);
            }
            return 0f;
        }

        private static AudioClip Concatenate(List<AudioClip> clips, int sampleRate, int channels)
        {
            int totalFrames = 0;
            foreach (var c in clips) totalFrames += c.TotalSamples / channels;
            float[] data = new float[totalFrames * channels];
            int writeIndex = 0;
            foreach (var c in clips)
            {
                var src = c.Samples; // internal
                Array.Copy(src, 0, data, writeIndex, src.Length);
                writeIndex += src.Length;
            }
            return new AudioClip(data, sampleRate, channels);
        }
    }
}
