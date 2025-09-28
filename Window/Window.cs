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
        private int _width = 384, _height = 256; // 2x bigger pixels
        private Surface framebuffer;
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

        public Window() : base(GameWindowSettings.Default, NativeWindowSettings.Default) { }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            GL.DeleteTexture(_texture);
            GL.DeleteProgram(_shaderProgram);
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            Environment.Exit(0); // Force exit to stop audio threads
        }
        protected override void OnLoad()
        {
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
            _shaderProgram = CreateShaderProgram("Window/Shaders/shader.vert", "Window/Shaders/shader.frag");

            // Initialize framebuffer and console host
            framebuffer = new Surface(_width, _height);
            framebuffer.Window = this;
            consoleHost = new ConsoleHost();
            // Set the window title
            Title = "Asmo Game Console";
            // set the window size
            Size = new Vector2i((int)(_width * 2.5), _height * 2);
            GL.Viewport(0, 0, (int)(_width * 1.5), Size.Y * -1);
           
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
            }
            // Read back the current texture data (or keep a persistent buffer)
            byte[] data = new byte[_width * _height * 4];
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
            // Copy pixels into the buffer at (x, y)
            for (int py = 0; py < ph; py++)
            {
                // Flip Y: OpenGL expects (0,0) at bottom-left, Surface is top-left
                int flippedY = ph - 1 - py;
                int dy = y + flippedY;
                if (dy < 0 || dy >= _height) continue;
                for (int px = 0; px < pw; px++)
                {
                    int dx = x + px;
                    if (dx < 0 || dx >= _width) continue;
                    int i = (dy * _width + dx) * 4;
                    var c = pixels[px][py];
                    data[i + 0] = (byte)c.R;
                    data[i + 1] = (byte)c.G;
                    data[i + 2] = (byte)c.B;
                    data[i + 3] = (byte)c.A;
                }
            }
            // Upload the updated buffer
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _width, _height, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        }

        protected override void OnRenderFrame(OpenTK.Windowing.Common.FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);// --- FNA framework update ---
         
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
                display.RenderHomeScreen(args,framebuffer);
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
            GL.Viewport(0, 0, Size.X, Size.Y);
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
            string vertSource = File.ReadAllText(vertPath);
            string fragSource = File.ReadAllText(fragPath);
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
