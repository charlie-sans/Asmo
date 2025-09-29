# Immediate-Mode GUI Enhancements

## Goal
Expand Asmo's immediate-mode GUI toolkit with richer widgets, styling, and layout utilities that keep the API approachable while unlocking more sophisticated interfaces.

## Motivation
- Reduce friction when building in-game menus, HUDs, and tooling panels.
- Support theming and responsive layouts for different resolutions.
- Provide accessibility improvements (keyboard navigation, color contrast options).

## In Scope
- New widgets: sliders, dropdowns, toggle switches, text input, progress bars.
- Theming system with palette/typography presets and runtime switching.
- Layout helpers (flex/grid-like utilities) on top of existing `ZoneLayout`.
- Keyboard/controller navigation for widgets to complement mouse input.

## Out of Scope
- Visual GUI designer/editor application.
- Complex text layout (rich text, RTL) beyond improved basic text input.
- Localization system (covered separately in documentation plan).

## Milestones
- [ ] Audit current GUI capabilities and gather pain points from sample games.
- [ ] Design widget API extensions ensuring backwards compatibility.
- [ ] Implement widget set with automated UI tests where practical.
- [ ] Update docs and demo showcasing themed menus and accessibility options.

## Dependencies
- Rendering primitives (`Surface`, `Font`, `Colors`).
- Input systems (keyboard, mouse, gamepad once available).

## Validation
- Demo menu supports full navigation via keyboard/controller with visual focus states.
- Theming allows swapping palettes at runtime with cached assets.
- Widgets handle edge cases (long text, disabled states) gracefully.

## Open Questions
- Should themes be data-driven (JSON) or code-defined initially?
- How do we approach text input IME support for non-Latin scripts later?
- Do we need animation hooks for transitions (e.g., easing when opening panels)?
