# VR Integration (OpenXR Scaffold)

Status: Early scaffold. Desktop fallback remains the default.

## Build Flags
- `USE_OPENXR`: Compiles the OpenXR scaffolding (`VrManager`).
- `OPENXR_GL_IMPLEMENTED`: (Not yet enabled) When defined, attempts real OpenGL session creation.

## Runtime Flag
Run with `--vr` to attempt VR init. If anything fails, it silently falls back.

## Next Implementation Milestones
1. Enumerate and enable required extensions (XR_KHR_opengl_enable).
2. Create OpenGL graphics binding (Win32 HDC + HGLRC) and XR session.
3. Enumerate view config, create per-eye swapchains.
4. Implement frame loop (xrWaitFrame / xrBeginFrame / xrEndFrame).
5. Blit software framebuffer texture into each swapchain image.

## Fallback
Any failure leaves `VrManager.Initialized = false`; the normal window path draws as usual.

## openxr_loader.dll Not Found
If you see:
```
Could not load openxr_loader.dll
```
You need an OpenXR runtime / loader:

1. Install one of:
	- SteamVR (sets itself as OpenXR runtime in its settings)
	- OpenXR Tools for Windows Mixed Reality (Microsoft Store) and press “Set as active runtime”
	- Oculus software (enable OpenXR in settings)
2. Re-run with `--vr`.
3. If still missing, manually place the correct architecture `openxr_loader.dll` next to `AsmoRuntime.exe` or add it via project:
	```xml
	<ItemGroup>
	  <None Include="Native\\openxr_loader.dll" CopyToOutputDirectory="PreserveNewest" />
	</ItemGroup>
	```
4. Ensure the DLL matches process bitness (x64).

The code now catches loader errors and logs a message instead of crashing.

## Debugging Tips
- Ensure an OpenXR runtime (SteamVR / WMR) is active.
- If `xrCreateInstance` fails, check enabled extension names.
- Validate your OpenGL context is current before session creation.

## Enabling Real Session (Future)
Uncomment / implement `CreateSession_OpenGL()` and define `OPENXR_GL_IMPLEMENTED` once binding code is stable.

---
This file is a living checklist—update as features land.