# Micro-Assembly (AsmoMASM) Integration Plan

## Goal
Embed the AsmoMASM micro-assembly environment into the Asmo framework so games can execute scripted routines (rendering, IO, gameplay logic) authored in MASM alongside C# components.

## Motivation
- Empower designers/modders to extend games by editing MASM scripts without rebuilding the engine.
- Reuse the existing SharpMASM toolchain for rapid iteration and hot-reloading of low-level effects.
- Demonstrate hybrid workflows where MASM routines drive rendering, audio triggers, or AI behaviors.

## In Scope
- Runtime host API for loading, executing, and managing MASM scripts from within `Asmo`.
- Memory bridge between MASM virtual machine and engine data (framebuffer, input states, audio triggers).
- Debugging and logging hooks so MASM scripts can report state or errors to the engine overlay.
- Example scenes/demos showcasing MASM-driven rendering and control loops.

## Out of Scope (initially)
- Full MASM editor integration or GUI tooling inside Asmo.
- Cross-platform MASM runtime beyond the existing Windows-focused SharpMASM implementation.
- Sandboxing for untrusted scripts (document as future requirement if external mods are planned).

## Milestones
- [ ] Audit AsmoMASM runtime capabilities: execution model, IO hooks, performance characteristics.
- [ ] Design integration architecture (host services, memory mapping, event hooks) and update scene plans accordingly.
- [ ] Implement MASM host service in `Asmo` with lifecycle methods (load, reset, step, shutdown).
- [ ] Wire MASM rendering commands into the scene manager via a dedicated render layer.
- [ ] Build demo scene showing MASM script drawing primitives and reacting to input.
- [ ] Document workflow (building scripts, hot-reload, troubleshooting) in main docs.

## Dependencies
- `SharpMASM` runtime components (execution engine, memory manager).
- Scene/render layer systems for embedding MASM output.
- Planned debug overlay for introspecting MASM state, registers, and logs.

## Validation
- MASM script can render to a surface at 60 FPS without starving the C# game loop.
- Input events from Asmo (keyboard/gamepad) can be consumed by MASM routines through shared state.
- Errors in MASM scripts surface clearly to the developer (overlay, console logs) without crashing the host.

## Open Questions
- **Do we allow MASM scripts to allocate their own surfaces or operate strictly on shared buffers?**
	- ✅ Resolved: Scripts may allocate their own surfaces within MASM/host memory limits so teams can build full MASM-driven games without relying solely on C# buffers.

- **What security considerations are needed before enabling user-supplied MASM scripts?**
	- ✅ Resolved: External distribution of arbitrary MASM content isn’t in scope—only trusted developers or open-source projects will ship MASM code for now. Sandbox requirements can be revisited if that assumption changes.

- **Should MASM execution be deterministic for replay/rollback features, and if so how do we enforce it?**
	- ✅ Resolved: SharpMASM is deterministic given identical inputs; host integration should preserve deterministic input delivery to maintain that guarantee.

