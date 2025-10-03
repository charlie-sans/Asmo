using System;
using System.Collections.Generic;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using Asmo.Window.input;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chippy
{
    // Chippy, a chiptune tracker for Asmo

    public class Game : IConsoleGame
    {
        // Cache for background gradient row colors
        private static Color[] gradientCache = null;

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

        private readonly Color[] instrumentColors =
        {
            Colors.Cyan,
            Colors.Magenta,
            Colors.Yellow,
            Colors.Orange
        };

        private Keyboard keyboard = null!;
        private Mouse mouse = null!;
        private AudioEngine audioEngine = null!;
        private AudioBus chippyBus = null!;
        private readonly List<AudioHandle> activeVoices = new();
        private readonly Queue<QueuedNote> oneShotQueue = new();
        private readonly AudioHandle[] channelVoices = new AudioHandle[PatternChannels];
        private readonly bool[] channelMute = new bool[PatternChannels];
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


    private static readonly string[] instrumentNames = new[] { "Square", "Triangle", "Bass", "Noise" };
    private static readonly float[] instrumentAmps = new[] { 1.0f, 1.0f, 1.0f, 1.0f };

        public Game() { }

        public void Init(Surface surface)
        {
            keyboard = new Keyboard(surface.Window);
            mouse = new Mouse(surface.Window);
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

            // --- Layout variables for tracker and mixer (shared with Draw) ---
            int visibleRows = Math.Min(PatternRows, 28);
            int highlightWidth = PatternChannels * ChannelColumnWidth + 40;
            int rowHeight = 9;
            int headerHeight = rowHeight + 2;
            int patternHeight = visibleRows * rowHeight + headerHeight;
            int patternX = (640 - (highlightWidth + 12)) / 2; // fallback width, adjust if you have actual surface width
            int patternY = 58 - headerHeight - 8;
            int mixerY = patternY + patternHeight + 28;

            UpdateBlink(deltaTime);
            HandleInput();
            AdvancePlayback(deltaTime);
            UpdateAudio(deltaTime);

            keyboard.Update();
            audioEngine.Update(deltaTime);

            // --- Mixer mute button click logic ---
            if (mouse != null && mouse.IsPressed)
            {
                for (int channel = 0; channel < PatternChannels; channel++)
                {
                    int channelX = patternX + 38 + channel * ChannelColumnWidth;
                    int muteBtnX = channelX + 16;
                    int muteBtnY = mixerY + 12;
                    if (mouse.X >= muteBtnX && mouse.X < muteBtnX + 16 && mouse.Y >= muteBtnY && mouse.Y < muteBtnY + 16)
                    {
                        channelMute[channel] = !channelMute[channel];
                    }
                }
            }
        }

        public void Draw(Surface surface)
        {
            // --- Optimized Background: cache gradient row colors ---
            if (gradientCache == null || gradientCache.Length != surface.Height)
            {
                gradientCache = new Color[surface.Height];
                for (int y = 0; y < surface.Height; y++)
                {
                    int r = 16 + (int)(32 * y / (float)surface.Height);
                    int g = 18 + (int)(36 * y / (float)surface.Height);
                    int b = 32 + (int)(48 * y / (float)surface.Height);
                    gradientCache[y] = new Color(r, g, b, 255);
                }
            }
            for (int y = 0; y < surface.Height; y++)
            {
                Color rowColor = gradientCache[y];
                for (int x = 0; x < surface.Width; x++)
                    surface.SetPixel(x, y, rowColor);
            }

            // Center the tracker pattern horizontally
            int visibleRows = Math.Min(PatternRows, 28);
            int highlightWidth = PatternChannels * ChannelColumnWidth + 40;
            int rowHeight = 9;
            int headerHeight = rowHeight + 2;
            int patternHeight = visibleRows * rowHeight + headerHeight;
            int patternX = (surface.Width - (highlightWidth + 12)) / 2;
            int patternY = 58 - headerHeight - 8;

            // --- Title Bar ---
            int titleBarHeight = 28;
            surface.DrawRect(patternX - 12, patternY - titleBarHeight - 8, highlightWidth + 36, titleBarHeight, new Color(32, 36, 56, 255));
            surface.DrawText(patternX + 12, patternY - titleBarHeight - 2, "Chippy Tracker", Colors.Yellow);

            // --- Tracker Background ---
            surface.DrawRect(patternX - 12, patternY - 12, highlightWidth + 36, patternHeight + 32, new Color(18, 20, 32, 255));

            // --- Tracker Pattern ---
            DrawPattern(surface, patternX + 10);

            // --- Mixer UI ---
            int mixerY = patternY + patternHeight + 28;
            int mixerHeight = 40;
            surface.DrawRect(patternX - 12, mixerY, highlightWidth + 36, mixerHeight, new Color(24, 26, 38, 255));
            for (int channel = 0; channel < PatternChannels; channel++)
            {
                int channelX = patternX + 38 + channel * ChannelColumnWidth;
                // Draw volume bar (dummy value for now)
                int volBarHeight = 24;
                int volBarWidth = 8;
                int volBarY = mixerY + 8;
                int vol = channelMute[channel] ? 0 : 18; // Show 0 if muted
                surface.DrawRect(channelX, volBarY + (volBarHeight - vol), volBarWidth, vol, channelMute[channel] ? Colors.DarkGray : Colors.Green);
                surface.DrawOutlinedRect(channelX, volBarY, volBarWidth, volBarHeight, Colors.DarkGray);
                // Draw mute button (clickable)
                int muteBtnX = channelX + 16;
                int muteBtnY = mixerY + 12;
                bool mouseOverMute = mouse != null && mouse.X >= muteBtnX && mouse.X < muteBtnX + 16 && mouse.Y >= muteBtnY && mouse.Y < muteBtnY + 16;
                Color muteColor = channelMute[channel]
                    ? (mouseOverMute ? new Color(180, 60, 60, 255) : new Color(120, 30, 30, 255))
                    : (mouseOverMute ? new Color(90, 90, 90, 255) : new Color(60, 60, 60, 255));
                surface.DrawRect(muteBtnX, muteBtnY, 16, 16, muteColor);
                surface.DrawText(muteBtnX + 2, muteBtnY + 2, channelMute[channel] ? "X" : "M", Colors.White);
            }

            // --- Footer ---
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

                int instrumentIndex = Math.Clamp(note.Instrument, 0, instrumentNames.Length - 1);
                float amplitude = instrumentAmps[instrumentIndex];
                float frequency = note.GetFrequency();
                // Derive an actual musical duration from upcoming empty rows so we don't allocate giant 12s clips every time.
                double durationSeconds = ComputeNoteDuration(row, channel);

                if (step.Effect.Enabled)
                {
                    float semitoneOffset = step.Effect.ToSemitoneOffset();
                    frequency *= MathF.Pow(2f, semitoneOffset / 12f);
                }
                System.Diagnostics.Debug.WriteLine($"[Row {row} Ch {channel}] NewNote: StopChannelVoice, then PlayInstrumentNote freq={frequency} dur={durationSeconds}");
                StopChannelVoice(channel);
                PlayInstrumentNote(instrumentIndex, frequency, durationSeconds, amplitude, channel);
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
                // Advance one row at a time (previously jumped 4, shrinking duration incorrectly)
                row = (row + 1) % PatternRows;
            }

            double sustain = Math.Max(steps * stepDuration, MinNoteDurationSeconds);
            return sustain;
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

        private void PlayInstrumentNote(int instrumentIndex, float frequency, double durationSeconds, float amplitude, int channel)
        {
            double sustainSeconds = Math.Max(durationSeconds, MinNoteDurationSeconds);
            double releaseSeconds = Math.Clamp(sustainSeconds * 0.75, MinReleaseSeconds, MaxReleaseSeconds);
            double totalDurationSeconds = sustainSeconds + releaseSeconds;

            Console.WriteLine($"PlayInstrumentNote: freq={frequency} durationSeconds={durationSeconds} sustainSeconds={sustainSeconds} releaseSeconds={releaseSeconds} totalDurationSeconds={totalDurationSeconds}");
            var clip = GenerateInstrumentClip(instrumentIndex, frequency, totalDurationSeconds, (float)amplitude, (float)releaseSeconds);
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

        // Generate instrument-specific clip using ProceduralSynth
        private AudioClip GenerateInstrumentClip(int instrumentIndex, float frequency, double totalDurationSeconds, float amplitude, float releaseSeconds)
        {
            var env = new ProceduralSynth.ADSR(0.005f, 0.05f, 0.75f, releaseSeconds <= 0 ? 0.01f : releaseSeconds);
            ProceduralSynth.Waveform wf = ProceduralSynth.Waveform.Sine;
            switch (instrumentIndex)
            {
                case 0: wf = ProceduralSynth.Waveform.Square; break; // Square
                case 1: wf = ProceduralSynth.Waveform.Triangle; break; // Triangle
                case 2: wf = ProceduralSynth.Waveform.Square; frequency = Math.Max(10f, frequency / 2f); break; // Bass (lower square)
                case 3: wf = ProceduralSynth.Waveform.Noise; frequency = 440f; break; // Noise ignores frequency
            }
            return ProceduralSynth.GenerateTone(frequency, totalDurationSeconds, amplitude, wf, env);
        }

    private double GetStepDurationSeconds() => 60.0 / Math.Max(1.0, bpm) * BeatsPerStep;

        private void DrawHeader(Surface surface)
        {
            string mode = isPlaying ? "PLAY" : "EDIT";
            surface.DrawText(10, 10, $"Mode: {mode}  BPM: {bpm:0}  Octave: {currentOctave}", Colors.White);
            surface.DrawText(10, 22, $"Instrument: {instrumentNames[currentInstrument]} (F{currentInstrument + 1})", instrumentColors[currentInstrument]);
            surface.DrawText(10, 34, $"Follow: {(followMode ? "ON" : "OFF")}  Step: 1/4 beat (16th)", Colors.White);
        }

    private void DrawPattern(Surface surface, int originX = 10)
        {
            int visibleRows = Math.Min(PatternRows, 28);
            int anchorRow = followMode && isPlaying ? activeRow : cursorRow;
            int half = visibleRows / 2;
            int startRow = Math.Clamp(anchorRow - half, 0, Math.Max(0, PatternRows - visibleRows));
            int highlightWidth = PatternChannels * ChannelColumnWidth + 40;
            int rowHeight = 9;
            int originY = 58;
            int headerHeight = rowHeight + 2;

            // Draw border around the pattern area
            int patternHeight = visibleRows * rowHeight + headerHeight;
            surface.DrawOutlinedRect(originX - 8, originY - headerHeight, highlightWidth + 12, patternHeight + 6, Colors.Gray);

            // Draw channel headers
            for (int channel = 0; channel < PatternChannels; channel++)
            {
                int channelX = originX + 28 + channel * ChannelColumnWidth;
                surface.DrawText(channelX, originY - headerHeight + 2, $"CH{channel + 1}", Colors.Yellow);
            }

            // Draw vertical separators between channels
            for (int channel = 1; channel < PatternChannels; channel++)
            {
                int sepX = originX + 28 + channel * ChannelColumnWidth - 8;
                surface.DrawLine(sepX, originY - headerHeight, sepX, originY + visibleRows * rowHeight, Colors.DarkGray);
            }

            // Draw pattern rows
            for (int i = 0; i < visibleRows && startRow + i < PatternRows; i++)
            {
                int rowIndex = startRow + i;
                int y = originY + i * rowHeight;

                bool playingRow = isPlaying && rowIndex == activeRow;
                bool cursorRowActive = rowIndex == cursorRow;

                // Alternating row backgrounds
                if (i % 2 == 0)
                {
                    surface.DrawRect(originX - 6, y - 1, highlightWidth, rowHeight + 2, new Color(30, 30, 40, 255));
                }

                if (playingRow)
                {
                    surface.DrawRect(originX - 6, y - 1, highlightWidth, rowHeight + 2, Colors.DarkGreen);
                }
                else if (cursorRowActive && blinkVisible)
                {
                    surface.DrawRect(originX - 6, y - 1, highlightWidth, rowHeight + 2, Colors.DarkGray);
                }

                // Row number
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
            currentInstrument = Math.Clamp(index, 0, instrumentNames.Length - 1);
        }
    }
}
