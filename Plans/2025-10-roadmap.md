# Asmo Roadmap (Q4 2025 → Q1 2026)

This roadmap consolidates existing plan files, marks completed work, and introduces new, higher-level epics. Individual plan briefs can still live beside this document for deeper dives.

## Legend
- ✅ Done
- 🔄 In Progress / Partial
- 🆕 Planned / Not Started
- 💤 Deferred / Out of Scope for this cycle

---
## Recently Completed (Do not re-plan unless regression arises)
- ✅ Scene Management core (API, manager, transitions prototype) — see `scene-management-system.md` (last remaining task: docs/migration)
- ✅ MASM integration architecture decisions (open questions resolved in `masm-integration.md`)

## Epics & Status
### 1. Documentation & Tutorials (Developer Onboarding)
Status: 🔄
Goals:
- Unified quickstart
- Minimal runnable template
- MASM + Scene system walkthrough
Key Tasks:
- [ ] Author Quickstart in root README
- [ ] Add "Your First Game" step-by-step
- [ ] Create MASM scripting tutorial
- [ ] API reference generation (xml -> md pipeline) (optional)

### 2. Debug Overlay & Observability
Status: 🆕
- Folding in prior profiling overlay plan
Key Tasks:
- [ ] Minimal metrics HUD (FPS, frame time avg/1% low)
- [ ] Draw-call / surface blit counts
- [ ] Input state panel
- [ ] Audio channel summary (depends on mixer)
- [ ] Toggle & layout config

### 3. Audio Mixer & Effects
Status: 🆕
- Based on `audio-mixer-and-effects.md`
Key Tasks:
- [ ] Audit current audio stack
- [ ] Implement mixer graph (buses & routing)
- [ ] Add core effects (EQ (2-band), delay, vibrato/chorus, envelope)
- [ ] Per-bus volume/pan + ducking API
- [ ] Debug panel hooks

### 4. Render Layer System Refactor
Status: 🆕
- Generalize composition of surfaces/layers for post-processing & UI stacking.
Key Tasks:
- [ ] Define layer abstraction (order, clear rules, blend mode)
- [ ] Migrate existing GUI + framebuffer path
- [ ] Add simple post-pass example (scanline / CRT)
- [ ] Document usage

### 5. Asset Pipeline Tooling
Status: 🆕
Goals:
- Deterministic packing of fonts, sprites, audio into bundles
- Hash-based rebuild avoidance
Key Tasks:
- [ ] Specify bundle manifest format
- [ ] Implement packer CLI (import -> processed assets)
- [ ] Runtime loader with lazy surface/audio decode
- [ ] Cache invalidation & hot reload stub

### 6. Shader Library & Effects
Status: 🆕
Key Tasks:
- [ ] Central shader registry file
- [ ] Shared includes (color utils, crt, blur)
- [ ] Hot reload support (dev only)
- [ ] Example effect: palette shift

### 7. GUI Polish & Layout QoL
Status: 🆕
Key Tasks:
- [ ] Theming system expansion (dark/light + custom palette)
- [ ] Flexible layout (grid / stack containers)
- [ ] Focus navigation helpers (keyboard/gamepad)
- [ ] Text rendering: kerning + inline color spans

### 8. Tilemap & World Support (Early Slice)
Status: 🆕
Scope (minimum viable):
- Static tile layer loader
- Simple collision metadata
- Camera scrolling
Key Tasks:
- [ ] Define tilemap JSON (or TMX subset) schema
- [ ] Loader -> surface blit
- [ ] Collision proxy structure
- [ ] Camera/viewport integration

### 9. ECS (Entity Component System) Foundations
Status: 💤 (Deferred until after Debug Overlay + Audio Mixer)
Rationale: Avoid premature abstraction before existing demos stress current patterns.

---
## Dependency Map (High-Level)
- Debug Overlay depends on: basic render layering + audio mixer metrics + input API
- Audio Mixer desirable before Overlay’s audio panel
- Render Layer System precedes Shader Library & Overlay polish
- Asset Pipeline unblocks faster iteration for shader effects & tilemap assets

---
## Suggested Order of Execution
1. Documentation & Tutorials (baseline clarity)
2. Render Layer System (core architectural enabler)
3. Audio Mixer & Effects (unblocks overlay metrics)
4. Debug Overlay (observability while building later features)
5. Asset Pipeline Tooling
6. Shader Library
7. GUI Polish
8. Tilemap Slice
9. ECS (re-evaluate after 1–8)

---
## Trimming Legacy Plan Files
Once this roadmap stabilizes, you can:
- Merge resolved sections from old briefs into a `/docs` folder
- Delete stale plan files (or move to `Plans/archive/`)
- Keep only active epics + any deep-design briefs in progress

---
## Next Immediate Actions (Concrete)
- [ ] Confirm ordering or adjust based on any hidden dependencies
- ✅ Create `docs/quickstart.md`
- ✅ Create `docs/architecture.md`
- ✅ Create `docs/masm-getting-started.md` (preview)
- [ ] Start Render Layer abstraction sketch (minimal interface)

---
Feel free to edit inline or break out any epic into its own sub-plan if it grows beyond 1 page.
