using System;
using System.Collections.Generic;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Sound;
using Asmo.Types;
using Asmo.Window.input;
using CSCore;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chippy
{
    // Chippy, a chiptune tracker for Asmo
    public class Game : IConsoleGame
    {
        private const int PatternRows = 32;
        private const int PatternChannels = 8;
        private const double BeatsPerStep = 0.25; // Sixteenth notes at the current tempo
        private const int NoteFieldIndex = 0;
        private const int EffectHighFieldIndex = 1;
        private const int EffectLowFieldIndex = 2;
        private const int ChannelColumnWidth = 60;
        private const int NoteColumnWidth = 18;
    private const int EffectColumnWidth = 30;
    private const double MinNoteDurationSeconds = 0.05;
    private const double MinReleaseSeconds = 0.9;
    private const double MaxReleaseSeconds = 2;

        private readonly Instrument[] instruments;
        private readonly float[] instrumentAmps = { 0.7f, 0.55f, 0.85f, 0.45f };
        private readonly string[] instrumentNames = { "Square Lead", "Triangle", "Bass", "Noise" };
        private readonly Color[] instrumentColors =
        {
            Colors.Cyan,
            Colors.Magenta,
            Colors.Yellow,
            Colors.Orange
        };

    private Keyboard keyboard = null!;
    private AudioEngine audioEngine = null!;
    private AudioBus chippyBus = null!;
    private readonly List<AudioHandle> activeVoices = new();
    private readonly Queue<QueuedNote> oneShotQueue = new();
    private readonly AudioHandle[] channelVoices = new AudioHandle[PatternChannels];
    private bool isInitialized;
        private readonly TrackerPattern pattern = new(PatternRows, PatternChannels);

        private int cursorRow;
        private int cursorChannel;
        private int cursorField;
        private int currentOctave = 4;
        private int currentInstrument;
        private bool followMode = true;

        private bool isPlaying;
        private double bpm = 140.0;
        private double playbackTimer;
        private int nextPlaybackRow;
        private int activeRow;
        private double blinkTimer;
        private bool blinkVisible = true;

        private readonly struct QueuedNote
        {
            public QueuedNote(int channel, AudioClip clip, AudioPlaybackSettings settings)
            {
                Channel = channel;
                Clip = clip;
                Settings = settings;
            }

            public int Channel { get; }
            public AudioClip Clip { get; }
            public AudioPlaybackSettings Settings { get; }
        }

        private static readonly Dictionary<Keys, int> HexKeyMap = new()
        {
            { Keys.D0, 0x0 },
            { Keys.D1, 0x1 },
            { Keys.D2, 0x2 },
            { Keys.D3, 0x3 },
            { Keys.D4, 0x4 },
            { Keys.D5, 0x5 },
            { Keys.D6, 0x6 },
            { Keys.D7, 0x7 },
            { Keys.D8, 0x8 },
            { Keys.D9, 0x9 },
            { Keys.A, 0xA },
            { Keys.B, 0xB },
            { Keys.C, 0xC },
            { Keys.D, 0xD },
            { Keys.E, 0xE },
            { Keys.F, 0xF }
        };

        public Game()
        {
            
            instruments = new Instrument[]
            {
                CreateSquareInstrument(),
                CreateTriangleInstrument(),
                CreateBassInstrument(),
                CreateNoiseInstrument()
            };
        }

        public void Init(Surface surface)
        {
            keyboard = new Keyboard(surface.Window);
            audioEngine = new AudioEngine();
            chippyBus = audioEngine.GetOrCreateBus("chippy");
            chippyBus.Volume = 0.85f;
            activeRow = 0;
            nextPlaybackRow = 0;
            cursorField = NoteFieldIndex;
            isInitialized = true;
        
        }

        public void Update(double deltaTime)
        {
            if (!isInitialized)
            {
                return;
            }

            UpdateBlink(deltaTime);
            HandleInput();
            AdvancePlayback(deltaTime);
            UpdateAudio(deltaTime);

            keyboard.Update();
            audioEngine.Update(deltaTime);
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);

            DrawHeader(surface);
            DrawPattern(surface);
            DrawFooter(surface);
        }

        private void UpdateBlink(double deltaTime)
        {
            blinkTimer += deltaTime;
            if (blinkTimer >= 0.4f)
            {
                blinkTimer = 0;
                blinkVisible = !blinkVisible;
            }
        }

        private void HandleInput()
        {
            if (!isInitialized)
            {
                return;
            }

            HandleTransport();
            HandleSettings();
            HandleNavigation();
            HandleEditing();
        }

        private void HandleTransport()
        {
            if (!isInitialized)
            {
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Space))
            {
                if (!isPlaying)
                {
                    StartPlayback();
                }
                else
                {
                    StopPlayback();
                }
            }

            if (keyboard.IsKeyPressed(Keys.Tab))
            {
                bool shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                if (shiftDown)
                {
                    followMode = !followMode;
                }
            }
        }

        private void HandleSettings()
        {
            if (!isInitialized)
            {
                return;
            }

            if (keyboard.IsKeyPressed(Keys.LeftBracket))
            {
                currentOctave = Math.Clamp(currentOctave - 1, 0, 8);
            }

            if (keyboard.IsKeyPressed(Keys.RightBracket))
            {
                currentOctave = Math.Clamp(currentOctave + 1, 0, 8);
            }

            if (keyboard.IsKeyPressed(Keys.PageUp))
            {
                bpm = Math.Min(300.0, bpm + 5.0);
            }

            if (keyboard.IsKeyPressed(Keys.PageDown))
            {
                bpm = Math.Max(40.0, bpm - 5.0);
            }

            if (keyboard.IsKeyPressed(Keys.F1)) SetInstrument(0);
            if (keyboard.IsKeyPressed(Keys.F2)) SetInstrument(1);
            if (keyboard.IsKeyPressed(Keys.F3)) SetInstrument(2);
            if (keyboard.IsKeyPressed(Keys.F4)) SetInstrument(3);
        }

        private void HandleNavigation()
        {
            if (!isInitialized)
            {
                return;
            }

            if (keyboard.IsKeyPressed(Keys.Up))
            {
                MoveRow(-1);
            }

            if (keyboard.IsKeyPressed(Keys.Down))
            {
                MoveRow(1);
            }

            if (keyboard.IsKeyPressed(Keys.Left))
            {
                MoveHorizontal(-1);
            }

            if (keyboard.IsKeyPressed(Keys.Right))
            {
                MoveHorizontal(1);
            }

            if (keyboard.IsKeyPressed(Keys.Home))
            {
                cursorRow = 0;
                if (!isPlaying)
                {
                    activeRow = cursorRow;
                    nextPlaybackRow = cursorRow;
                }
            }

            if (keyboard.IsKeyPressed(Keys.End))
            {
                cursorRow = PatternRows - 1;
                if (!isPlaying)
                {
                    activeRow = cursorRow;
                    nextPlaybackRow = cursorRow;
                }
            }
        }

        private void HandleEditing()
        {
            if (!isInitialized)
            {
                return;
            }

            bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);

            if (ctrl && keyboard.IsKeyPressed(Keys.Delete))
            {
                pattern.ClearRow(cursorRow);
                return;
            }

            if (cursorField == NoteFieldIndex)
            {
                bool shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                if (keyboard.IsKeyPressed(Keys.Tab) && !shiftDown)
                {
                    InsertNoteOff();
                    return;
                }

                foreach (var key in TrackerKeyboardMap.AllMappedKeys)
                {
                    if (keyboard.IsKeyPressed(key) && TrackerKeyboardMap.TryGetSemitoneOffset(key, out int offset))
                    {
                        InsertNote(offset);
                        return;
                    }
                }

                if (keyboard.IsKeyPressed(Keys.Backspace) || keyboard.IsKeyPressed(Keys.Delete))
                {
                    pattern.ClearCell(cursorRow, cursorChannel);
                }
            }
            else
            {
                foreach (var (key, value) in HexKeyMap)
                {
                    if (keyboard.IsKeyPressed(key))
                    {
                        ApplyEffectNibble(value & 0xF);
                        return;
                    }
                }

                if (keyboard.IsKeyPressed(Keys.Backspace) || keyboard.IsKeyPressed(Keys.Delete))
                {
                    pattern.ClearEffect(cursorRow, cursorChannel);
                }
            }
        }

        private void MoveRow(int delta)
        {
            cursorRow = (cursorRow + delta + PatternRows) % PatternRows;
            if (!isPlaying)
            {
                activeRow = cursorRow;
                nextPlaybackRow = cursorRow;
            }
        }

        private void MoveHorizontal(int delta)
        {
            if (delta < 0)
            {
                if (cursorField > NoteFieldIndex)
                {
                    cursorField--;
                }
                else
                {
                    cursorChannel = (cursorChannel + PatternChannels - 1) % PatternChannels;
                    cursorField = EffectLowFieldIndex;
                }
            }
            else if (delta > 0)
            {
                if (cursorField < EffectLowFieldIndex)
                {
                    cursorField++;
                }
                else
                {
                    cursorChannel = (cursorChannel + 1) % PatternChannels;
                    cursorField = NoteFieldIndex;
                }
            }
        }

        private void InsertNote(int semitoneOffset)
        {
            int baseMidi = (currentOctave + 1) * 12;
            int midi = Math.Clamp(baseMidi + semitoneOffset, 0, 127);
            var note = TrackerNote.FromMidi(midi, currentInstrument);
            var step = pattern[cursorRow, cursorChannel];
            step.Note = note;
            pattern[cursorRow, cursorChannel] = step;

            cursorField = EffectHighFieldIndex;
        }

        private void InsertNoteOff()
        {
            var step = pattern[cursorRow, cursorChannel];
            step.Note = TrackerNote.NoteOff;
            pattern[cursorRow, cursorChannel] = step;

            cursorField = EffectHighFieldIndex;
        }

        private void ApplyEffectNibble(int nibble)
        {
            nibble &= 0xF;

            var step = pattern[cursorRow, cursorChannel];
            if (!step.Effect.Enabled)
            {
                step.Effect = TrackerEffect.FromByte(0);
            }

            byte current = step.Effect.Value;

            if (cursorField == EffectHighFieldIndex)
            {
                current = (byte)((nibble << 4) | (current & 0x0F));
                step.Effect = TrackerEffect.FromByte(current);
                pattern[cursorRow, cursorChannel] = step;
                cursorField = EffectLowFieldIndex;
            }
            else
            {
                current = (byte)((current & 0xF0) | nibble);
                step.Effect = TrackerEffect.FromByte(current);
                pattern[cursorRow, cursorChannel] = step;
                cursorField = NoteFieldIndex;
                MoveRow(1);
            }
        }

        private void StartPlayback()
        {
            if (!isInitialized)
            {
                return;
            }

            isPlaying = true;
            playbackTimer = 0;
            nextPlaybackRow = cursorRow;
            TriggerAndAdvance();
        }

        private void StopPlayback()
        {
            if (!isInitialized)
            {
                return;
            }

            isPlaying = false;
            playbackTimer = 0;
            nextPlaybackRow = cursorRow;
            activeRow = cursorRow;
            StopAllVoices();
        }

        private void AdvancePlayback(double deltaTime)
        {
            if (!isPlaying || !isInitialized)
            {
                return;
            }

            playbackTimer += deltaTime;
            double stepDuration = GetStepDurationSeconds();

            while (playbackTimer >= stepDuration)
            {
                playbackTimer -= stepDuration;
                TriggerAndAdvance();
            }
        }

        private void TriggerAndAdvance()
        {
            if (!isInitialized)
            {
                return;
            }

            int rowToPlay = nextPlaybackRow;
            TriggerRow(rowToPlay);
            activeRow = rowToPlay;

            nextPlaybackRow = (nextPlaybackRow + 1) % PatternRows;

            if (followMode)
            {
                cursorRow = activeRow;
            }
        }

        private void TriggerRow(int row)
        {
            if (!isInitialized)
            {
                return;
            }

            for (int channel = 0; channel < PatternChannels; channel++)
            {
                var step = pattern[row, channel];
                var note = step.Note;
                if (note.IsNoteOff)
                {
                    System.Diagnostics.Debug.WriteLine($"[Row {row} Ch {channel}] NoteOff: StopChannelVoice");
                    StopChannelVoice(channel);
                    continue;
                }

                if (note.IsEmpty)
                {
                    continue;
                }

                int instrumentIndex = Math.Clamp(note.Instrument, 0, instruments.Length - 1);
                float amplitude = instrumentAmps[instrumentIndex];
                var instrument = instruments[instrumentIndex];
                float frequency = note.GetFrequency();
                // Let notes ring for a long time unless a note-off or new note is encountered
                double durationSeconds = 12.0; // e.g. 12 seconds, much longer than any pattern

                if (step.Effect.Enabled)
                {
                    float semitoneOffset = step.Effect.ToSemitoneOffset();
                    frequency *= MathF.Pow(2f, semitoneOffset / 12f);
                }
                System.Diagnostics.Debug.WriteLine($"[Row {row} Ch {channel}] NewNote: StopChannelVoice, then PlayInstrumentNote freq={frequency} dur={durationSeconds}");
                StopChannelVoice(channel);
                PlayInstrumentNote(instrument, frequency, durationSeconds, amplitude, channel);
            }
        }

        private double ComputeNoteDuration(int startRow, int channel)
        {
            double stepDuration = GetStepDurationSeconds();
            int steps = 1;
            int row = (startRow + 1) % PatternRows;

            while (row != startRow)
            {
                var nextStep = pattern[row, channel];
                if (nextStep.Note.IsNoteOff)
                {
                    break;
                }

                if (!nextStep.Note.IsEmpty)
                {
                    break;
                }

                steps++;
                row = (row + 1) % PatternRows;
            }

            return Math.Max(steps * stepDuration, MinNoteDurationSeconds);
        }

        private void UpdateAudio(double deltaTime)
        {
            if (!isInitialized)
            {
                return;
            }

            while (oneShotQueue.Count > 0)
            {
                var queued = oneShotQueue.Dequeue();
                var handle = chippyBus.Play(queued.Clip, queued.Settings);
                channelVoices[queued.Channel] = handle;
                activeVoices.Add(handle);
                Console.WriteLine($"Playing note on channel {queued.Channel}");
                Console.WriteLine($"Clip Samples: {queued.Clip.TotalSamples} samples, Channels: {queued.Clip.Channels}");
                
            }

            audioEngine.Update(deltaTime);

            for (int i = activeVoices.Count - 1; i >= 0; i--)
            {
                var handle = activeVoices[i];
                if (!handle.IsPlaying)
                {
                    activeVoices.RemoveAt(i);
                    for (int channel = 0; channel < channelVoices.Length; channel++)
                    {
                        if (channelVoices[channel] == handle)
                        {
                            channelVoices[channel] = null;
                            break;
                        }
                    }
                }
            }
        }

        private void PlayInstrumentNote(Instrument instrument, float frequency, double durationSeconds, float amplitude, int channel)
        {
            double sustainSeconds = Math.Max(durationSeconds, MinNoteDurationSeconds);
            double releaseSeconds = Math.Clamp(sustainSeconds * 0.75, MinReleaseSeconds, MaxReleaseSeconds);
            double totalDurationSeconds = sustainSeconds + releaseSeconds;

            Console.WriteLine($"PlayInstrumentNote: freq={frequency} durationSeconds={durationSeconds} sustainSeconds={sustainSeconds} releaseSeconds={releaseSeconds} totalDurationSeconds={totalDurationSeconds}");

            var rawSource = instrument(frequency, (float)totalDurationSeconds, amplitude);
            using var envelopedSource = new ReleaseEnvelopeSampleSource(rawSource, releaseSeconds, disposeInner: true);
            var clip = AudioClip.FromSampleSource(envelopedSource, AudioClip.DefaultSampleRate, AudioClip.DefaultChannels);

            Console.WriteLine($"  -> clip.TotalSamples={clip.TotalSamples} channels={clip.Channels} sampleRate={clip.SampleRate}");

            var settings = AudioPlaybackSettings.Default;
            settings.Volume = Math.Clamp(amplitude, 0f, 1.5f);

            oneShotQueue.Enqueue(new QueuedNote(channel, clip, settings));
        }

        private void StopChannelVoice(int channel)
        {
            if (channel < 0 || channel >= channelVoices.Length)
            {
                return;
            }

            var handle = channelVoices[channel];
            if (handle == null)
            {
                return;
            }

            handle.Stop();
            activeVoices.Remove(handle);
            channelVoices[channel] = null;
        }

        private void StopAllVoices()
        {
            for (int channel = 0; channel < channelVoices.Length; channel++)
            {
                StopChannelVoice(channel);
            }

            Array.Clear(channelVoices, 0, channelVoices.Length);
            activeVoices.Clear();
            oneShotQueue.Clear();
        }

        private sealed class ReleaseEnvelopeSampleSource : ISampleSource
        {
            private readonly ISampleSource inner;
            private readonly long totalSamples;
            private readonly long releaseSamples;
            private readonly bool disposeInner;
            private long position;

            public ReleaseEnvelopeSampleSource(ISampleSource inner, double releaseSeconds, bool disposeInner = false)
            {
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
                WaveFormat = inner.WaveFormat;
                totalSamples = Math.Max(0, inner.Length);
                this.disposeInner = disposeInner;
                int channels = Math.Max(1, WaveFormat.Channels);
                long requestedReleaseSamples = (long)Math.Round(Math.Max(releaseSeconds, 0) * WaveFormat.SampleRate * channels);
                releaseSamples = totalSamples > 0
                    ? Math.Clamp(requestedReleaseSamples, 0, totalSamples)
                    : requestedReleaseSamples;
            }

            public WaveFormat WaveFormat { get; }

            public bool CanSeek => false;

            public long Position
            {
                get => position;
                set => throw new NotSupportedException("ReleaseEnvelopeSampleSource does not support seeking.");
            }

            public long Length => totalSamples;

            public int Read(float[] buffer, int offset, int count)
            {
                int samplesRead = inner.Read(buffer, offset, count);
                Console.WriteLine($"[ReleaseEnvelopeSampleSource] Read: samplesRead={samplesRead} totalSamples={totalSamples} releaseSamples={releaseSamples} position={position}");
                if (samplesRead <= 0 || releaseSamples <= 0 || totalSamples <= 0)
                {
                    position += Math.Max(samplesRead, 0);
                    return samplesRead;
                }

                long releaseStart = totalSamples - releaseSamples;
                for (int i = 0; i < samplesRead; i++)
                {
                    long sampleIndex = position + i;
                    if (sampleIndex >= releaseStart)
                    {
                        double progress = (double)(sampleIndex - releaseStart) / Math.Max(1, releaseSamples);
                        float gain = (float)Math.Clamp(1.0 - progress, 0.0, 1.0);
                        buffer[offset + i] *= gain;
                    }
                }

                position += samplesRead;
                return samplesRead;
            }

            public void Dispose()
            {
                if (disposeInner)
                {
                    inner.Dispose();
                }
            }
        }

        private double GetStepDurationSeconds() => 60.0 / Math.Max(1.0, bpm) * BeatsPerStep;

        private void DrawHeader(Surface surface)
        {
            surface.DrawText(10, 10, "Chippy Tracker", Colors.Yellow);

            string mode = isPlaying ? "PLAY" : "EDIT";
            surface.DrawText(10, 22, $"Mode: {mode}  BPM: {bpm:0}  Octave: {currentOctave}", Colors.White);
            surface.DrawText(10, 34, $"Instrument: {instrumentNames[currentInstrument]} (F{currentInstrument + 1})", instrumentColors[currentInstrument]);
            surface.DrawText(10, 46, $"Follow: {(followMode ? "ON" : "OFF")}  Step: 1/4 beat (16th)", Colors.White);
        }

        private void DrawPattern(Surface surface)
        {
            int visibleRows = Math.Min(PatternRows, 28);
            int anchorRow = followMode && isPlaying ? activeRow : cursorRow;
            int half = visibleRows / 2;
            int startRow = Math.Clamp(anchorRow - half, 0, Math.Max(0, PatternRows - visibleRows));
            int highlightWidth = PatternChannels * ChannelColumnWidth + 40;
            int rowHeight = 9;
            int originX = 10;
            int originY = 58;

            for (int i = 0; i < visibleRows && startRow + i < PatternRows; i++)
            {
                int rowIndex = startRow + i;
                int y = originY + i * rowHeight;

                bool playingRow = isPlaying && rowIndex == activeRow;
                bool cursorRowActive = rowIndex == cursorRow;

                if (playingRow)
                {
                    surface.DrawRect(originX - 6, y - 1, highlightWidth, rowHeight + 2, Colors.DarkGreen);
                }
                else if (cursorRowActive && blinkVisible)
                {
                    surface.DrawRect(originX - 6, y - 1, highlightWidth, rowHeight + 2, Colors.DarkGray);
                }

                surface.DrawText(originX, y, rowIndex.ToString("00"), Colors.Gray);

                for (int channel = 0; channel < PatternChannels; channel++)
                {
                    var step = pattern[rowIndex, channel];
                    var note = step.Note;
                    var effect = step.Effect;

                    string noteText = note.ToDisplay();
                    string effectText = effect.ToDisplay();

                    int channelX = originX + 28 + channel * ChannelColumnWidth;
                    int noteX = channelX;
                    int effectX = channelX + NoteColumnWidth + 6;

                    Color noteColor;
                    if (note.IsEmpty)
                    {
                        noteColor = Colors.White;
                    }
                    else if (note.IsNoteOff)
                    {
                        noteColor = Colors.Gray;
                    }
                    else
                    {
                        noteColor = instrumentColors[Math.Clamp(note.Instrument, 0, instrumentColors.Length - 1)];
                    }
                    Color effectColor = effect.Enabled ? Colors.Green : Colors.DarkGray;

                    surface.DrawText(noteX, y, noteText, noteColor);
                    surface.DrawText(effectX, y, effectText, effectColor);

                    if (cursorRowActive && channel == cursorChannel && blinkVisible)
                    {
                        if (cursorField == NoteFieldIndex)
                        {
                            surface.DrawOutlinedRect(noteX - 2, y - 2, NoteColumnWidth + 4, rowHeight + 2, Colors.Cyan);
                        }
                        else
                        {
                            surface.DrawOutlinedRect(effectX - 2, y - 2, EffectColumnWidth + 4, rowHeight + 2, Colors.Cyan);
                        }
                    }
                }
            }
        }

        private void DrawFooter(Surface surface)
        {
            int y = surface.Height - 36;
            surface.DrawText(10, y, "Space Play/Stop  Shift+Tab Follow  Tab Note-Off  Arrows Move  Z-M Notes  [ ] Octave", Colors.White);
            surface.DrawText(10, y + 12, "PgUp/PgDn Tempo  F1-F4 Instruments  Backspace/Delete Clear  Ctrl+Del Row", Colors.White);
            surface.DrawText(10, y + 24, "FX: Move to 3Cxx column with Left/Right, type 0-9/A-F for pitch detune", Colors.White);
        }

        private void SetInstrument(int index)
        {
            currentInstrument = Math.Clamp(index, 0, instruments.Length - 1);
        }

        private static Instrument CreateSquareInstrument()
        {
            return (frequency, durationSeconds, amplitude) => new SimpleSquareWaveSource(frequency, durationSeconds, amplitude * 0.8f);
        }

        private static Instrument CreateTriangleInstrument()
        {
            return (frequency, durationSeconds, amplitude) => new CustomSampleSource(
                pos =>
                {
                    const float sampleRate = 44100f;
                    float t = pos / sampleRate;
                    float phase = (t * frequency) - MathF.Floor(t * frequency);
                    float value = 1f - 4f * MathF.Abs(phase - 0.5f);
                    return value * amplitude * 0.8f;
                },
                durationSeconds);
        }

        private static Instrument CreateBassInstrument()
        {
            return (frequency, durationSeconds, amplitude) => new SimpleSquareWaveSource(Math.Max(10f, frequency / 2f), durationSeconds, amplitude);
        }

        private static Instrument CreateNoiseInstrument()
        {
            return (frequency, durationSeconds, amplitude) => SoundSynth.WhiteNoise(durationSeconds, amplitude * 0.8f);
        }
    }
}
