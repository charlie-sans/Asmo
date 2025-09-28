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
using Asmo.Window.input; // Add at top
using OpenTK.Windowing.Common;

namespace Asmo.Window
{
    public class Window : GameWindow
    {
        private bool gameLoaded = false;
        private int _texture;
        private int _shaderProgram;
        private int _vao, _vbo;
        private int _width = 384, _height = 256;
        private Surface framebuffer;
        private ConsoleHost consoleHost;
        private HomeScreenDisplay display = new HomeScreenDisplay();
        private Mouse mouse;
        public Mouse Mouse => mouse;

        // New configuration properties
        public WindowSettings Settings { get; private set; }
        public GraphicsConfig GraphicsConfig { get; private set; }
        public WindowManager Manager { get; private set; }

        /// <summary>
        /// Gets the width of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferX => _width;
        /// <summary>
        /// Gets the height of the frame buffer in pixels.
        /// </summary>
        public int FrameBufferY => _height;

        public Window(WindowSettings? settings = null, GraphicsConfig? graphicsConfig = null) 
            : base(GameWindowSettings.Default, NativeWindowSettings.Default) 
        {
            Settings = settings ?? new WindowSettings();
            GraphicsConfig = graphicsConfig ?? GraphicsConfig.FromQuality(RenderingQuality.Retro);
            mouse = new Mouse(this);
            Manager = new WindowManager(this);
            
            ApplyWindowSettings();
        }

        /// <summary>
        /// Helper method to create a window with specific quality preset
        /// </summary>
        public static Window CreateWithQuality(RenderingQuality quality, WindowSettings? settings = null)
        {
            var config = GraphicsConfig.FromQuality(quality);
            var windowSettings = settings ?? (quality switch
            {
                RenderingQuality.Retro => WindowSettings.Presets.Retro,
                RenderingQuality.Enhanced => WindowSettings.Presets.Modern,
                RenderingQuality.HighQuality => WindowSettings.Presets.HighRes,
                _ => new WindowSettings()
            });
            
            return new Window(windowSettings, config);
        }

        /// <summary>
        /// Update window settings at runtime
        /// </summary>
        public void UpdateSettings(WindowSettings newSettings)
        {
            Settings = newSettings;
            ApplyWindowSettings();
            
            // Recreate framebuffer with new size
            if (framebuffer is EnhancedSurface enhanced)
            {
                enhanced.Dispose();
            }
            
            _width = Settings.FramebufferWidth;
            _height = Settings.FramebufferHeight;
            CreateFramebuffer();
            
            // Update OpenGL viewport
            GL.Viewport(0, 0, Settings.Width, Settings.Height);
            
            // Recreate texture with new framebuffer size
            if (_texture != 0)
            {
                GL.DeleteTexture(_texture);
                CreateTexture();
            }
        }

        /// <summary>
        /// Update graphics configuration
        /// </summary>
        public void UpdateGraphicsConfig(GraphicsConfig newConfig)
        {
            Console.WriteLine($"UpdateGraphicsConfig called - Old: {GraphicsConfig.Quality}, New: {newConfig.Quality}");
            GraphicsConfig = newConfig;
            
            // Recreate framebuffer with new config
            if (framebuffer is EnhancedSurface enhanced)
            {
                enhanced.Dispose();
            }
            CreateFramebuffer();
            Console.WriteLine($"Framebuffer recreated with quality: {GraphicsConfig.Quality}");
            
            // Update texture filtering
            if (_texture != 0)
            {
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                var minFilter = GraphicsConfig.EnableFilteredScaling ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
                var magFilter = GraphicsConfig.EnableFilteredScaling ? TextureMagFilter.Linear : TextureMagFilter.Nearest;
                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
                Console.WriteLine($"Texture filtering updated - Linear: {GraphicsConfig.EnableFilteredScaling}");
            }
        }

