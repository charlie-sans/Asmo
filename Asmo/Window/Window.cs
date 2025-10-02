
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Runtime.InteropServices;
using System.Text;
using Asmo.Types;
using Asmo.Gfx;
using System.ComponentModel;
using System.IO.Compression;
using Asmo;
using Asmo.Window.HomeScreen;
using System.Diagnostics;

namespace Asmo.Window
{
    public class Window : GameWindow
    {
        public bool UseVr { get; private set; }
    private VrManager? _vr;
        // Central canonical window title (update here if you want to rename globally)

        public Window(GameWindowSettings settings, bool useVr = false)
            : this(settings)
        {
            UseVr = useVr;
            if (UseVr)
            {
                _vr = new VrManager();
                _vr.InitOpenXR_OpenGL(); // best-effort, will fallback if not available
            }
        }
        public const string CanonicalTitle = "ASMO Game Console";
        private bool gameLoaded = false;
        private int _texture;
        private int _shaderProgram;
        private int _vao, _vbo;
    private int _width = GameEnvironment.ScreenWidth, _height = GameEnvironment.ScreenHeight;
    public Surface framebuffer;
    // Persistent buffer for texture uploads
    // Pixel buffer backend abstraction (starts with CPU copy backend)
    private IPixelBufferBackend _pixelBackend = null!; // initialized in OnLoad
    // Optional legacy fields retained until GPU backend implemented
    private byte[] _uploadBuffer = Array.Empty<byte>(); // transitional (will be removed once Surface rewired)
    private int _pbo = 0; // reserved for persistent mapped path later
    // Hybrid renderer support

    public enum RendererType { Software, Hardware }
    public RendererType CurrentRendererType { get; private set; } = RendererType.Hardware;
    private ConsoleHost consoleHost;
    private HomeScreenDisplay display = new HomeScreenDisplay();
    private Asmo.Window.input.Mouse mouse;
    private double _titleRefreshTimer = 0;
    private double _nativePollTimer = 0;
    private string _sentinel = string.Empty;
    private string _baseTitle = CanonicalTitle; // mutable base part (game can change)
    private string _expectedFullTitle = CanonicalTitle; // base + sentinel
    private int _titleCorruptionIncidents = 0;
    private bool _internalTitleUpdate = false; // guard flag for internal Title sets
    private static bool _suppressTitleSets = true; // experiment: do not set title after creation

        /// <summary>
        /// Gets the width of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferX => _width;
        /// <summary>
        /// Gets the height of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferY => _height;

        private static readonly string _titleLogPath = Path.Combine(Path.GetTempPath(), "asmo_title_log.txt");

        public Window(GameWindowSettings settings) : base(
            gameWindowSettings: settings,
            nativeWindowSettings: new NativeWindowSettings
            {
                // Title = CanonicalTitle,
                ClientSize = new Vector2i(GameEnvironment.ScreenWidth * 1, GameEnvironment.ScreenHeight * 1)
            })
        {
            GameEnvironment.WindowTitle = CanonicalTitle;
            framebuffer = new Surface(_width, _height) { Window = this };
            consoleHost = new ConsoleHost();
            mouse = new Asmo.Window.input.Mouse(this);
                Title = CanonicalTitle;
        }
    // Removed title log path (instrumentation disabled)
    // private static readonly string _titleLogPath = Path.Combine(Path.GetTempPath(), "asmo_title_log.txt");

