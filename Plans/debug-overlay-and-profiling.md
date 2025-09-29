# Debug Overlay & Profiling Tools

## Goal
Deliver an in-engine overlay that surfaces real-time performance metrics, input diagnostics, and developer toggles to accelerate debugging.

## Motivation
- Quickly spot frame time spikes, draw-call bottlenecks, and memory usage.
- Visualize input state and collision bounds without leaving the game.
- Provide hooks for future automated capture/profiling workflows.

## In Scope
- Toggleable overlay layered on top of existing rendering (keyboard shortcut).
- Metrics: FPS (avg/1% low), CPU/GPU frame breakdown, draw call counts, memory usage.
- Panels for input state, audio channels, scene stack, and ECS statistics once available.
- Screenshot/log capture of overlay data for regression tracking.

## Out of Scope
- Full flamegraph or trace recording (defer to integration with external profilers).
- Remote network debugging UI.
- Editor window embedding (focus on runtime overlay first).

## Milestones
- [ ] Identify most valuable metrics and data sources across engine subsystems.
- [ ] Prototype overlay rendering layer with minimal perf impact (<1ms budget).
- [ ] Implement data collectors and ensure thread-safe updates.
- [ ] Document keyboard shortcuts, configuration, and extension points.

## Dependencies
- Rendering pipeline for overlay drawing (likely `Surface`/GUI modules).
- Access to timing info and resource stats from engine core.

## Validation
- Overlay updates in real time without notable performance degradation.
- Metrics verified against external tools (Task Manager, Visual Studio profiler).
- Developers can export snapshot for bug reports.

## Open Questions
- Do we include profiling markers API for game-level instrumentation?
- Should overlay layout be customizable via JSON config?
- How do we persist overlay state between sessions (if needed)?
