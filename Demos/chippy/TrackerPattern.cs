using System;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Chippy;

public readonly struct TrackerNote
{
    private const int EmptyMidi = -1;
    private const int NoteOffMidi = -2;

    public static TrackerNote Empty { get; } = new(EmptyMidi, 0);
    public static TrackerNote NoteOff => new(NoteOffMidi, 0);

    public int Midi { get; }
    public int Instrument { get; }

    public bool IsEmpty => Midi == EmptyMidi;
    public bool IsNoteOff => Midi == NoteOffMidi;

    public TrackerNote(int midi, int instrument)
    {
        Midi = midi;
        Instrument = instrument;
    }

    public float GetFrequency(float basePitch = 440f)
    {
        if (IsEmpty || IsNoteOff)
        {
            return 0f;
        }

        return basePitch * MathF.Pow(2f, (Midi - 69) / 12f);
    }

    public string ToDisplay()
    {
        if (IsEmpty)
        {
            return "---";
        }

        if (IsNoteOff)
        {
            return "===";
        }

        int noteIndex = ((Midi % 12) + 12) % 12;
        int octave = (Midi / 12) - 1;
        return $"{NoteNames[noteIndex]}{octave}";
    }

    public TrackerNote WithInstrument(int instrument) => new(Midi, instrument);

    public static int ClampInstrument(int instrument, int maxInstrument)
    {
        if (instrument < 0)
        {
            return 0;
        }

        if (instrument >= maxInstrument)
        {
            return maxInstrument - 1;
        }

        return instrument;
    }

    public static TrackerNote FromMidi(int midi, int instrument) => new(midi, instrument);

    public static bool TryParseName(string name, out int midi)
    {
        midi = 0;
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2)
        {
            return false;
        }

        name = name.Trim().ToUpperInvariant();
        string pitchPart = name[..^1];
        if (!int.TryParse(name[^1].ToString(), out int octave))
        {
            return false;
        }

        int noteIndex = Array.IndexOf(NoteNames, pitchPart);
        if (noteIndex < 0)
        {
            return false;
        }

        midi = (octave + 1) * 12 + noteIndex;
        return true;
    }

    private static readonly string[] NoteNames =
    {
        "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"
    };
}

public struct TrackerEffect
{
    public bool Enabled { get; set; }
    public byte Value { get; set; }

    public static TrackerEffect Disabled => new() { Enabled = false, Value = 0 };

    public static TrackerEffect FromByte(byte value) => new() { Enabled = true, Value = value };

    public string ToDisplay() => Enabled ? $"3C{Value:X2}" : "....";

    public float ToSemitoneOffset() => Enabled ? (Value - 0x80) / 16f : 0f;
}

public struct TrackerStep
{
    public TrackerNote Note;
    public TrackerEffect Effect;

    public static TrackerStep Empty => new()
    {
        Note = TrackerNote.Empty,
        Effect = TrackerEffect.Disabled
    };
}

public sealed class TrackerPattern
{
    public int Rows { get; }
    public int Channels { get; }

    private readonly TrackerStep[,] _grid;

    public TrackerPattern(int rows, int channels)
    {
        Rows = rows;
        Channels = channels;
        _grid = new TrackerStep[Rows, Channels];

        for (int row = 0; row < Rows; row++)
        {
            for (int channel = 0; channel < Channels; channel++)
            {
                _grid[row, channel] = TrackerStep.Empty;
            }
        }
    }

    public TrackerStep this[int row, int channel]
    {
        get => _grid[row, channel];
        set => _grid[row, channel] = value;
    }

    public void SetNote(int row, int channel, TrackerNote note)
    {
        var step = _grid[row, channel];
        step.Note = note;
        _grid[row, channel] = step;
    }

    public void SetEffect(int row, int channel, TrackerEffect effect)
    {
        var step = _grid[row, channel];
        step.Effect = effect;
        _grid[row, channel] = step;
    }

    public void ClearRow(int row)
    {
        for (int channel = 0; channel < Channels; channel++)
        {
            _grid[row, channel] = TrackerStep.Empty;
        }
    }

    public void ClearCell(int row, int channel)
    {
        _grid[row, channel] = TrackerStep.Empty;
    }

    public void ClearEffect(int row, int channel)
    {
        var step = _grid[row, channel];
        step.Effect = TrackerEffect.Disabled;
        _grid[row, channel] = step;
    }

    public void FillRow(int row, TrackerNote note)
    {
        for (int channel = 0; channel < Channels; channel++)
        {
            var step = _grid[row, channel];
            step.Note = note;
            step.Effect = TrackerEffect.Disabled;
            _grid[row, channel] = step;
        }
    }

    public IEnumerable<(int row, TrackerStep[] steps)> EnumerateRows()
    {
        for (int row = 0; row < Rows; row++)
        {
            var copy = new TrackerStep[Channels];
            for (int channel = 0; channel < Channels; channel++)
            {
                copy[channel] = _grid[row, channel];
            }

            yield return (row, copy);
        }
    }
}

public static class TrackerKeyboardMap
{
    private static readonly Dictionary<Keys, int> KeyOffsets = new()
    {
        { Keys.Z, 0 },
        { Keys.S, 1 },
        { Keys.X, 2 },
        { Keys.D, 3 },
        { Keys.C, 4 },
        { Keys.V, 5 },
        { Keys.G, 6 },
        { Keys.B, 7 },
        { Keys.H, 8 },
        { Keys.N, 9 },
        { Keys.J, 10 },
        { Keys.M, 11 },
        { Keys.Comma, 12 },
        { Keys.L, 13 },
        { Keys.Period, 14 },
        { Keys.Semicolon, 15 },
        { Keys.Slash, 16 }
    };

    public static bool TryGetSemitoneOffset(Keys key, out int offset) => KeyOffsets.TryGetValue(key, out offset);

    public static IEnumerable<Keys> AllMappedKeys => KeyOffsets.Keys;
}
