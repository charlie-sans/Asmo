# MASM Integration: Getting Started (Preview)

> Status: Experimental – integration layer is being implemented. This doc outlines intended usage so early contributors can align.

## Overview
The MASM (micro-assembly) module lets you write low-level routines that run alongside C# game logic. Typical uses:
- Procedural visual effects directly manipulating a pixel surface
- Specialized mini interpreters (bytecode-driven UI, scripted cutscenes)
- Performance-critical inner loops you want to experiment with

## Conceptual Model
```
+------------------------+
|  C# Game / Scene       |
+-----------+------------+
            | calls service API
            v
    +---------------+          +-------------------+
    | MasmHost      | <----->  | MASM VM Runtime   |
    +---------------+   mem    +-------------------+
            |                          |
            | blit / events            | script opcodes manipulate buffers
            v                          v
      Shared Surfaces / Buffers   (Registers, Scratch RAM)
```

## Planned Components
| Component | Role |
|-----------|------|
| `MasmHostService` | Loads, caches, steps MASM programs; exposes host calls |
| `MappedMemory` (existing) | Shared memory layer between VM and host |
| `IMemoryManager` | Allocation strategy for script-visible regions |
| `MasmScript` | Metadata bundle (entry point, symbol table, flags) |

## Lifecycle
1. Load script bytes (from `Assets/MASM/*.masm`) or compiled `.mni` form.
2. Register it via `MasmHostService.Load(scriptId, bytes)`.
3. Each frame: `MasmHostService.Step(delta)` executes a fixed or configurable instruction budget.
4. Scripts write into a surface or structured memory; host reads results (e.g., effect pattern) and composites.

## Example (Pseudo / Target API)
```csharp
var masm = services.Get<MasmHostService>();
masm.Load("plasma", File.ReadAllBytes("Assets/MASM/plasma.mni"));
masm.BindSurface("plasma", sharedSurface); // share a Surface or raw pixel span

// In update loop
masm.StepAll(deltaTime);
// After stepping, the sharedSurface now has new pixels to blit
```

## Host <-> Script Interactions
| Direction | Mechanism | Example |
|-----------|-----------|---------|
| Script → Host | Syscall opcode | Request play sound, set palette, trigger scene change |
| Host → Script | Memory mapping / register poke | Inject time value, input state bits, random seed |

## Determinism
If host injects the same input + time progression, script output should be deterministic. Avoid non-deterministic syscalls (e.g., direct wall-clock access) inside the VM; provide pseudo-random via seeded state.

## Safety / Stability
Initial phase assumes trusted scripts. Later hardening steps may include:
- Instruction quota per frame
- Memory region bounds checks (already largely present)
- Optional sandbox flag disabling certain syscalls

## Debugging (Future Overlay Hooks)
Planned overlay panels:
- Register view
- Last N syscalls log
- Instruction throughput (instr/frame)

## Open Questions (To Finalize Before API Freeze)
- Surface sharing granularity: whole-surface vs region handles
- Standard syscall set and numeric assignments
- Hot reload semantics (replace running script safely)

## Immediate Next Steps
- [ ] Finalize `MasmHostService` interface sketch
- [ ] Integrate service into runtime init
- [ ] Provide one demo (`Demos/MasmPlasma`) exercising a pixel effect
- [ ] Add minimal validation tests (load → step → memory write)

Contributions welcome—open an issue with “MASM” in the title to discuss design details.
