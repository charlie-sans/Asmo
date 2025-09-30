# Chippy Tracker

Chippy is a tiny, four-channel chiptune tracker demo built on the Asmo framework. It lets you sketch quick melodies, lay down bass lines, and trigger crunchy noise percussion straight from the keyboard.

## Features

- Pattern grid with 32 rows × 4 channels
- Real-time playback with square, triangle, bass, and noise instruments
- Tracker-style keyboard note entry (computer keys map to semitone steps)
- Adjustable tempo, octave, instrument assignment, and follow mode
- Per-step pitch detune effect (`3Cxx`) with hex input

## Controls

| Action | Keys |
| --- | --- |
| Play / stop | `Space` |
| Toggle follow cursor | `Shift + Tab` |
| Insert note-off | `Tab` |
| Move cursor | Arrow keys |
| Jump to top / bottom | `Home` / `End` |
| Enter note | `Z X C V B N M , . /` (use `S`, `D`, `G`, `H`, `J`, `L`, `;` for sharps) |
| Change octave | `[` / `]` |
| Change tempo | `PageUp` / `PageDown` (±5 BPM) |
| Select instrument | `F1`–`F4` |
| Clear cell | `Backspace` or `Delete` |
| Clear row | `Ctrl + Delete` |
| Edit effect nibble | Move cursor to effect column (`←` / `→`), then type `0`-`9` or `A`-`F` |
| Clear effect | Select effect column, press `Backspace` or `Delete` |

## Instruments

| Slot | Name | Description |
| --- | --- | --- |
| F1 | Square Lead | Bright pulse wave lead |
| F2 | Triangle | Smooth triangle voice |
| F3 | Bass | Square wave shifted an octave down |
| F4 | Noise | White-noise percussion burst |

## Tips

- Notes inherit the instrument that is active when you place them.
- Follow mode keeps the view centered on the playhead during playback; toggle it off to edit elsewhere while the pattern loops.
- Tempo steps are sixteenth notes (0.25 beats) by default—perfect for rapid arps or classic chip grooves.
- The `3Cxx` effect detunes the note: `80` is neutral, lower values bend down, higher values bend up. Each unit ≈ 1/16th of a semitone.
- Use `Tab` on a note cell to insert a note-off (`===`) so sustained notes can ring across rows until you explicitly release them.

Have fun tracking!
