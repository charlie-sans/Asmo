# Shader Library & Hot Reload

## Goal
Ship a curated collection of reusable shaders (CRT, bloom, palette swap, post-process effects) and provide a workflow for rapid iteration and hot-reloading.

## Motivation
- Help developers achieve distinctive visual styles without deep shader knowledge.
- Encourage experimentation via quick feedback loops.
- Establish conventions for shader authoring and integration in Asmo.

## In Scope
- Shader packaging format (GLSL/HLSL variants depending on backend).
- Runtime shader manager with hot-reload trigger (file watch or debug command).
- Sample gallery demonstrating each effect with toggles and parameter controls.
- Documentation on writing custom shaders within the engine's pipeline.

## Out of Scope (initial phase)
- Cross-compiling between graphics APIs (stick with current renderer backend).
- Node-based shader editor UI.
- Advanced post-processing chain management (multiple passes) beyond simple stack.

## Milestones
- [ ] Audit current shader usage (e.g., `Window/Shaders`) and define extension points.
- [ ] Implement shader manager with reload hooks and error reporting.
- [ ] Add curated shader set with parameterized presets.
- [ ] Document workflow and integrate sample toggle menu in demo game.

## Dependencies
- Rendering backend specifics (OpenGL? ensure compatibility).
- File watching utilities for hot-reload.
- GUI components for parameter tweaking.

## Validation
- Hot-reload switches shaders without crashing or leaking resources.
- Sample effects demonstrate noticeable visual changes and adjustable parameters.
- Shader errors surface with helpful diagnostics (line numbers, suggestions).

## Open Questions
- Do we need platform abstraction to support both desktop and potential future web targets?
- Should shader parameters be scriptable via JSON for quick iteration?
- Is there demand for lighting pipeline beyond post-process effects?
