# Documentation & Tutorials Overhaul

## Goal
Transform Asmo's documentation into a comprehensive, developer-friendly resource with guided tutorials, API references, and onboarding checklists.

## Motivation
- Lower the barrier for newcomers exploring the framework.
- Keep pace with new features by providing living documentation.
- Encourage community contributions through transparent guidelines.

## In Scope
- Structured docs site plan (e.g., mkdocs/docusaurus) with navigation hierarchy.
- Step-by-step tutorials ("Hello World", "Platformer", "Audio Remix").
- API reference generation pipeline from XML docs or source annotations.
- Contribution guide enhancements (code style, testing expectations, review flow).

## Out of Scope (initial phase)
- Full localization of documentation (note as future aspiration).
- Video production (can reference community content later).
- Paid hosting or custom domain setup (use GitHub Pages to start).

## Milestones
- [ ] Audit current README/wiki content and identify gaps.
- [ ] Choose documentation tooling, set up scaffolding, and CI deployment.
- [ ] Draft tutorials and ensure they stay in sync with sample projects.
- [ ] Establish maintenance process (versioning, doc review checklist).

## Dependencies
- Codebase comments and XML docs for automated API reference.
- Sample games for tutorial walkthroughs.
- CI integration for docs build and deployment.

## Validation
- Docs site builds successfully in CI and deploys to public URL.
- New contributors can follow onboarding tutorial to ship small feature.
- Documentation issue backlog decreases as coverage improves.

## Open Questions
- Do we want versioned docs aligned with release cadence or rolling latest?
- How do we solicit and triage community doc contributions effectively?
- Should we embed runnable code snippets via web-based playground eventually?
