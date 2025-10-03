#if USE_RAYLIB
using System;
using Asmo.Gfx;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace Asmo.Window.Backend
{
    /// <summary>
    /// Raylib platform window wrapper.
    /// </summary>
    public sealed class RaylibWindow : IPlatformWindow
    {
        private double _lastTime;
        public RaylibWindow(int width, int height, string title)
        {
            Raylib.InitWindow(width, height, title);
            Raylib.SetTargetFPS(0);
            _lastTime = Raylib.GetTime();
        }
        public bool ShouldClose => Raylib.WindowShouldClose();
        public (int Width, int Height) ClientSize => (Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
        public double GetFrameDeltaSeconds()
        {
            double now = Raylib.GetTime();
            double dt = now - _lastTime;
            _lastTime = now;
            return dt;
        }
        public void PollEvents() { /* Raylib polls internally during BeginDrawing; no-op for now */ }
        public void SetTitle(string title) => Raylib.SetWindowTitle(title);
        public void Dispose() => Raylib.CloseWindow();
    }

    /// <summary>
    /// Simple texture blit backend: uploads dirty rects into a Raylib Texture2D and draws scaled.
    /// </summary>
    public sealed class RaylibTextureRenderer : IRenderBackend
    {
        private int _width;
        private int _height;
    private Texture2D _texture;
        private byte[] _staging; // CPU linear RGBA staging (if Surface not span-backed)
        // Raylib VR stereo integration (optional)
        private bool _vrEnabled;
        private VrStereoConfig _vrConfig;
        private bool _vrConfigLoaded;
        private RenderTexture2D _vrTarget; // off-screen per-eye render (raylib handles splitting)
        private bool _vrTargetCreated;
        private Camera3D _vrCamera;
        public bool EnableRaylibVr()
        {
            if (_vrEnabled) return true;
            try
            {
                // Build a generic Oculus-like device profile (values from sample provided by user)
                VrDeviceInfo device = new VrDeviceInfo
                {
                    HResolution = 2160,
                    VResolution = 1200,
                    HScreenSize = 0.133793f,
                    VScreenSize = 0.0669f,
                    EyeToScreenDistance = 0.041f,
                    LensSeparationDistance = 0.07f,
                    InterpupillaryDistance = 0.07f,
                };
                unsafe
                {
                    device.LensDistortionValues[0] = 1.0f;
                    device.LensDistortionValues[1] = 0.22f;
                    device.LensDistortionValues[2] = 0.24f;
                    device.LensDistortionValues[3] = 0.0f;
                    device.ChromaAbCorrection[0] = 0.996f;
                    device.ChromaAbCorrection[1] = -0.004f;
                    device.ChromaAbCorrection[2] = 1.014f;
                    device.ChromaAbCorrection[3] = 0.0f;
                }
                _vrConfig = Raylib.LoadVrStereoConfig(device);
                _vrConfigLoaded = true;
                // Create target sized to current window (using raylib screen dims)
                _vrTarget = Raylib.LoadRenderTexture(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                _vrTargetCreated = true;
                // Simple forward camera pointing at origin; we draw a billboard panel with console texture
                _vrCamera = new Camera3D
                {
                    Position = new System.Numerics.Vector3(0f, 0f, 1.2f),
                    Target = new System.Numerics.Vector3(0f, 0f, 0f),
                    Up = new System.Numerics.Vector3(0f, 1f, 0f),
                    FovY = 90f,
                    Projection = CameraProjection.CAMERA_PERSPECTIVE
                };
                _vrEnabled = true;
                Console.WriteLine("[VR] Raylib VR stereo mode enabled (generic profile).");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[VR] Failed to enable Raylib VR: " + ex.Message);
                return false;
            }
        }
    public bool RaylibVrActive => _vrEnabled;

        public int FrameWidth => _width;
        public int FrameHeight => _height;

        private ulong _fullUploads;
        private ulong _partialUploads;
        public RaylibTextureRenderer(int width, int height)
        {
            _width = width; _height = height;
            _texture = CreateBlankTexture(width, height);
            // Set point/nearest filtering for crisp pixels (enum name per raylib-cs 5.0.0)
            try { Raylib.SetTextureFilter(_texture, TextureFilter.TEXTURE_FILTER_POINT); } catch { /* ignore if enum differs */ }
            _staging = new byte[width * height * 4];
            // Read optional env overrides
            string? scaleMode = Environment.GetEnvironmentVariable("ASMO_SCALE_MODE"); // "integer" or null
            if (!string.IsNullOrEmpty(scaleMode)) _forceIntegerScale = scaleMode.Equals("integer", StringComparison.OrdinalIgnoreCase);
            string? thr = Environment.GetEnvironmentVariable("ASMO_DIRTY_THRESHOLD");
            if (double.TryParse(thr, out var parsed) && parsed > 0 && parsed < 1) _dirtyFullUploadThreshold = parsed;
            _logStatsEvery = int.TryParse(Environment.GetEnvironmentVariable("ASMO_UPLOAD_STATS_INTERVAL"), out var iv) && iv > 0 ? iv : 600;
        }

    // (Legacy placeholder removed) VrActive not used; use RaylibVrActive instead.

        private static Texture2D CreateBlankTexture(int w, int h)
        {
            var img = Raylib.GenImageColor(w, h, Color.BLANK);
            var tex = Raylib.LoadTextureFromImage(img);
            Raylib.UnloadImage(img);
            return tex;
        }

        public Memory<byte>? TryGetFrameMemory() => null; // not exposing direct GPU buffer yet

        public void Resize(int width, int height)
        {
            if (width == _width && height == _height) return;
            Raylib.UnloadTexture(_texture);
            _width = width; _height = height;
            _texture = CreateBlankTexture(width, height);
            if (_staging.Length != width * height * 4)
                _staging = new byte[width * height * 4];
        }

        private bool _forceIntegerScale = true; // default for pixel art
        private double _dirtyFullUploadThreshold = 0.45; // if dirty area > 45% treat as full frame update

        private int _logStatsEvery;
        private int _framesSinceLog;
        private bool _vrRequested;

        public void CommitDirtyRect(int x, int y, int w, int h, Func<(ReadOnlyMemory<byte> data, int strideBytes)> pixelProvider)
        {
            if (w <= 0 || h <= 0) return;
            var (data, stride) = pixelProvider();
            int totalPixels = _width * _height;
            int dirtyPixels = w * h;
            bool treatAsFull = (w == _width && h == _height && stride == _width * 4) || ((double)dirtyPixels / totalPixels) >= _dirtyFullUploadThreshold;
            unsafe
            {
                fixed (byte* srcBase = data.Span)
                {
                    if (treatAsFull)
                    {
                        // Full upload path
                        Raylib.UpdateTexture(_texture, (void*)(IntPtr)srcBase);
                        _fullUploads++;
                    }
                    else
                    {
                        int rowBytes = w * 4;
                        int needed = rowBytes * h;
                        if (_staging.Length < needed) _staging = new byte[needed];
                        fixed (byte* dst = _staging)
                        {
                            for (int row = 0; row < h; row++)
                            {
                                byte* srcRow = srcBase + ((y + row) * stride) + x * 4;
                                Buffer.MemoryCopy(srcRow, dst + row * rowBytes, rowBytes, rowBytes);
                            }
                            var rect = new Rectangle(x, y, w, h);
                            Raylib.UpdateTextureRec(_texture, rect, (void*)(IntPtr)dst);
                            _partialUploads++;
                        }
                    }
                }
            }
            _framesSinceLog++;
            if (_logStatsEvery > 0 && _framesSinceLog >= _logStatsEvery)
            {
                ulong total = _fullUploads + _partialUploads;
                if (total > 0)
                {
                    double pctFull = 100.0 * _fullUploads / total;
                    Console.WriteLine($"[UploadStats] FramesWithUploads={total} Full={_fullUploads} Partial={_partialUploads} Full%={pctFull:F1} Threshold={_dirtyFullUploadThreshold:P0}");
                }
                _framesSinceLog = 0;
            }
        }

        public void Present()
        {
            if (_vrEnabled && _vrConfigLoaded && _vrTargetCreated)
            {
                // Render into VR target with stereo.
                Raylib.BeginTextureMode(_vrTarget);
                Raylib.ClearBackground(Color.BLACK);
                Raylib.BeginVrStereoMode(_vrConfig);
                Raylib.BeginMode3D(_vrCamera);
                // Draw a billboard (flat panel) showing the software console texture directly in front of camera.
                float panelWorldHeight = 1.2f; // size in world units (meters-ish)
                float aspect = (float)_width / _height;
                float panelWorldWidth = panelWorldHeight * aspect;
                try
                {
                    // Prefer billboard rectangle variant if exposed (rec + size scaling); fallback to basic.
                    Raylib.DrawBillboardRec(
                        _vrCamera,
                        _texture,
                        new Rectangle(0, 0, _texture.Width, _texture.Height),
                        new System.Numerics.Vector3(0f, 0f, 0f),
                        new System.Numerics.Vector2(panelWorldWidth, panelWorldHeight),
                        Color.WHITE
                    );
                }
                catch
                {
                    // Fallback: uniform square billboard, may stretch
                    Raylib.DrawBillboard(_vrCamera, _texture, new System.Numerics.Vector3(0f, 0f, 0f), panelWorldHeight, Color.WHITE);
                }
                Raylib.EndMode3D();
                Raylib.EndVrStereoMode();
                Raylib.EndTextureMode();

                // Present the combined stereo render (no extra distortion shader here yet)
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.BLACK);
                Raylib.DrawTextureRec(
                    _vrTarget.Texture,
                    new Rectangle(0, 0, _vrTarget.Texture.Width, -_vrTarget.Texture.Height),
                    new System.Numerics.Vector2(0, 0),
                    Color.WHITE
                );
                Raylib.EndDrawing();
                return;
            }
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.BLACK);
            DrawCoreTexture();
            Raylib.EndDrawing();
        }

        private void DrawCoreTexture()
        {
            // Letterbox scale (optionally integer)
            float windowW = Raylib.GetScreenWidth();
            float windowH = Raylib.GetScreenHeight();
            float srcAspect = (float)_width / _height;
            float winAspect = windowW / windowH;
            float drawW, drawH;
            if (_forceIntegerScale)
            {
                int maxScale = (int)MathF.Min(windowW / _width, windowH / _height);
                if (maxScale < 1) maxScale = 1;
                drawW = _width * maxScale;
                drawH = _height * maxScale;
            }
            else
            {
                if (winAspect > srcAspect)
                {
                    drawH = windowH;
                    drawW = drawH * srcAspect;
                }
                else
                {
                    drawW = windowW;
                    drawH = drawW / srcAspect;
                }
            }
            float dx = (windowW - drawW) * 0.5f;
            float dy = (windowH - drawH) * 0.5f;
            Raylib.DrawTexturePro(
                _texture,
                new Rectangle(0, 0, _width, _height),
                new Rectangle(dx, dy, drawW, drawH),
                System.Numerics.Vector2.Zero,
                0f,
                Color.WHITE
            );
        }

        public void Dispose()
        {
            if (_texture.Id != 0)
                Raylib.UnloadTexture(_texture);
            if (_vrConfigLoaded)
            {
                try { Raylib.UnloadVrStereoConfig(_vrConfig); } catch { }
                _vrConfigLoaded = false;
            }
            if (_vrTargetCreated)
            {
                try { Raylib.UnloadRenderTexture(_vrTarget); } catch { }
                _vrTargetCreated = false;
            }
        }
    }
}
#endif
