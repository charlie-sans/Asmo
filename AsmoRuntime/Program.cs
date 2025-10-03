using System;
using System.Diagnostics;
using Asmo.Gfx;
using Asmo.Window;
using Asmo.Window.Backend;
namespace Asmo;

public class Startup
{
    public static void Main(string[] args)
    {
        // we really should be doing more here... sad to see this as dead as it is.
        // but for now, this is just a stub to launch the window.

        bool wantVr = false;
        foreach (var a in args)
        {
            if (a.Equals("--vr", StringComparison.OrdinalIgnoreCase) || a.Equals("-vr", StringComparison.OrdinalIgnoreCase))
            {
                wantVr = true; break;
            }
        }

        // Raylib backend (primary path). VR flag currently ignored.
        int w = GameEnvironment.ScreenWidth;
        int h = GameEnvironment.ScreenHeight;
        using IPlatformWindow window = new RaylibWindow(w * 2, h * 2, "Asmo Runtime");
        using IRenderBackend renderer = new RaylibTextureRenderer(w, h);
        VrManager? vr = null;
        if (wantVr)
        {
            // Try Raylib stereo first.
            if (renderer is RaylibTextureRenderer rt && rt.EnableRaylibVr())
            {
                Console.WriteLine("[Runtime] Raylib VR stereo enabled.");
            }
            else
            {
                Console.WriteLine("[Runtime] Raylib VR stereo failed, attempting OpenXR path...");
                var localVr = new VrManager();
                if (!localVr.InitOpenXR_OpenGL())
                {
                    Console.WriteLine("[Runtime] OpenXR VR init failed — falling back to desktop mode.");
                }
                else
                {
                    Console.WriteLine("[Runtime] OpenXR VR mode enabled.");
                    vr = localVr;
                }
            }
        }
        var consoleHost = new ConsoleHost();
        var home = new Asmo.Window.HomeScreen.HomeScreenDisplay();
        var mouse = new Asmo.Window.input.RaylibMouse(() => (renderer.FrameWidth, renderer.FrameHeight));
        // Use a span-backed surface to avoid per-frame full flatten copies.
        byte[] frameBuffer = new byte[w * h * 4];
        var surface = new Surface(w, h, frameBuffer); // span-backed surface
        byte[] linearCache = new byte[w * h * 4];
        // Optional profiling frame limit (set ASMO_PROFILE_FRAMES env var)
        int profileFrameLimit = 0; int.TryParse(Environment.GetEnvironmentVariable("ASMO_PROFILE_FRAMES"), out profileFrameLimit);
        bool profiling = profileFrameLimit > 0;
        long accUpdateTicks = 0, accFlattenTicks = 0, accUploadTicks = 0, accPresentTicks = 0; int accFrames = 0;
        Stopwatch sw = Stopwatch.StartNew();

        while (!window.ShouldClose)
        {
            vr?.BeginFrame(); // Only for OpenXR path currently
            long frameStart = Stopwatch.GetTimestamp();
            window.PollEvents();
            double dt = window.GetFrameDeltaSeconds();
            mouse.Update();
            long afterUpdate = Stopwatch.GetTimestamp();

            // Raylib drag & drop handling (poll style)
            var droppedFiles = Raylib_cs.Raylib.GetDroppedFiles();
            if (droppedFiles != null && droppedFiles.Length > 0)
            {
                foreach (var file in droppedFiles)
                {
                    TryLoadGameFromPath(consoleHost, surface, file);
                    if (consoleHost.Game != null) break;
                }
            }

            if (consoleHost.Game != null)
            {
                consoleHost.Update(dt);
                consoleHost.Draw(surface);
            }
            else
            {
                home.RenderHomeScreenRaylib(dt, surface, mouse.X, mouse.Y, mouse.IsButtonDown(Raylib_cs.MouseButton.MOUSE_BUTTON_LEFT));
            }

            var (x, y, dw, dh) = surface.GetDirtyRect();
            long beforeFlatten = Stopwatch.GetTimestamp();
            // No flatten needed for span-backed surface; we directly provide memory.
            long afterFlatten = Stopwatch.GetTimestamp();
            if (dw > 0 && dh > 0 && surface.FrameMemory.HasValue)
            {
                var mem = surface.FrameMemory.Value;
                renderer.CommitDirtyRect(x, y, dw, dh, () => (mem, surface.Width * 4));
                surface.ClearDirty();
            }
            long afterUpload = Stopwatch.GetTimestamp();
            renderer.Present();

            // OpenXR submission placeholder (Raylib VR path handled inside renderer.Present())
            vr?.EndFrame();
            long afterPresent = Stopwatch.GetTimestamp();

            if (profiling)
            {
                accUpdateTicks += (afterUpdate - frameStart);
                accFlattenTicks += (afterFlatten - beforeFlatten);
                accUploadTicks += (afterUpload - afterFlatten);
                accPresentTicks += (afterPresent - afterUpload);
                accFrames++;
                if (accFrames % 120 == 0)
                {
                    double toMs(long t) => t * 1000.0 / Stopwatch.Frequency;
                    double frameMs = toMs(accUpdateTicks + accFlattenTicks + accUploadTicks + accPresentTicks) / accFrames;
                    Console.WriteLine($"[Profiler] Frames: {accFrames} | Avg Frame {frameMs:F2} ms (Update {toMs(accUpdateTicks) / accFrames:F2} ms, Flatten {toMs(accFlattenTicks) / accFrames:F2} ms, Upload {toMs(accUploadTicks) / accFrames:F2} ms, Present {toMs(accPresentTicks) / accFrames:F2} ms)");
                }
                if (profileFrameLimit > 0 && accFrames >= profileFrameLimit)
                {
                    Console.WriteLine("[Profiler] Frame limit reached, exiting (set by ASMO_PROFILE_FRAMES)");
                    break;
                }
            }
    }

    vr?.Dispose();

        static void TryLoadGameFromPath(ConsoleHost host, Surface surface, string path)
        {
            try
            {
                if (System.IO.Directory.Exists(path))
                {
                    // Search for first dll containing a game
                    foreach (var dll in System.IO.Directory.GetFiles(path, "*.dll", System.IO.SearchOption.AllDirectories))
                    {
                        if (TryLoadGameFromAssembly(host, surface, dll, path)) return;
                    }
                }
                else if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AsmoGameZip_" + Guid.NewGuid().ToString("N"));
                    System.IO.Directory.CreateDirectory(tempDir);
                    System.IO.Compression.ZipFile.ExtractToDirectory(path, tempDir);
                    foreach (var dll in System.IO.Directory.GetFiles(tempDir, "*.dll", System.IO.SearchOption.AllDirectories))
                    {
                        if (TryLoadGameFromAssembly(host, surface, dll, tempDir)) return;
                    }
                }
                else if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    TryLoadGameFromAssembly(host, surface, path, System.IO.Path.GetDirectoryName(path)!);
                }
            }
            catch { /* swallow individual load errors */ }
        }

        static bool TryLoadGameFromAssembly(ConsoleHost host, Surface surface, string dllPath, string assetRoot)
        {
            try
            {
                var asm = System.Reflection.Assembly.LoadFrom(dllPath);
                var gameType = asm.GetTypes().FirstOrDefault(t => typeof(IConsoleGame).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                if (gameType != null)
                {
                    GameEnvironment.AssetRoot = assetRoot;
                    var game = (IConsoleGame)Activator.CreateInstance(gameType)!;
                    host.LoadGame(game, surface);
                    // TODO: integrate with GameHistory (currently not accessible here)
                    return true;
                }
            }
            catch { }
            return false;
        }

    }
}