        /// <summary>
        /// Resize the framebuffer and all related resources at runtime.
        /// </summary>
        public void ResizeFrameBuffer(int width, int height)
        {
            if (width == _width && height == _height) return;
            _width = width;
            _height = height;
            // Recreate framebuffer respecting backend type
            if (_pixelBackend is GpuPixelBuffer gpu)
            {
                gpu.Resize(_width, _height); // will remap buffer
                framebuffer = new Surface(_width, _height, gpu.FrameMemory) { Window = this };
            }
            else
            {
                framebuffer = new Surface(_width, _height) { Window = this };
            }
            // Resize backend (handles texture reallocation internally if GPU path; for CPU we handle here)
            _pixelBackend.Resize(_width, _height);
            // Transitional legacy buffers (still used by Render path until refactor complete)
            _uploadBuffer = new byte[_width * _height * 4];
            // Optionally update window size or viewport
            int scale = 2;
            Size = new Vector2i(_width * scale, _height * scale);
            UpdateViewport();
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            GL.DeleteTexture(_texture);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            Environment.Exit(0); // Force exit to stop audio threads
        }
        /// <summary>
        /// Public method to load a game into the window for standalone/debug launching.
        /// </summary>
        public void LoadGame(IConsoleGame game)
        {
            framebuffer.Window = this;
            consoleHost.LoadGame(game, framebuffer);
            gameLoaded = true;
            try { GameHistory.AddOrUpdate(game.GetType().Assembly.Location, game.GetType().Name); } catch { }
        }
        protected override void OnLoad()
        {
            OpenTK.Graphics.OpenGL.GL.LoadBindings(new OpenTK.Windowing.GraphicsLibraryFramework.GLFWBindingsContext());
            base.OnLoad();
            // Setup OpenGL state
            GL.ClearColor(0f, 0f, 0f, 1f);

            // Ensure debug overlay exists early
            if (Asmo.DebugOverlay.Current == null)
            {
                _ = new Asmo.DebugOverlay();
                // Asmo.DebugOverlay.Current?.Show();
                System.Diagnostics.Debug.WriteLine("[DebugOverlay] Created in Window.OnLoad");
            }

            // Create texture
            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            // Setup quad
            float[] vertices = {
                // positions   // texcoords
                -1f, -1f,     0f, 0f,
                 1f, -1f,     1f, 0f,
                 1f,  1f,     1f, 1f,
                -1f,  1f,     0f, 1f
            };
            uint[] indices = { 0, 1, 2, 2, 3, 0 };
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            int ebo = GL.GenBuffer();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.BindVertexArray(0);

            // Load shaders
            _shaderProgram = CreateShaderProgram(Shaders.DefaultVertexShaderSource, Shaders.DefaultFragmentShaderSource);

            // Initialize framebuffer and console host
            // Framebuffer will be replaced below if GPU backend selected
            framebuffer = new Surface(_width, _height) { Window = this };
            consoleHost = new ConsoleHost();
            // Allocate persistent upload buffer
            _uploadBuffer = new byte[_width * _height * 4];
            // Create a PBO for async uploads
            _pbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, _width * _height * 4, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            // Try to use persistent mapped GPU backend; fallback to CPU if not supported
            try
            {
                // Check for GL_ARB_buffer_storage or OpenGL >= 4.4
                string versionStr = GL.GetString(StringName.Version) ?? "";
                string extensions = GL.GetString(StringName.Extensions) ?? "";
                bool hasBufferStorage = extensions.Contains("GL_ARB_buffer_storage") || versionStr.StartsWith("4.4") || versionStr.StartsWith("4.5") || versionStr.StartsWith("4.6") || versionStr.StartsWith("4.7") || versionStr.StartsWith("4.8") || versionStr.StartsWith("4.9") || versionStr.StartsWith("5.");
                if (hasBufferStorage)
                {
                    _pixelBackend = new GpuPixelBuffer(_width, _height, _texture);
                    framebuffer = new Surface(_width, _height, _pixelBackend.FrameMemory) { Window = this };
                    System.Diagnostics.Debug.WriteLine("[PixelBuffer] Using persistent mapped GPU backend (span-backed framebuffer).");
                }
                else
                {
                    _pixelBackend = new CpuCopyPixelBuffer(_width, _height, _texture);
                    framebuffer = new Surface(_width, _height) { Window = this };
                    System.Diagnostics.Debug.WriteLine("[PixelBuffer] Using legacy CPU copy backend (no buffer_storage support).");
                }
            }
            catch (Exception ex)
            {
                _pixelBackend = new CpuCopyPixelBuffer(_width, _height, _texture);
                framebuffer = new Surface(_width, _height) { Window = this };
                System.Diagnostics.Debug.WriteLine($"[PixelBuffer] GPU backend unavailable, falling back to CPU: {ex.Message}");
            }
           
            // Set the window size to a multiple of framebuffer (e.g., 2x)
            int scale = 2;
            Size = new Vector2i(_width * scale, _height * scale);
            // Set initial viewport (will be updated in OnResize)
            UpdateViewport();

            // Load recent game history
            try { GameHistory.Load(); } catch { }

    }