        private void ApplyWindowSettings()
        {
            Title = Settings.Title;
            Size = new Vector2i(Settings.Width, Settings.Height);
            
            // Apply VSync setting
            VSync = Settings.VSync ? VSyncMode.On : VSyncMode.Off;
            
            // Apply fullscreen setting
            if (Settings.Fullscreen && WindowState != WindowState.Fullscreen)
            {
                WindowState = WindowState.Fullscreen;
            }
            else if (!Settings.Fullscreen && WindowState == WindowState.Fullscreen)
            {
                WindowState = WindowState.Normal;
            }
            
            // Apply size constraints
            if (Settings.MinSize.HasValue)
            {
                // Note: OpenTK doesn't have direct MinSize property, would need platform-specific implementation
                // For now, we'll store it in settings for future use
            }
            if (Settings.MaxSize.HasValue)
            {
                // Note: OpenTK doesn't have direct MaxSize property, would need platform-specific implementation
                // For now, we'll store it in settings for future use
            }
        }

        private void CreateFramebuffer()
        {
            if (GraphicsConfig.Quality == RenderingQuality.Retro)
            {
                framebuffer = new Surface(_width, _height);
            }
            else
            {
                framebuffer = new EnhancedSurface(_width, _height, GraphicsConfig);
            }
            framebuffer.Window = this;
        }

        private void CreateTexture()
        {
            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            
            // Set texture filtering based on graphics config
            var minFilter = GraphicsConfig.EnableFilteredScaling ? TextureMinFilter.Linear : TextureMinFilter.Nearest;
            var magFilter = GraphicsConfig.EnableFilteredScaling ? TextureMagFilter.Linear : TextureMagFilter.Nearest;
            
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            // Dispose of enhanced surface resources
            if (framebuffer is EnhancedSurface enhanced)
            {
                enhanced.Dispose();
            }
            
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

            // Create texture first
            CreateTexture();

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
            _width = Settings.FramebufferWidth;
            _height = Settings.FramebufferHeight;
            CreateFramebuffer();
            consoleHost = new ConsoleHost();

            Console.WriteLine($"Window loaded - Framebuffer: {_width}x{_height}, Quality: {GraphicsConfig.Quality}");

            // Apply final window settings
            Size = new Vector2i(Settings.Width, Settings.Height);
            GL.Viewport(0, 0, Settings.Width, Settings.Height);
        }

        public void Render(Surface surface, int x, int y)
        {
            var pixels = surface.Pixels;
            int pw = surface.Width;
            int ph = surface.Height;
            if (pixels == null || pw == 0 || ph == 0)
                return;

            // Ensure texture is large enough (resize if needed)
            if (_width < x + pw || _height < y + ph)
            {
                _width = Math.Max(_width, x + pw);
                _height = Math.Max(_height, y + ph);
                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, _width, _height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            }

            // Create pixel data array directly from surface
            byte[] data = new byte[_width * _height * 4];
            
            // Initialize with black/transparent
            for (int i = 0; i < data.Length; i += 4)
            {
                data[i + 0] = 0;     // R
                data[i + 1] = 0;     // G  
                data[i + 2] = 0;     // B
                data[i + 3] = 255;   // A (opaque black background)
            }

            // Copy surface pixels into the buffer at (x, y)
            for (int py = 0; py < ph; py++)
            {
                int dy = y + py;
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

            // Upload the complete buffer to the texture
            GL.BindTexture(TextureTarget.Texture2D, _texture);
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
            // Don't update mouse at the start - do it at the end
            base.OnUpdateFrame(args);
            
            // Clear with debug color so we can see if anything is being drawn
            framebuffer.Clear(new Color(0, 0, 32, 255)); // dark blue background
            
            if (!gameLoaded)
            {
                // Show home screen
                display.RenderHomeScreen(args, framebuffer);
            }
            else
            {
                // Run the loaded game
                consoleHost.Update(args.Time);
                consoleHost.Draw(framebuffer);
            }
            
            // Always render the framebuffer to screen
            Render(framebuffer, 0, 0);
            
            // Reset mouse pressed/released states AFTER all GUI checks are done
            mouse.Update();
        }
        
        protected override void OnResize(OpenTK.Windowing.Common.ResizeEventArgs e)
        {
            base.OnResize(e);
            // Keep viewport consistent with window size
            GL.Viewport(0, 0, Size.X, Size.Y);
            
            // Update settings to reflect new size
            Settings.Width = Size.X;
            Settings.Height = Size.Y;
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
