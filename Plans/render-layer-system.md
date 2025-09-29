# Render Layer System & Object-Level Debugging

## Goal
Introduce a prioritized render layer pipeline that lets developers control draw order explicitly and inspect individual renderables without stepping entire frames.

## Motivation
- Prevent z-order glitches by defining deterministic layer priorities (background, actors, UI, debug).
- Enable selective rendering or stepping through individual objects to simplify debugging complex frames.
- Provide groundwork for future features like render-to-texture effects or post-processing passes.

## In Scope
- Render layer abstraction with numeric priority (lower = earlier draw, higher = overlay) and named presets.
- Registration API for renderables (sprites, GUI elements, debug overlays) with optional dynamic ordering.
- Debug tooling to toggle visibility per layer and step through queued draw calls object-by-object.
- Integration points for batching so layers can still benefit from efficient GPU submissions.

## Out of Scope (initial iteration)
- Full render graph with dependencies between passes.
- 3D rendering support or depth buffer management.
- Complex culling or LOD strategies beyond existing capabilities.

## Milestones
- [ ] Audit current rendering pipeline (`Surface`, `Window`, `Gui`) and document insertion points for a layer manager.
- [ ] Design layer API (structs, manager classes) and ensure backward compatibility with existing draw calls.
- [ ] Implement layer-aware renderer with unit/integration tests verifying order correctness.
- [ ] Build debug inspector allowing per-layer toggles and object-level stepping, integrated with future overlay tools.
- [ ] Update sample games to assign sensible layer priorities (background, gameplay, UI, debug).

## Dependencies
- Rendering primitives in `Gfx` and `Window/ConsoleHost` modules.
- Planned debug overlay system for exposing layer controls.
- Future ECS or scene management systems may register renderables automatically.

## Validation
- Automated tests confirm render order matches specified priorities under mixed loads.
- Debug stepping view lets developers advance draw commands individually while frame timing remains paused.
- No regression in rendering performance; batching still groups compatible draw commands within each layer.

## Open Questions
- Should we support fractional priorities or reserved ranges for engine subsystems?
- How do we reconcile object-level stepping with time-based animations (e.g., keep animation paused or scrubbed)?
- Do we need serialization/config files for layer definitions, or keep them code-defined initially?
