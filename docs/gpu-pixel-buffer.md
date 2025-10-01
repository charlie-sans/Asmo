# GPU Pixel Buffer (Level 1) Design

Status: Draft (Planning)  
Target Milestone: Before Render Layer System (to validate perf & inform later abstractions)

## 1. Objective
Introduce an optional GPU-backed pixel buffer path that eliminates the extra CPU→PBO copy by persistently mapping a Pixel Buffer Object (PBO) and letting existing software drawing code write directly into GPU-visible memory.

## 2. Non-Goals (For This Level)
- No rewrite of draw primitives into shaders.
- No multi-surface compositing yet (that belongs to Render Layer System epic).
- No compute shaders or GPU-side rasterization of rectangles/text.
- No dependency on GL 4.5+ only features (must gracefully fallback to current path if `GL_ARB_buffer_storage` absent).

## 3. Success Criteria
| Metric | Target |
|--------|--------|
| Frame copy elimination | 100% (no Marshal.Copy / manual copy into PBO) |
| Additional frame cost | <= +0.1 ms vs baseline |
| Fallback correctness | Identical visual output in legacy path |
| Resize stability | No leaks / crashes after 100 consecutive resizes |

## 4. Capability Detection
At init:
1. Query version & extension string for one of: `GL_VERSION >= 4.4` OR `GL_ARB_buffer_storage`.
2. If unsupported → remain in LegacyMode (existing upload code path).

## 5. Proposed Types / Interfaces
```csharp
public enum PixelBufferBackend { LegacyCpuCopy, PersistentMapped }

public interface IPixelBufferBackend {
    int Width { get; }
    int Height { get; }
    PixelBufferBackend Mode { get; }
    Memory<byte> FrameMemory { get; }      // Full writable RGBA surface (Width*Height*4)
    Span<byte> GetSpan();                  // Convenience accessor: FrameMemory.Span
    void CommitDirty(int x, int y, int w, int h);
    void Resize(int w, int h);
    void Dispose();
}
```
Implementation class: `GpuPixelBuffer` (even if it wraps legacy path for now).

## 6. Memory Layout & Offsets
- Format: RGBA8 tightly packed (4 bytes/pixel).  
- Row stride = `Width * 4`.  
- Byte offset for (x,y) = `(y * Width + x) * 4`.
- Dirty rect upload uses `glPixelStorei(GL_UNPACK_ROW_LENGTH, Width)` and pointer = base + offset.

## 7. GL Resource Lifecycle
| Phase | Action |
|-------|--------|
| Create | `glGenBuffers` → `glBindBuffer(GL_PIXEL_UNPACK_BUFFER)` → `glBufferStorage(size, NULL, MAP_WRITE_BIT|MAP_PERSISTENT_BIT|MAP_COHERENT_BIT)` |
| Map | `glMapBufferRange(... MAP_WRITE_BIT|MAP_PERSISTENT_BIT|MAP_COHERENT_BIT)` once; keep pointer |
| Upload Dirty | Bind PBO + `glTexSubImage2D` with pointer offset |
| Resize | Unmap → Delete buffer → Recreate & remap |
| Destroy | Unmap (if necessary) → Delete buffer |

### Flags Rationale
- `MAP_PERSISTENT_BIT` + `MAP_COHERENT_BIT` let CPU writes be visible without explicit flush.  
- If `COHERENT` unsupported, fallback to manual `glFlushMappedBufferRange` after dirty commits.

## 8. Dirty Region Strategy
Current engine tracks a single or list of dirty rectangles (?) — plan:
1. If multiple small dirty rects ≤ 8 per frame → issue one `glTexSubImage2D` per rect.
2. If count > threshold OR total dirty area > 40% of surface → coalesce to full-frame upload.
3. Provide instrumentation counters: `DirtyRectCount`, `DirtyAreaRatio`.

Coalescing algorithm (simple):
- Maintain running minX, minY, maxX, maxY while enumerating.
- Decide coalesce if area heuristic triggers.

## 9. Threading & Safety
- Writes happen on main thread only (no locking initially).
- Future: allow background producer with double PBO ring — out of scope (documented as Level 2 enhancement).

## 10. Fallback/Compatibility Path
Legacy path renamed internally to `CpuCopyPixelBuffer` implementing same interface:
```csharp
class CpuCopyPixelBuffer : IPixelBufferBackend { /* wraps byte[] + existing upload */ }
```
Window chooses implementation:
```csharp
_pixelBuffer = GpuPixelBuffer.TryCreate(width, height, out var backend) ? backend : new CpuCopyPixelBuffer(width, height);
```

## 11. Integration Points
| Location | Change |
|----------|--------|
| `Window` | Replace manual `_uploadBuffer` + PBO fields with `_pixelBufferBackend` reference |
| `Surface` | Provide a factory that can accept external span; OR keep as-is and write into backend via explicit copy (preferred: adapt `Surface` to wrap span to realize full benefit) |
| Frame Upload | For each dirty rect: `_pixelBufferBackend.CommitDirty(rect...)` |

### Option A (Preferred): `Surface` wraps `Span<byte>` directly
Pros: Zero extra copy.  
Cons: Must ensure `Surface.Pixels` no longer allocates a new array; refactor drawing code to use span safely.

