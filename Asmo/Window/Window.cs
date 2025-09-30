
using System;
using System.IO;
using System.Linq;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Asmo.Types;
using Asmo.Gfx;
using System.ComponentModel;
using System.IO.Compression;
using Asmo;
using Asmo.Window.HomeScreen;

namespace Asmo.Window
{
    public class Window : GameWindow
    {
        private bool gameLoaded = false;
        private int _texture;
        private int _shaderProgram;
        private int _vao, _vbo;
        private int _width = GameEnvironment.ScreenWidth, _height = GameEnvironment.ScreenHeight;
    public Surface framebuffer;
    // Persistent buffer for texture uploads
    private byte[] _uploadBuffer;
    // Optional: PBO for async texture uploads (advanced)
    private int _pbo = 0;
    // Hybrid renderer support
    public Asmo.Gfx.IRenderer _renderer;
    private Asmo.Gfx.SoftwareRenderer _softwareRenderer;
    private Asmo.Gfx.HardwareRenderer _hardwareRenderer;
    public enum RendererType { Software, Hardware }
    public RendererType CurrentRendererType { get; private set; } = RendererType.Hardware;
        private ConsoleHost consoleHost;
        private HomeScreenDisplay display = new HomeScreenDisplay();

        /// <summary>
        /// Gets the width of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferX => _width;
        /// <summary>
        /// Gets the height of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferY => _height;

        public Window() : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            framebuffer = new Surface(_width, _height);
            framebuffer.Window = this;
            consoleHost = new ConsoleHost();
            // Initialize both renderers
            _softwareRenderer = new Asmo.Gfx.SoftwareRenderer(framebuffer);
            _hardwareRenderer = new Asmo.Gfx.HardwareRenderer();
            _renderer = _hardwareRenderer;
        }

        /// <summary>
        /// Switch between hardware and software renderer at runtime.
        /// </summary>
        public void SetRenderer(RendererType type)
        {
            if (type == RendererType.Hardware)
                _renderer = _hardwareRenderer;
            else
                _renderer = _softwareRenderer;
            CurrentRendererType = type;
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
        }
        protected override void OnLoad()
        {
            OpenTK.Graphics.OpenGL.GL.LoadBindings(new OpenTK.Windowing.GraphicsLibraryFramework.GLFWBindingsContext());
            base.OnLoad();
            // Setup OpenGL state
            GL.ClearColor(0f, 0f, 0f, 1f);

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
            framebuffer = new Surface(_width, _height);
            framebuffer.Window = this;
            consoleHost = new ConsoleHost();
            // Allocate persistent upload buffer
            _uploadBuffer = new byte[_width * _height * 4];
            // Create a PBO for async uploads
            _pbo = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, _width * _height * 4, IntPtr.Zero, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            // Set the window title
            Title = "Asmo Game Console";
            // Set the window size to a multiple of framebuffer (e.g., 2x)
            int scale = 2;
            Size = new Vector2i(_width * scale, _height * scale);
            // Set initial viewport (will be updated in OnResize)
            UpdateViewport();

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
                // Resize upload buffer and PBO
                _uploadBuffer = new byte[_width * _height * 4];
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
                GL.BufferData(BufferTarget.PixelUnpackBuffer, _width * _height * 4, IntPtr.Zero, BufferUsageHint.StreamDraw);
                GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            }

            // Use dirty rectangle to minimize updates
            if (surface.IsDirty)
            {
                var (dirtyX, dirtyY, dirtyW, dirtyH) = surface.GetDirtyRect();
                if (dirtyW > 0 && dirtyH > 0)
                {
                    // Use persistent buffer for dirty region
                    int stride = dirtyW * 4;
                    for (int py = 0; py < dirtyH; py++)
                    {
                        int srcY = dirtyY + py;
                        int flippedY = ph - 1 - srcY;
                        if (flippedY < 0 || flippedY >= ph) continue;
                        for (int px = 0; px < dirtyW; px++)
                        {
                            int srcX = dirtyX + px;
                            if (srcX < 0 || srcX >= pw) continue;
                            int i = (py * dirtyW + px) * 4;
                            var c = pixels[srcX][flippedY];
                            _uploadBuffer[i + 0] = (byte)c.R;
                            _uploadBuffer[i + 1] = (byte)c.G;
                            _uploadBuffer[i + 2] = (byte)c.B;
                            _uploadBuffer[i + 3] = (byte)c.A;
                        }
                    }
                    GL.BindTexture(TextureTarget.Texture2D, _texture);
                    // Use PBO for async upload
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _pbo);
                    IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
                    System.Runtime.InteropServices.Marshal.Copy(_uploadBuffer, 0, ptr, dirtyW * dirtyH * 4);
                    GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
                    GL.TexSubImage2D(TextureTarget.Texture2D, 0, dirtyX, dirtyY, dirtyW, dirtyH, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
                    GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
                }
                surface.ClearDirty();
            }
        }

        protected override void OnRenderFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Ensure viewport is correct (in case window was resized)
            UpdateViewport();

            GL.UseProgram(_shaderProgram);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, 0);
            SwapBuffers();

        }

        protected override void OnUpdateFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            framebuffer.Clear(new Color(0, 0, 32, 255)); // dark blue background
            if (!gameLoaded)
            {
                // Pass the current renderer and window size to HomeScreenDisplay
                display.RenderHomeScreen(args, framebuffer);
            }
            else
            {
                consoleHost.Update(args.Time);
                consoleHost.Draw(framebuffer);
            }
            Render(framebuffer, 0, 0);
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
                string rootDir = null;
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

    }
}
