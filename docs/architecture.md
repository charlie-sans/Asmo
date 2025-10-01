# Asmo Architecture Overview

This document explains the moving pieces of the Asmo framework so you can orient quickly, extend safely, and know where upcoming roadmap work will land.

## High-Level Goals
Asmo provides a deliberately small, hackable 2D console-style environment. The design favors:
- Deterministic, frame-based update loop
- Explicit surfaces / blits instead of retained scene graphs
- Progressive enhancement (features can be layered on without rewriting the core)

## Core Runtime Flow
```
+--------------------+          +-------------------------+
|  Program / Host    |  creates |  Window (GameWindow)    |
+--------------------+ -------->+-------------------------+
         |                             |
         | per frame                   |
         v                             v
+--------------------+        +-------------------------+
|  ConsoleHost       |<------>|  Input (OpenTK events)  |
+--------------------+        +-------------------------+
         | update()                     |
         v                              |
+--------------------+        +-------------------------+
| Active Game (IConsoleGame)  |  Future: SceneManager   |
+-----------------------------+-------------------------+
                |
                v
         +-------------+
         |  Surface    |  (CPU pixel buffer)
         +-------------+
                |
                v upload
         +-------------+
         | OpenGL Tex  |
         +-------------+
                |
                v
           Backbuffer
```

## Key Components
| Component | Purpose | Notes |
|-----------|---------|-------|
| `Window` | Wraps OpenTK GameWindow; handles GL setup and pixel texture blitting | Will host debug overlay, future render layers |
| `Surface` | CPU-side pixel buffer (RGBA) with simple drawing primitives | Software rendering entry point |
| `ConsoleHost` | Glue object connecting input, game lifecycle, and surfaces | Could broker services (audio, scenes) later |
| `IConsoleGame` | Minimal contract a demo/game implements | Keep lean to avoid tight coupling |
| `Gui` (immediate mode) | Simple widget helpers rendered into a `Surface` | Candidate for future theme/layout polish |
| `Audio*` classes | Current audio playback pipeline | Mixer/FX refactor planned (see roadmap) |
| `Scenes/*` | New scene stack & transitions | Documentation/migration still in progress |
| `AsmoMASM` (separate project) | Micro-assembly runtime & tooling | Integration layer not yet merged into core |

## Update / Render Loop (Current)
1. OpenTK pumps window events.
2. `OnUpdateFrame` → `ConsoleHost.Update()` → active game `Update()`.
3. Game draws into its assigned `Surface` (and/or additional surfaces in future layering system).
4. Modified regions uploaded to a persistent GL texture.
5. A fullscreen quad is drawn to the backbuffer.

Planned evolution:
- Introduce a `RenderLayer` abstraction so multiple surfaces (game, UI, overlay, post-effects) can be composed predictably.
- Add optional hardware shader effects between layer composition and final blit.

## Input Flow
OpenTK raises keyboard (and later gamepad) events → Window stores current state → `ConsoleHost` exposes polling helpers (e.g. `IsKeyDown`). A dedicated input module is planned to centralize device handling and mapping.

## Audio (Pre-Refactor State)
The existing audio code provides clip playback and basic handles. The roadmap refactor will add:
- Bus graph (Master → Music/SFX/Ambience…)
- Effect chain (EQ, delay, vibrato, envelopes)
- Mixer metrics for the debug overlay

## Scenes & Transitions (In Progress)
Scenes encapsulate game states (menu, gameplay, pause). The manager holds a stack and coordinates transitions (fade, slide, etc.). Remaining work:
- Documentation + demo migration
- Cleanup of legacy direct game loading paths

## MASM Integration (Planned)
Integration aims to let MASM scripts:
- Write into a surface or sub-surface
- Read shared memory blocks (input state, timers)
- Trigger events (play audio, swap scene)

First milestone will expose a `MasnHostService` (placeholder name) registered with `ConsoleHost` or a service locator.

## Extensibility Principles
- Keep interfaces small (`IConsoleGame`, future `IScene`)
- Favor composition over inheritance (e.g. surface layers instead of a giant renderer class)
- Provide opt-in diagnostics (debug overlay disabled by default in release builds)

## Threading Model
Currently largely single-threaded (update + render on main thread). Future safe parallelization candidates:
- Audio mixing (already typically separate or callback based)
- Asset decode / pipeline (background tasks feeding main thread staging buffers)

## Error Handling Philosophy
- Fail fast in development (exceptions bubble) unless a subsystem can degrade safely (e.g., missing optional font file → fallback font).
- Provide structured logs for: asset load errors, audio device issues, shader compile failures (post shader-library introduction).

## Planned Architecture Additions
| Feature | Architectural Hook |
|---------|--------------------|
| Debug Overlay | Late render pass using highest layer, aggregated metrics services |
| Render Layers | Ordered list with clear rules; dependency: simple compositor |
| Asset Pipeline | Build-time packer + runtime index/cache service |
| Shader Library | Registry + hot reload watcher (dev only) |
| Tilemap Support | Loader + draw system potentially using its own intermediate surface |
| ECS Foundations | Opt-in module layering on top of scenes (avoid coupling core types) |

## Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Overgrowth of `Window` class | Split rendering, input, overlay orchestration into dedicated managers |
| Per-frame GC pressure from text/dynamic allocations | Pool buffers (string builders, temporary sprite lists) |
| Tight coupling of upcoming systems | Enforce service boundary / interfaces (e.g., `IAudioMixer`, `IRenderLayer`) |

## Diagram: Future Layer Composition
```
[ Scene Surface ]
[ GUI Surface   ]  --+-->  Layer Combiner  -->  (Optional Post FX) --> Final Blit
[ Overlay HUD   ]  --+
```

## When To Add a New Abstraction
Add one if ALL apply:
1. It reduces duplicate logic across ≥2 demos/features.
2. It makes a performance or clarity improvement measurable in profiling.
3. It does not lock future design (easy to remove or bypass).

## Contributing Architecture Changes
1. Open a short proposal (1–2 paragraphs) referencing the roadmap epic.
2. Describe interfaces + data flow diagrams (ASCII is fine).
3. Provide a small demo or test verifying the behavior.
4. Keep diffs focused (avoid large opportunistic refactors in the same PR).

---
Questions or improvements? Open an issue referencing this doc.
