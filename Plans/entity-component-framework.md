# Entity Component System (ECS) Lite

## Goal
Deliver a pragmatic component-based architecture that helps games manage many interactive objects with consistent update and render lifecycles.

## Motivation
- Reduce boilerplate for spawning, updating, and drawing entities.
- Encourage composition over inheritance for game actors.
- Improve performance via batch updates and spatial partition helpers.

## In Scope
- Core ECS primitives: `Entity`, `Component`, `System` interfaces/classes.
- Scheduler that wires ECS update calls into the existing game loop.
- Basic component library (transform, sprite renderer, velocity, input listener).
- Optional spatial index (grid or quadtree) for query optimization.

## Out of Scope (initially)
- Jobified multithreaded systems.
- Serialization of entities/components.
- Scripting integration for runtime component hot-swap.

## Milestones
- [ ] Sketch ECS architecture diagram and API surface.
- [ ] Implement minimal runtime with unit tests covering entity creation/lifecycle.
- [ ] Port a sample feature (e.g., moving enemy) from existing sample game to ECS.
- [ ] Capture best practices in docs and add profiling hooks for system timings.

## Dependencies
- Rendering (`Surface`, `Gui`) for component outputs.
- Input subsystems for systems that consume keyboard/mouse events.

## Validation
- Profiling shows ECS overhead acceptable (<5% frame time increase) on sample scenes.
- Components can be added/removed at runtime without crashes or leaks.
- Sample game parity maintained after ECS migration.

## Open Questions
- Should systems be allowed to run at different rates (fixed vs variable timestep)?
- Do we need authoring helpers for grouping entities (prefabs/blueprints)?
- Is there appetite for eventually swapping to a data-oriented layout (struct-of-arrays)?
