# Tilemap Support

## Goal
Provide tooling to import, render, and interact with tile-based levels, accelerating development of platformers, RPGs, and puzzle games.

## Motivation
- Streamline level creation with external editors like Tiled.
- Standardize collision and metadata layers for consistent gameplay logic.
- Enable efficient rendering of large tile worlds via batching.

## In Scope
- Importer for Tiled JSON maps (orthogonal focus initially).
- Tile atlas management with shared textures and palettes.
- Runtime tilemap renderer with culling and layer ordering.
- Optional collision layer parsing into physics system.

## Out of Scope (v1)
- Isometric or hexagonal map support.
- In-engine tile editor UI.
- Runtime tile streaming for extremely large worlds.

## Milestones
- [ ] Align on supported map formats and metadata conventions.
- [ ] Implement importer and data structures with automated tests using fixture maps.
- [ ] Build renderer prototype and integrate with scene/physics demos.
- [ ] Write developer guide plus sample level showcasing parallax layers.

## Dependencies
- Asset pipeline for loading atlas textures and map JSON.
- Rendering module for batching/drawing tiles.
- Optional physics system for collision layer consumption.

## Validation
- Sample level renders at 60 FPS with large (e.g., 256x256) map.
- Collision layer correctly interacts with physics demo actors.
- Map reload support works at runtime (for editor iteration).

## Open Questions
- Should we cache baked tile chunks to minimize draw calls?
- How do we expose tile metadata (scripts, triggers) to gameplay code?
- Do we need animation support for autotiles in v1?
