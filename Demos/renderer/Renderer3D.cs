using System;

namespace Renderer3D
{
    // Minimal 3D vector struct
    public struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);
        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);
        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vec3 Normalized() { float l = Length(); return l > 0 ? this * (1f / l) : this; }
    }

    // Minimal 3D camera
    public class Camera
    {
        public Vec3 Position;
        public float Yaw, Pitch;
        public float Fov = 1.5f, ZNear = 0.1f, ZFar = 100f;
        public Camera(Vec3 pos) { Position = pos; }
        public Vec3 Forward => new Vec3(
            (float)(Math.Cos(Pitch) * Math.Sin(Yaw)),
            (float)Math.Sin(Pitch),
            (float)(Math.Cos(Pitch) * Math.Cos(Yaw)));
        public Vec3 Right => new Vec3((float)Math.Sin(Yaw - Math.PI / 2), 0, (float)Math.Cos(Yaw - Math.PI / 2));
        public Vec3 Up => Vec3.Cross(Right, Forward);
    }

    // Minimal 3D mesh (wireframe)
    public class Mesh
    {
        public Vec3[] Vertices;
        public int[,] Edges;
        public Mesh(Vec3[] v, int[,] e) { Vertices = v; Edges = e; }
        public static Mesh Cube(float size = 1f)
        {
            float s = size / 2f;
            return new Mesh(
                new Vec3[] {
                    new Vec3(-s,-s,-s), new Vec3(s,-s,-s), new Vec3(s,s,-s), new Vec3(-s,s,-s),
                    new Vec3(-s,-s,s), new Vec3(s,-s,s), new Vec3(s,s,s), new Vec3(-s,s,s)
                },
                new int[,] {
                    {0,1},{1,2},{2,3},{3,0},
                    {4,5},{5,6},{6,7},{7,4},
                    {0,4},{1,5},{2,6},{3,7}
                }
            );
        }
    }
}
