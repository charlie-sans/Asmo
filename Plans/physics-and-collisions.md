# Physics & Collision Helpers

## Goal
Add reusable 2D collision detection and simple physics utilities to support platformers, top-down games, and arcade experiences.

## Motivation
- Avoid every game re-implementing AABB and circle collisions.
- Provide consistent collision resolution responses (slide, bounce, trigger).
- Enable simple physics behaviors (gravity, friction) without heavy third-party engines.

## In Scope
- Collision primitives (AABB, circle, line) and intersection routines.
- Broad-phase acceleration (uniform grid or sweep-and-prune) for many entities.
- Basic physics integrator with configurable forces and constraints.
- Debug visualization hooks for collision volumes.

## Out of Scope
- Full rigid-body solver with rotations and complex shapes.
- Networking synchronization of physics state.
- Destructive terrain or soft-body simulations.

## Milestones
- [ ] Define collision/physics API and integration points with ECS/scene systems.
- [ ] Implement unit-tested intersection and resolution helpers.
- [ ] Create sample demo showcasing collisions (platformer room or pong clone).
- [ ] Document usage patterns and performance considerations.

## Dependencies
- Rendering layer for debug draw overlays.
- Time step management (fixed/variable) from main loop.
- Optional ECS integration for component-driven physics.

## Validation
- Sample demo maintains stable 60 FPS with at least 100 moving colliders.
- Collision response behaves predictably across edge cases (corner hits, tunneling).
- Unit tests cover degenerate cases (zero-area shapes, overlapping starts).

## Open Questions
- Do we need continuous collision detection (CCD) in v1 or can we start with discrete?
- Should physics operate in fixed timestep separate from rendering?
- Is there value in offering deterministic mode for replay/rollback features?