        public void Render(Surface surface, int x, int y)
        {
            var pixels = surface.Pixels;
            int pw = surface.Width;
            int ph = surface.Height;
            if (pixels == null || pw == 0 || ph == 0)
                return;
            // Ensure texture is large enough
            if (_width < x + pw || _height < y + ph)
            {
                _width = Math.Max(_width, x + pw);
                _height = Math.Max(_height, y + ph);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                _pixelBackend.Resize(_width, _height);
                _uploadBuffer = new byte[_width * _height * 4]; // transitional
            }

            // Use dirty rectangle to minimize updates
            if (surface.IsDirty)
            {
                var (dirtyX, dirtyY, dirtyW, dirtyH) = surface.GetDirtyRect();
                if (dirtyW > 0 && dirtyH > 0)
                {
                    if (_pixelBackend is GpuPixelBuffer gpu)
                    {
                        if (!surface.IsSpanBacked)
                        {
                            // Transitional path: copy only dirty rect into mapped buffer
                            var span = gpu.GetSpan();
                            int width = surface.Width;
                            int height = surface.Height;
                            for (int py = 0; py < dirtyH; py++)
                            {
                                int srcY = dirtyY + py;
                                int flippedY = height - 1 - srcY;
                                if (flippedY < 0 || flippedY >= height) continue;
                                for (int px = 0; px < dirtyW; px++)
                                {
                                    int srcX = dirtyX + px;
                                    if (srcX < 0 || srcX >= width) continue;
                                    var c = pixels[srcX][flippedY];
                                    int i = (srcY * width + srcX) * 4; // write at absolute position; flip handled earlier
                                    span[i + 0] = (byte)c.R;
                                    span[i + 1] = (byte)c.G;
                                    span[i + 2] = (byte)c.B;
                                    span[i + 3] = (byte)c.A;
                                }
                            }
                        }
                        // Commit only dirty rectangle
                        gpu.CommitDirty(dirtyX, dirtyY, dirtyW, dirtyH);
                    }
                    else if (_pixelBackend is CpuCopyPixelBuffer cpu)
                    {
                        cpu.BindSource(pixels);
                        cpu.CommitDirty(dirtyX, dirtyY, dirtyW, dirtyH);
                    }
                }
                surface.ClearDirty();
            }
        }

        protected override void OnRenderFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            // base.OnRenderFrame(args);
    
            // Render overlay (after scene/UI updates done in UpdateFrame) onto framebuffer before upload
            Asmo.DebugOverlay.Current?.Render(framebuffer);
            // Upload latest framebuffer (UI + overlay) to GPU texture
            Render(framebuffer, 0, 0);

            if (UseVr && _vr != null && _vr.Initialized)
            {
                _vr.BeginFrame();
                // Submit the framebuffer texture as a quad layer (placeholder)
                _vr.SubmitQuadLayer(_texture, _width, _height);
                _vr.EndFrame();
            }
            else
            {
                GL.Clear(ClearBufferMask.ColorBufferBit);
                UpdateViewport();
                GL.UseProgram(_shaderProgram);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.BindVertexArray(_vao);
                GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
                SwapBuffers();
            }

        }

