# Audio Mixer & Effects

## Goal
Upgrade Asmo's audio stack with a channel-based mixer, effect chain, and quality-of-life tooling for composing chiptune soundtracks and sound effects.

## Motivation
- Allow simultaneous playback of music, ambient loops, and SFX with independent levels.
- Provide creative tools (filters, envelopes, arpeggios) without relying on external DAWs.
- Improve runtime control (pause, fade, ducking) for polished audio experiences.

## In Scope
- Mixer abstraction with configurable buses and per-bus volume/pan.
- Effect modules: simple EQ, delay/echo, vibrato, and automatic arpeggiator.
- Scripting or data-driven envelopes for chip instruments.
- Debug audio panel (CLI or in-engine overlay) to monitor active channels.

## Out of Scope
- Full-fledged tracker UI embedded in the engine.
- Spatialized/3D audio calculations.
- Streaming large audio files from disk (defer to later release).

## Milestones
- [ ] Audit existing `ChipTunePlayer` and identify refactoring points.
- [ ] Implement mixer core with unit tests for gain staging and clipping.
- [ ] Add effect modules and integrate with sample soundtrack.
- [ ] Document composer workflow and expose runtime controls to games.

## Dependencies
- Current audio playback primitives and sample loaders.
- Potential third-party math/FFT libraries depending on effects implementation.

## Validation
- Mixer can handle at least 16 concurrent voices without audible artifacts.
- Effects chain configurable per-bus and can be toggled at runtime.
- Sample game demonstrates music fade-out and SFX ducking during dialogue.

## Open Questions
- Do we target fixed-point or floating-point processing for performance/compatibility?
- Should we expose Lua/JSON descriptors for soundscapes?
- Is real-time waveform visualization useful for debugging?
