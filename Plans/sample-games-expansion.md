# Sample Games Expansion

## Goal
Broaden the suite of Asmo example projects to showcase diverse genres, engine features, and best practices for structuring real-world games.

## Motivation
- Provide ready-made references for new contributors exploring engine capabilities.
- Highlight integration patterns for new systems (scene management, ECS, audio, etc.).
- Offer regression targets that guard against engine breakages.

## In Scope
- Curate 3–4 small sample games (platformer, shmup, puzzle, narrative vignette).
- Each sample should demonstrate distinct features (tilemaps, physics, shaders, GUI).
- Shared tooling for launching, packaging, and running samples from a central menu.
- Documentation explaining architectural choices and extension points.

## Out of Scope (initial wave)
- Massive content-heavy games (keep scopes tight and instructional).
- Online multiplayer samples.
- Commercial-quality art/audio asset production (use permissive placeholders).

## Milestones
- [ ] Define shortlist of target genres/features and map to engine showcases.
- [ ] Build project skeletons with shared build configuration and asset pipeline usage.
- [ ] Implement gameplay loops and polish to "delightful demo" level.
- [ ] Write walkthrough docs and integrate sample selector UI in the launcher.

## Dependencies
- Core engine features targeted by each sample (tilemaps, ECS, audio, etc.).
- Asset pipeline tooling for consistent asset packaging once available.

## Validation
- Samples run from a unified launcher with minimal setup.
- Each sample highlights at least one advanced engine feature.
- Community feedback indicates improved onboarding experience.

## Open Questions
- How do we manage shared assets/code across samples (submodules, shared libs)?
- Should samples support web builds or focus on desktop only?
- Do we want automated tests that smoke-test each sample as part of CI?
