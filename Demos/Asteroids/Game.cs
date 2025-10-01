using Asmo;
using Asmo.Gfx;
using Asmo.Window.input;
using System;
using System.Collections.Generic;
using Asmo.Audio;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Asteroids
{
    // Minimal Asteroids demo scaffold
    public class Game : IConsoleGame
    {
    private int invincibilityTimer = 0;
    private bool restartRequested = false;
    private Keyboard? kb;
    private int lastSurfaceWidth = 0;
    private int lastSurfaceHeight = 0;
    private float shipX, shipY, shipAngle;
    private float shipVX, shipVY;
    private bool thrusting;
    private List<Asteroid> asteroids = new();
    private List<Bullet> bullets = new();
    private List<(int x, int y)> stars = new();
    private readonly Random rand = new();
    private int score;
    private int lives = 3;
    private bool gameOver = false;
    private int respawnTimer = 0;
    private int fireCooldown = 0;
    // Audio
    private Asmo.Audio.AudioEngine? audio;
    private Asmo.Audio.AudioClip? shootClip;
    private Asmo.Audio.AudioClip? explodeClip;
    private Asmo.Audio.AudioClip? hitClip;

        public void Init(Surface surface)
        {
            if (surface.Window is not null)
                kb = new Keyboard(surface.Window);
            else
                kb = null;
            shipX = surface.Width / 2;
            shipY = surface.Height / 2;
            lastSurfaceWidth = surface.Width;
            lastSurfaceHeight = surface.Height;
            shipAngle = 0f;
            shipVX = shipVY = 0f;
            asteroids.Clear();
            bullets.Clear();
            stars.Clear();
            for (int i = 0; i < 100; i++)
                stars.Add((rand.Next(surface.Width), rand.Next(surface.Height)));
            score = 0;
            lives = 3;
            gameOver = false;
            respawnTimer = 0;
            invincibilityTimer = 0;
            // Audio setup
            if (audio == null)
            {
                // audio = new Asmo.Audio.AudioEngine();
                // shootClip = Asmo.Audio.AudioClip.CreateSquare(900, 0.08, 0.2f);
                // explodeClip = Asmo.Audio.AudioClip.CreateNoise(0.18, 0.2f, 0, 0);
                // hitClip = Asmo.Audio.AudioClip.CreateSine(120, 0.25, 0.2f, 0);
            }
            // Spawn some asteroids
            for (int i = 0; i < 5; i++)
                asteroids.Add(Asteroid.Spawn(surface.Width, surface.Height, rand));
        }

        public void Update(double deltaTime)
        {
            audio?.Update(deltaTime);
            const double targetFrame = 1.0 / 60.0;
            double dt = deltaTime / targetFrame; // dt = 1 at 60fps, <1 if faster, >1 if slower

            if (restartRequested)
            {
                restartRequested = false;
                Init(new Surface(lastSurfaceWidth, lastSurfaceHeight));
                return;
            }
            if (kb == null)
                return;
            if (gameOver)
            {
                if (kb.IsKeyPressed(Keys.Enter))
                {
                    restartRequested = true;
                }
                return;
            }
            if (respawnTimer > 0)
            {
                respawnTimer -= (int)Math.Ceiling(dt);
                if (respawnTimer > 0) return;
                respawnTimer = 0;
            }
            if (invincibilityTimer > 0)
            {
                invincibilityTimer -= (int)Math.Ceiling(dt);
                if (invincibilityTimer < 0) invincibilityTimer = 0;
            }
            // Ship controls
            float rotSpeed = 0.08f * (float)dt;
            if (kb.IsKeyDown(Keys.Left)) shipAngle -= rotSpeed;
            if (kb.IsKeyDown(Keys.Right)) shipAngle += rotSpeed;
            thrusting = kb.IsKeyDown(Keys.Up);
            if (thrusting)
            {
                float thrust = 0.15f * (float)dt;
                shipVX += (float)Math.Cos(shipAngle) * thrust;
                shipVY += (float)Math.Sin(shipAngle) * thrust;
            }
            // Fire (with cooldown)
            if (fireCooldown > 0) fireCooldown -= (int)Math.Ceiling(dt);
            if (fireCooldown < 0) fireCooldown = 0;
            if (kb.IsKeyDown(Keys.Space) && fireCooldown == 0)
            {
                bullets.Add(new Bullet(shipX, shipY, shipAngle));
                fireCooldown = 10; // frames between shots (now in 60fps units)
                if (audio is not null && shootClip is not null)
                    audio.PlayClip(shootClip);
            }
            // Move ship
            shipX += shipVX * (float)dt;
            shipY += shipVY * (float)dt;
            float drag = (float)Math.Pow(0.99f, dt); // frame-rate independent drag
            shipVX *= drag;
            shipVY *= drag;
            Wrap(ref shipX, 0, lastSurfaceWidth);
            Wrap(ref shipY, 0, lastSurfaceHeight);
            // Move asteroids
            foreach (var a in asteroids) a.Update(lastSurfaceWidth, lastSurfaceHeight);
            // Move bullets
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                bullets[i].Update((float)dt);
                if (bullets[i].Life <= 0) bullets.RemoveAt(i);
            }
            // Bullet-asteroid collision
            for (int i = asteroids.Count - 1; i >= 0; i--)
            {
                var a = asteroids[i];
                for (int j = bullets.Count - 1; j >= 0; j--)
                {
                    var b = bullets[j];
                    float dx = a.X - b.X, dy = a.Y - b.Y;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist < a.R)
                    {
                        score += 10;
                        bullets.RemoveAt(j);
                        // Split asteroid or remove
                        if (a.R > 16)
                        {
                            asteroids.Add(new Asteroid(a.X, a.Y, (float)(rand.NextDouble() * 2 - 1) * 2f, (float)(rand.NextDouble() * 2 - 1) * 2f, a.R / 2));
                            asteroids.Add(new Asteroid(a.X, a.Y, (float)(rand.NextDouble() * 2 - 1) * 2f, (float)(rand.NextDouble() * 2 - 1) * 2f, a.R / 2));
                        }
                        asteroids.RemoveAt(i);
                        if (audio is not null && explodeClip is not null)
                            audio.PlayClip(explodeClip);
                        break;
                    }
                }
            }
            // Ship-asteroid collision (skip if invincible)
            if (invincibilityTimer == 0)
            {
                for (int i = 0; i < asteroids.Count; i++)
                {
                    var a = asteroids[i];
                    float dx = a.X - shipX, dy = a.Y - shipY;
                    float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist < a.R + 10)
                    {
                        lives--;
                        if (audio is not null && hitClip is not null)
                            audio.PlayClip(hitClip);
                        if (lives <= 0)
                        {
                            gameOver = true;
                        }
                        else
                        {
                            // Respawn ship (ensure not on asteroid)
                            bool safe = false;
                            int attempts = 0;
                            while (!safe && attempts < 20)
                            {
                                shipX = lastSurfaceWidth / 2 + rand.Next(-40, 40);
                                shipY = lastSurfaceHeight / 2 + rand.Next(-40, 40);
                                safe = true;
                                foreach (var ast in asteroids)
                                {
                                    float ddx = ast.X - shipX, ddy = ast.Y - shipY;
                                    float ddist = (float)Math.Sqrt(ddx * ddx + ddy * ddy);
                                    if (ddist < ast.R + 24)
                                    {
                                        safe = false;
                                        break;
                                    }
                                }
                                attempts++;
                            }
                            shipVX = shipVY = 0;
                            respawnTimer = 60;
                            invincibilityTimer = 90; // 1.5 seconds invincible
                        }
                        break;
                    }
                }
            }
            // Win: all asteroids destroyed
            if (asteroids.Count == 0)
            {
                for (int i = 0; i < 5; i++)
                    asteroids.Add(Asteroid.Spawn(lastSurfaceWidth, lastSurfaceHeight, rand));
            }
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.Black);
            // Draw stars
            foreach (var star in stars) surface.SetPixel(star.x, star.y, Colors.White);
            // Draw asteroids
            foreach (var a in asteroids) a.Draw(surface);
            // Draw bullets
            foreach (var b in bullets) b.Draw(surface);
            // Draw ship (if not game over or respawning)
            if (!gameOver && respawnTimer == 0)
            {
                // During invincibility, flicker but always show at least every other frame
                bool showShip = true;
                if (invincibilityTimer > 0)
                {
                    // Flicker: hide 2 out of every 6 frames (show 4/6)
                    showShip = (invincibilityTimer % 6) < 4;
                }
                if (showShip)
                    DrawShip(surface, shipX, shipY, shipAngle, thrusting);
            }
            // Score and lives
            surface.DrawText(10, 10, $"Score: {score}", Colors.White);
            surface.DrawText(10, 28, $"Lives: {lives}", Colors.White);
            if (gameOver)
            {
                surface.DrawText(surface.Width / 2 - 40, surface.Height / 2 - 10, "GAME OVER", Colors.Red);
                surface.DrawText(surface.Width / 2 - 60, surface.Height / 2 + 10, "Press Enter to Restart", Colors.Yellow);
            }
            else if (respawnTimer > 0)
            {
                surface.DrawText(surface.Width / 2 - 40, surface.Height / 2, "Respawning...", Colors.Cyan);
            }
        }

        private void DrawShip(Surface s, float x, float y, float angle, bool thrusting)
        {
            // Simple triangle for ship
            float r = 12f;
            float a1 = angle;
            float a2 = angle + 2.5f;
            float a3 = angle - 2.5f;
            int x1 = (int)(x + Math.Cos(a1) * r);
            int y1 = (int)(y + Math.Sin(a1) * r);
            int x2 = (int)(x + Math.Cos(a2) * r * 0.7f);
            int y2 = (int)(y + Math.Sin(a2) * r * 0.7f);
            int x3 = (int)(x + Math.Cos(a3) * r * 0.7f);
            int y3 = (int)(y + Math.Sin(a3) * r * 0.7f);
            s.DrawLine(x1, y1, x2, y2, Colors.Cyan);
            s.DrawLine(x2, y2, x3, y3, Colors.Cyan);
            s.DrawLine(x3, y3, x1, y1, Colors.Cyan);
            // Thrust flame
            if (thrusting)
            {
                float backAngle = angle + (float)Math.PI;
                int fx1 = (int)(x + Math.Cos(backAngle) * r * 0.5f);
                int fy1 = (int)(y + Math.Sin(backAngle) * r * 0.5f);
                int fx2 = (int)(x + Math.Cos(backAngle + 0.5f) * r * 0.8f);
                int fy2 = (int)(y + Math.Sin(backAngle + 0.5f) * r * 0.8f);
                int fx3 = (int)(x + Math.Cos(backAngle - 0.5f) * r * 0.8f);
                int fy3 = (int)(y + Math.Sin(backAngle - 0.5f) * r * 0.8f);
                s.DrawLine(fx1, fy1, fx2, fy2, Colors.Orange);
                s.DrawLine(fx2, fy2, fx3, fy3, Colors.Orange);
                s.DrawLine(fx3, fy3, fx1, fy1, Colors.Orange);
            }
        }

        private void Wrap(ref float v, float min, float max)
        {
            if (v < min) v = max;
            if (v > max) v = min;
        }
    }

    public class Asteroid
    {
        public float X, Y, VX, VY, R;
        public Asteroid(float x, float y, float vx, float vy, float r)
        {
            X = x; Y = y; VX = vx; VY = vy; R = r;
        }
        public void Update(int w, int h)
        {
            X += VX; Y += VY;
            if (X < 0) X = w;
            if (X > w) X = 0;
            if (Y < 0) Y = h;
            if (Y > h) Y = 0;
        }
        public void Draw(Surface s)
        {
            s.DrawCircle((int)X, (int)Y, (int)R, Colors.Gray);
        }
        public static Asteroid Spawn(int w, int h, Random rand)
        {
            float x = rand.Next(w), y = rand.Next(h);
            float vx = (float)(rand.NextDouble() * 2 - 1) * 1.5f;
            float vy = (float)(rand.NextDouble() * 2 - 1) * 1.5f;
            float r = rand.Next(18, 32);
            return new Asteroid(x, y, vx, vy, r);
        }
    }

    public class Bullet
    {
        public float X, Y, VX, VY;
        public int Life = 60;
        public Bullet(float x, float y, float angle)
        {
            X = x; Y = y;
            VX = (float)Math.Cos(angle) * 6f;
            VY = (float)Math.Sin(angle) * 6f;
        }
        public void Update(float dt = 1f)
        {
            X += VX * dt; Y += VY * dt; Life -= (int)Math.Ceiling(dt);
        }
        public void Draw(Surface s)
        {
            s.DrawRect((int)X - 1, (int)Y - 1, 3, 3, Colors.Yellow);
        }
    }
}
