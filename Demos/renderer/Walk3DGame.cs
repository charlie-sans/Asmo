using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
using Renderer3D;

namespace Demos.Renderer
{
    public class Walk3DGame : IConsoleGame
    {
    private Keyboard kb;
        private Camera cam = new Camera(new Vec3(0, 1, 6));
        private Mesh cube = Mesh.Cube(1f);
        private float yaw = 0, pitch = 0;
        private float moveSpeed = 2.5f;
        private float rotSpeed = 1.5f;

        // Simple level: array of (center, size, color)
        private (Vec3 center, Vec3 size, Asmo.Types.Color color)[] level = new[]
        {
            // Floor
            (new Vec3(0, -0.5f, 0), new Vec3(8, 1, 8), Colors.DarkGray),
            // Walls
            (new Vec3(-4, 1, 0), new Vec3(1, 3, 8), Colors.Gray),
            (new Vec3(4, 1, 0), new Vec3(1, 3, 8), Colors.Gray),
            (new Vec3(0, 1, -4), new Vec3(8, 3, 1), Colors.Gray),
            (new Vec3(0, 1, 4), new Vec3(8, 3, 1), Colors.Gray),
            // Pillar
            (new Vec3(2, 1, 2), new Vec3(1, 3, 1), Colors.Gray),
        };

        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
        }

        public void Update(double deltaTime)
        {
            if (kb == null) return;
            float dt = (float)deltaTime;
            // Camera rotation (standard FPS: left = decrease yaw, up = decrease pitch)
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left)) yaw += rotSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right)) yaw -= rotSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up)) pitch -= rotSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down)) pitch += rotSpeed * dt;
            pitch = Math.Clamp(pitch, -1.2f, 1.2f);
            cam.Yaw = yaw;
            cam.Pitch = pitch;
            // Movement relative to yaw (ignore pitch for ground movement)
            float moveYaw = yaw;
            float sinY = (float)Math.Sin(moveYaw);
            float cosY = (float)Math.Cos(moveYaw);
            Vec3 moveForward = new Vec3(sinY, 0, cosY).Normalized();
            Vec3 moveRight = new Vec3(cosY, 0, -sinY).Normalized();
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W)) cam.Position += moveForward * moveSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S)) cam.Position -= moveForward * moveSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A)) cam.Position -= moveRight * moveSpeed * dt;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)) cam.Position += moveRight * moveSpeed * dt;
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);
            int w = surface.Width, h = surface.Height;
            float scale = Math.Min(w, h) * 0.5f;

            // Helper: project a 3D point to 2D
            float[] Project(Vec3 v)
            {
                Vec3 rel = v - cam.Position;
                float cy = (float)Math.Cos(-cam.Yaw), sy = (float)Math.Sin(-cam.Yaw);
                float cp = (float)Math.Cos(-cam.Pitch), sp = (float)Math.Sin(-cam.Pitch);
                float x = rel.X * cy - rel.Z * sy;
                float z = rel.X * sy + rel.Z * cy;
                float y = rel.Y;
                float y2 = y * cp - z * sp;
                float z2 = y * sp + z * cp;
                float fov = cam.Fov;
                float proj = fov / (z2 > 0.1f ? z2 : 0.1f);
                return new float[] { w / 2 + x * scale * proj, h / 2 + y2 * scale * proj };
            }

            // Draw level boxes
            foreach (var (center, size, color) in level)
            {
                // 8 corners of the box
                Vec3[] corners = new Vec3[8];
                for (int i = 0; i < 8; i++)
                {
                    corners[i] = new Vec3(
                        center.X + size.X / 2 * ((i & 1) == 0 ? -1 : 1),
                        center.Y + size.Y / 2 * ((i & 2) == 0 ? -1 : 1),
                        center.Z + size.Z / 2 * ((i & 4) == 0 ? -1 : 1)
                    );
                }
                int[,] edges = new int[,] {
                    {0,1},{1,3},{3,2},{2,0}, // bottom
                    {4,5},{5,7},{7,6},{6,4}, // top
                    {0,4},{1,5},{2,6},{3,7}  // sides
                };
                float[,] proj = new float[8,2];
                for (int i = 0; i < 8; i++)
                {
                    var p = Project(corners[i]);
                    proj[i,0] = p[0]; proj[i,1] = p[1];
                }
                for (int e = 0; e < edges.GetLength(0); e++)
                {
                    int a = edges[e,0], b = edges[e,1];
                    int x0 = (int)proj[a,0], y0 = (int)proj[a,1];
                    int x1 = (int)proj[b,0], y1 = (int)proj[b,1];
                    surface.DrawLine(x0, y0, x1, y1, color);
                }
            }

            // Draw player marker cube at (0,1,0)
            float[,] projected = new float[cube.Vertices.Length, 2];
            for (int i = 0; i < cube.Vertices.Length; i++)
            {
                Vec3 v = cube.Vertices[i] + new Vec3(0, 1, 0);
                var p = Project(v);
                projected[i, 0] = p[0];
                projected[i, 1] = p[1];
            }
            for (int e = 0; e < cube.Edges.GetLength(0); e++)
            {
                int a = cube.Edges[e, 0], b = cube.Edges[e, 1];
                int x0 = (int)projected[a, 0], y0 = (int)projected[a, 1];
                int x1 = (int)projected[b, 0], y1 = (int)projected[b, 1];
                surface.DrawLine(x0, y0, x1, y1, Colors.Cyan);
            }

            surface.DrawText(10, 10, $"WASD: Move, Arrows: Look", Colors.Yellow);
            surface.DrawText(10, 30, $"Camera: {cam.Position.X:F2},{cam.Position.Y:F2},{cam.Position.Z:F2}", Colors.White);
        }
    }
}
