# Gamepad Input Module

## Goal
Extend Asmo's input system with robust gamepad/controller support targeting XInput devices first, while leaving room for future backends.

## Motivation
- Broaden hardware compatibility for couch-friendly games.
- Offer unified API with keyboard/mouse for easy input rebinding.
- Support multiple controllers for local multiplayer scenarios.

## In Scope
- Low-level XInput integration with polling and vibration feedback.
- Abstraction layer that normalizes buttons, triggers, and sticks into engine events.
- Input mapping utility to bind logical actions to physical controls.
- Sample overlay visualizing controller state for debugging.

## Out of Scope (initial pass)
- PlayStation/Switch controller specific features (touchpad, gyro).
- Steam Input or SDL controller database integration.
- Runtime UI for remapping controls (documented manual approach only).

## Milestones
- [ ] Design input abstraction changes and backward compatibility strategy.
- [ ] Implement XInput wrapper with error handling and hot-plug support.
- [ ] Add action mapping layer and port sample game controls to new system.
- [ ] Document usage, including guidance for local multiplayer setups.

## Dependencies
- Existing input event loop in `Window` and `Keyboard` modules.
- Platform interop capabilities (.NET P/Invoke for native libs).

## Validation
- Sample game playable end-to-end with controller only.
- Multiple controllers recognized simultaneously without conflicts.
- Controller disconnect/reconnect handled gracefully.

## Open Questions
- Should we expose raw axis data or quantize to digital inputs by default?
- How do we surface deadzone configuration and per-controller profiles?
- Do we want to emit vibration events through a dedicated feedback API?
