# Asset Pipeline Tooling

## Goal
Create a repeatable asset build pipeline that ingests raw art/audio/script files and outputs optimized bundles ready for fast loading in Asmo games.

## Motivation
- Prevent runtime surprises caused by missing or outdated assets.
- Optimize memory usage through atlas packing and compression.
- Support versioning and hot-reload to speed up iteration for designers.

## In Scope
- Command-line tool (or MSBuild task) that processes sprites, fonts, audio, and data files.
- Manifest format describing asset groups, variants, and dependencies.
- Incremental rebuilds based on file hashes and watch mode for rapid iteration.
- Integration hooks for tilemaps and scene data once those features land.

## Out of Scope (initially)
- GUI-based asset manager.
- Cloud-based asset distribution or patching.
- Complex localization workflows (note as future extension).

## Milestones
- [ ] Define manifest schema and integration with `GameEnvironment.AssetRoot`.
- [ ] Implement pipeline prototype covering spritesheets and audio compression.
- [ ] Add watch mode and document workflow for artists/developers.
- [ ] Hook into sample projects, replacing manual asset copies.

## Dependencies
- File I/O utilities and serialization layer (JSON/YAML? to be decided).
- Third-party libraries for encoding (PNG quantization, audio compression) if needed.

## Validation
- Pipeline reduces sample project load time by measurable margin (track before/after).
- Assets rebuild correctly when modified; unchanged assets remain cached.
- Pipeline failures provide actionable error messages.

## Open Questions
- Which compression/packing libraries are acceptable for project licensing?
- Should assets be bundled per platform (Windows/Linux) or shared?
- Do we need pluggable steps so contributors can add custom processors?