### Option B: Temporary Copy (Transitional)
Pros: Minimal code churn initially.  
Cons: Defeats performance goal (still doing CPU copy) → Not acceptable for Level 1 final.

## 12. API Changes to `Surface`
Add constructor overload:
```csharp
public Surface(int w, int h, Span<byte> backingSpan) { /* no internal allocation */ }
```
Risk: `Span<byte>` cannot be stored as a field directly (must convert to unsafe pointer or use `Memory<byte>` wrapper). Implementation plan:
- Store `unsafe byte* _ptr;` with length.
- Provide indexer & helper methods operating on pointer.
- Offer `Span<byte> GetSpan()` each use via `new Span<byte>(_ptr, length)`.

### Update: Adopt Memory<byte>
We will expose `Memory<byte>` from the backend to allow safer retention (e.g., caching across frames) while still providing fast `Span<byte>` access via `GetSpan()`. Internally the persistent mapped pointer is wrapped in a custom `UnmanagedMemoryManager<byte>` (future) or a lightweight shim that implements `Memory<byte>` without GC pinning.

## 13. Resizing Sequence
1. Flush any pending dirty rect logic.
2. `Surface` disposed (if span-backed) or re-bound.
3. Backend `Resize()` → remap pointer.
4. Recreate `Surface` bound to new span.
5. Mark full frame dirty for next upload.

## 14. Error Handling
| Failure | Response |
|---------|----------|
| `glBufferStorage` returns error | Log & fallback to `CpuCopyPixelBuffer` |
| Mapping fails | Fallback to legacy path |
| Extension missing | Skip attempt (no log spam; single info message) |
| Resize mapping failure | Attempt legacy fallback, keep engine running |

## 15. Instrumentation (Optional Flags)
Add a small struct polled by future overlay:
```csharp
struct PixelBackendStats {
    public int DirtyRects;
    public int CoalescedRects;
    public float DirtyAreaRatio;
    public PixelBufferBackend Mode;
}
```
Expose via `ConsoleHost` or a new `IRenderDiagnostics` service.

## 16. Testing Plan
| Test | Description |
|------|-------------|
| Fallback | Force-disable extensions via config; ensure legacy path works |
| Smoke | Draw moving sprite; confirm no visual artifacts w/ persistent path |
| Resize Stress | Loop 100 resizes, verify no GL errors (`glGetError`) |
| Partial Update | Mark small dirty rects; ensure only that region changes visually |
| Coalesce Trigger | Simulate many rects; ensure single large upload path used |

## 17. Incremental Implementation Phases
| Phase | Deliverable |
|-------|-------------|
| P1 | `GpuPixelBuffer` minimal (full-frame upload only) |
| P2 | Dirty rect offsets & row length logic |
| P3 | Coalescing heuristic + stats struct |
| P4 | Surface span-backed refactor |
| P5 | Cleanup & doc update in `architecture.md` |

## 21. Advanced Option: Compute Shader Backend

### Overview
In addition to the persistent mapped PBO/SSBO approach, a modern and highly efficient path can leverage compute shaders to process and transfer pixel data from a mapped GPU buffer to the final render target.

### Workflow
1. **Map a GPU buffer** (PBO or SSBO) persistently and expose it as a `Span<byte>` (or `Span<T>`) on the CPU side.
2. **Write pixel data** into this span as usual from the CPU.
3. **Dispatch a compute shader** that reads from this buffer and writes to the render target (e.g., a texture or framebuffer attachment).
4. The compute shader can perform format conversion, effects, or compositing as needed.

### Benefits
- Offloads pixel transfer and processing to the GPU, reducing CPU-GPU synchronization.
- Enables complex effects or conversions with minimal CPU cost.
- Scales well for high resolutions and modern hardware.
- Future-proofs the engine for advanced rendering workflows.

### Integration Plan
- This path can be implemented as an optional backend, selected at runtime or build time.
- Recommend targeting this for Level 2 or Level 3 of the GPU pixel buffer roadmap, after the basic persistent mapped PBO backend is stable.

### Open Questions
- What minimum hardware/driver support is required for compute shader path?
- Should the compute shader path be the default on modern hardware, or opt-in?
- How will fallback to legacy/CPU path be handled if compute shaders are unavailable?

---

## 18. Risks & Mitigations
| Risk | Mitigation |
|------|------------|
| Driver quirk with persistent mapping | Provide config toggle `ASMO_FORCE_LEGACY_PIXEL_BUFFER` |
| Accidental unsafe span misuse | Encapsulate pointer logic in dedicated type; limit `unsafe` regions |
| GC pin pressure if using GCHandle alternative | Prefer persistent map, avoid pinning managed arrays |

## 19. Open Questions
- Should we immediately add a second PBO for future ring buffer (even if unused)? → Probably no; keep minimal.
- Support partial pixel formats (RGB565) later for bandwidth? → Defer until profiling shows need.
- Allow MASM direct pointer access now? → Defer until MASM host service is merged (document future hook).

## 20. Acceptance Checklist
- [ ] Builds & runs with legacy path unchanged when feature disabled
- [ ] Feature flag on: persistent path active, no extra copy observed in profiler
- [ ] Resizing works without crash/leak (validated via diagnostics / memory snapshot)
- [ ] Documentation updated (this file + architecture cross-link)

---
**Next Step:** Approve or revise this design. Once accepted, implement Phase P1–P2 in a short PR.