        protected override void OnUpdateFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            // base.OnUpdateFrame(args);
            if (!IsFocused)
            {
                // Skip updates when not focused to save CPU/GPU
                System.Threading.Thread.Sleep(100);
                return;
            }

                
            Asmo.DebugOverlay.Current?.BeginFrame();
            if (!gameLoaded)
                framebuffer.Clear(new Color(0, 0, 32, 255)); // dark blue background
            if (!gameLoaded)
            {
                // Always pass the Mouse instance to HomeScreenDisplay
                display.RenderHomeScreen(args, framebuffer, mouse);
            }
            else
            {
                consoleHost.Update(args.Time);
                consoleHost.Draw(framebuffer);
            }
            Asmo.DebugOverlay.Current?.EndFrame();
            // Collect surface stats if span-backed
            if (Asmo.DebugOverlay.Current != null && framebuffer.IsSpanBacked)
            {
                var (dx, dy, dw, dh) = framebuffer.GetDirtyRect();
                Asmo.DebugOverlay.Current.UpdatePixelStats(dw * dh, _width * _height);
            }
            // Periodically flush history
            GameHistory.Tick();
            // Force overlay visible every frame (workaround for accidental hiding)
            // Asmo.DebugOverlay.Current?.Show();
        }

        /// <summary>
        /// Expose underlying frame memory if GPU backend (span-backed). Returns null otherwise.
        /// </summary>
        public Memory<byte>? TryGetFrameMemory()
        {
            if (framebuffer.IsSpanBacked && _pixelBackend != null)
                return _pixelBackend.FrameMemory;
            return null;
        }

        protected override void OnResize(OpenTK.Windowing.Common.ResizeEventArgs e)
        {
            base.OnResize(e);
            UpdateViewport();

        }

        /// <summary>
        /// Updates the OpenGL viewport to center the framebuffer in the window with black bars.
        /// </summary>
        private void UpdateViewport()
        {
            float windowAspect = (float)Size.X / Size.Y;
            float fbAspect = (float)_width / _height;
            int vpWidth, vpHeight, vpX, vpY;

            if (windowAspect > fbAspect)
            {
                // Window is wider than framebuffer
                vpHeight = Size.Y;
                vpWidth = (int)(vpHeight * fbAspect);
                vpX = (Size.X - vpWidth) / 2;
                vpY = 0;
            }
            else
            {
                // Window is taller than framebuffer
                vpWidth = Size.X;
                vpHeight = (int)(vpWidth / fbAspect);
                vpX = 0;
                vpY = (Size.Y - vpHeight) / 2;
            }
            GL.Viewport(vpX, vpY, vpWidth, vpHeight);
        }

        protected override void OnFileDrop(OpenTK.Windowing.Common.FileDropEventArgs e)
        {
            foreach (var file in e.FileNames)
            {
                string? rootDir = null;
                if (Directory.Exists(file))
                {
                    // Dropped folder
                    rootDir = file;
                }
                else if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    // Dropped zip file
                    string tempDir = Path.Combine(Path.GetTempPath(), "AsmoGameZip_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempDir);
                    ZipFile.ExtractToDirectory(file, tempDir);
                    rootDir = tempDir;
                }
                else if (file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    // Dropped DLL directly (legacy behavior)
                    GameEnvironment.AssetRoot = Path.GetDirectoryName(file);
                    var asm = System.Reflection.Assembly.LoadFrom(file);
                    var gameType = asm.GetTypes().FirstOrDefault(t =>
                        typeof(IConsoleGame).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                    if (gameType != null)
                    {
                        var game = (IConsoleGame)Activator.CreateInstance(gameType)!;
                        framebuffer.Window = this;
                        consoleHost.LoadGame(game, framebuffer);
                        gameLoaded = true;
                        try { GameHistory.AddOrUpdate(file, gameType.Name); } catch { }
                    }
                    continue;
                }
                else
                {
                    // Not a supported file type
                    continue;
                }

                // If we have a rootDir (folder or extracted zip), search for DLLs
                if (rootDir != null)
                {
                    var dlls = Directory.GetFiles(rootDir, "*.dll", SearchOption.AllDirectories);
                    foreach (var dll in dlls)
                    {
                        try
                        {
                            var asm = System.Reflection.Assembly.LoadFrom(dll);
                            var gameType = asm.GetTypes().FirstOrDefault(t =>
                                typeof(IConsoleGame).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
                            if (gameType != null)
                            {
                                GameEnvironment.AssetRoot = rootDir;
                                var game = (IConsoleGame)Activator.CreateInstance(gameType)!;
                                framebuffer.Window = this;
                                consoleHost.LoadGame(game, framebuffer);
                                gameLoaded = true;
                                    try { GameHistory.AddOrUpdate(dll, gameType.Name); } catch { }
                                break;
                            }
                        }
                        catch { /* Ignore bad DLLs */ }
                    }
                }
            }
            GameEnvironment.WindowX = FrameBufferY;
            GameEnvironment.WindowY = FrameBufferY;
            GameEnvironment.WindowTitle = Title;
        }

        private int CreateShaderProgram(string vertPath, string fragPath)
        {
            string vertSource = vertPath;
            string fragSource = fragPath;
            int vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertShader, vertSource);
            GL.CompileShader(vertShader);
            int fragShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragShader, fragSource);
            GL.CompileShader(fragShader);
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertShader);
            GL.AttachShader(program, fragShader);
            GL.LinkProgram(program);
            GL.DeleteShader(vertShader);
            GL.DeleteShader(fragShader);
            return program;
        }

                // Title instrumentation removed: SetWindowTitle no-op retained for API compatibility
                internal void SetWindowTitle(string v)
                {
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        _baseTitle = v;
                        Title = v;
                        GameEnvironment.WindowTitle = v;
                    }
                }

        internal string GetWindowName()
        {
            return Title;
        }

        // Simple heuristic to detect unexpected non-ASCII characters (e.g., accidental corruption / encoding issue)
        private void ValidateTitleForUnexpectedGlyphs() { }

        private void LogTitleEvent(string msg) { }

    }
}
