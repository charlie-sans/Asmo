using Asmo;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using Asmo.Window.input;
using System;
using System.IO;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace AstroTestGame
{
    public class Game : IConsoleGame
    {
        private Keyboard? kb;
        private readonly Random rand = new();

        // Audio
        private AudioEngine? _audio;
        private AudioBus? _musicBus;
        private AudioBus? _sfxBus;
    private AudioHandle? _ambientHandle;
    private AudioClip? _ambientClip;
    private AudioClip? _powerUpClip;
        private double _impactCooldown;

        // Player fields
        private int playerX = 150, playerY = 100;
        private int playerW = 16, playerH = 16;
        private int playerSpeed = 3;
        private readonly Color playerColor = Colors.Yellow;

        // Power-up fields
        private int powerUpX = -100, powerUpY = -100;
        private int powerUpW = 12, powerUpH = 12;
        private bool powerUpActive;
        private int powerUpTimer;
        private const int powerUpDuration = 180;
        private int powerUpEffectTimer;
        private bool powerUpEffectActive;
        private const int normalPlayerSpeed = 3;
        private const int boostedPlayerSpeed = 6;

        // DVD logo fields
        private int dvdBoxX = 10, dvdBoxY = 10;
        private const int dvdBoxW = 60, dvdBoxH = 30;
        private int dvdBoxVX = 2, dvdBoxVY = 2;
        private Color dvdBoxColor = Colors.White;
        private readonly Sprite disk = new(10, 10, new Color[][]
        {
            new[] { Colors.Transparent, Colors.White, Colors.White, Colors.White, Colors.Transparent },
            new[] { Colors.White, Colors.Blue, Colors.Blue, Colors.Blue, Colors.White },
            new[] { Colors.White, Colors.Blue, Colors.Blue, Colors.Blue, Colors.White },
            new[] { Colors.White, Colors.Blue, Colors.Blue, Colors.Blue, Colors.White },
            new[] { Colors.Transparent, Colors.White, Colors.White, Colors.White, Colors.Transparent },
            new[] { Colors.Transparent, Colors.Transparent, Colors.White, Colors.Transparent, Colors.Transparent }
        });

        // Timer/game over
        private float timeLeft = 30f;
        private int combo;
        private int comboTimer;
        private const int comboTimeout = 60;

        private enum GameState { StartScreen, Playing, GameOver }
        private GameState gameState = GameState.StartScreen;
        private int score;
        private int highScore;
        private bool canRestart;
        private int shakeTimer;
        private int shakeAmount;

        public void Init(Surface surface)
        {
            kb = new Keyboard(surface.Window);
            // Enable audio diagnostics and logging
            _audio = new AudioEngine();
            AudioEngine.DiagnosticsEnabled = true;
            var logPath = Path.Combine(AppContext.BaseDirectory, "astro-audio.log");
            var logEncoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AudioEngine.DiagnosticsSink = msg =>
            {
                Console.WriteLine(msg);
                File.AppendAllText(logPath, msg + Environment.NewLine, logEncoding);
            };
            InitialiseAudio();
        }

        public void Update(double deltaTime)
        {
            _audio?.Update(deltaTime);

            if (_impactCooldown > 0)
            {
                _impactCooldown = Math.Max(0, _impactCooldown - deltaTime);
            }

            if (kb == null) return;

            switch (gameState)
            {
                case GameState.StartScreen:
                    if (kb.IsKeyPressed(Keys.Enter))
                    {
                        StartNewGame();
                    }
                    return;
                case GameState.GameOver:
                    if (!kb.IsKeyDown(Keys.Enter))
                    {
                        canRestart = true;
                    }
                    else if (canRestart && kb.IsKeyPressed(Keys.Enter))
                    {
                        StartNewGame();
                        canRestart = false;
                    }
                    return;
            }

            // Timer
            timeLeft -= (float)deltaTime;
            if (timeLeft <= 0f)
            {
                if (score > highScore) highScore = score;
                timeLeft = 0f;
                gameState = GameState.GameOver;
                canRestart = false;
                StopAmbientLoop();
                return;
            }

            HandlePlayerMovement();
            UpdatePowerUps();
            UpdateDvdLogo();
            HandleCollisions();

            if (combo > 0)
            {
                comboTimer--;
                if (comboTimer <= 0) combo = 0;
            }

            if (shakeTimer > 0) shakeTimer--;
        }

        public void Draw(Surface surface)
        {
            int shakeX = 0, shakeY = 0;
            if (shakeTimer > 0)
            {
                shakeX = rand.Next(-shakeAmount, shakeAmount + 1);
                shakeY = rand.Next(-shakeAmount, shakeAmount + 1);
            }

            surface.Clear(Colors.DarkGreen);

            if (gameState == GameState.StartScreen)
            {
                surface.DrawText(surface.Width / 2 - 60, surface.Height / 2 - 30, "ASTRO TEST GAME", Colors.Yellow);
                surface.DrawText(surface.Width / 2 - 50, surface.Height / 2 - 10, $"High Score: {highScore}", Colors.Cyan);
                surface.DrawText(surface.Width / 2 - 70, surface.Height / 2 + 10, "Press Enter to Start", Colors.White);
                return;
            }

            if (gameState == GameState.GameOver)
            {
                surface.DrawText(surface.Width / 2 - 50 + shakeX, surface.Height / 2 - 20 + shakeY, "GAME OVER", Colors.Red);
                surface.DrawText(surface.Width / 2 - 60 + shakeX, surface.Height / 2 + shakeY, $"Final Score: {score}", Colors.Yellow);
                surface.DrawText(surface.Width / 2 - 60 + shakeX, surface.Height / 2 + 20 + shakeY, $"High Score: {highScore}", Colors.Cyan);
                surface.DrawText(surface.Width / 2 - 70 + shakeX, surface.Height / 2 + 40 + shakeY, "Press Enter for Menu", Colors.White);
                return;
            }

            surface.DrawText(10 + shakeX, 30 + shakeY, $"Score: {score}", Colors.Yellow);
            surface.DrawText(10 + shakeX, 40 + shakeY, "Arrow keys: Move | Touch DVD logo! | Grab power-ups!", Colors.White);
            surface.DrawText(10 + shakeX, 50 + shakeY, $"Time: {MathF.Ceiling(timeLeft)}", Colors.White);
            if (combo > 1)
            {
                surface.DrawText(10 + shakeX, 65 + shakeY, $"Combo: {combo}x!", Colors.Cyan);
            }

            surface.DrawRect(dvdBoxX + shakeX, dvdBoxY + shakeY, dvdBoxW, dvdBoxH, dvdBoxColor);
            surface.DrawSprite(disk, dvdBoxX + 5 + shakeX, dvdBoxY + 5 + shakeY);
            surface.DrawText(dvdBoxX + 10 + shakeX, dvdBoxY + dvdBoxH / 2 - 4 + shakeY, "DVD", Colors.Black);
            surface.DrawRect(playerX + shakeX, playerY + shakeY, playerW, playerH, playerColor);

            if (powerUpActive)
            {
                surface.DrawRect(powerUpX + shakeX, powerUpY + shakeY, powerUpW, powerUpH, Colors.Magenta);
                surface.DrawText(powerUpX - 2 + shakeX, powerUpY - 10 + shakeY, "P", Colors.White);
            }

            if (powerUpEffectActive)
            {
                surface.DrawText(10 + shakeX, 80 + shakeY, "Speed Boost!", Colors.Magenta);
            }
        }

        private void InitialiseAudio()
        {
            try
            {
                if (_audio == null)
                {
                    _audio = new AudioEngine();
                    _musicBus = _audio.GetOrCreateBus("music");
                    _musicBus.Volume = 0.35f;
                    _sfxBus = _audio.GetOrCreateBus("sfx");
                    _sfxBus.Volume = 0.85f;
                }
                _powerUpClip ??= AudioClip.CreateSquare(1100, 0.2, 0.55f);

                if (_audio == null || _musicBus == null)
                    return;

                _sfxBus ??= _audio.GetOrCreateBus("sfx");

                var assetRoot = GameEnvironment.AssetRoot ?? AppContext.BaseDirectory;
                var path = Path.Combine(assetRoot, "Assets", "Sound", "Demo.wav");
                if (_ambientClip == null)
                {
                    if (File.Exists(path))
                    {
                        _ambientClip = AudioClip.Load(path);
                    }
                    else
                    {
                        _ambientClip = AudioClip.CreateSine(220, 3.5, 0.2f);
                    }
                }

                if (_ambientHandle != null && _ambientHandle.IsPlaying)
                {
                    _ambientHandle.FadeTo(0.3f, 0.8);
                }
                else if (_ambientClip != null)
                {
                    _ambientHandle = _musicBus.Play(_ambientClip, new AudioPlaybackSettings
                    {
                        Loop = true,
                        Volume = 0.3f,
                        FadeInSeconds = 1.2
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Audio init failed: {ex.Message}");
            }
        }

        private void HandlePlayerMovement()
        {
            if (kb == null) return;

            if (kb.IsKeyDown(Keys.A)) playerX -= playerSpeed;
            if (kb.IsKeyDown(Keys.D)) playerX += playerSpeed;
            if (kb.IsKeyDown(Keys.W)) playerY -= playerSpeed;
            if (kb.IsKeyDown(Keys.S)) playerY += playerSpeed;

            playerX = Math.Clamp(playerX, 0, surfaceWidth - playerW);
            playerY = Math.Clamp(playerY, 0, surfaceHeight - playerH);
        }

        private void UpdatePowerUps()
        {
            if (!powerUpActive && rand.Next(0, 300) == 0)
            {
                powerUpX = rand.Next(surfaceWidth - powerUpW);
                powerUpY = rand.Next(surfaceHeight - powerUpH);
                powerUpActive = true;
                powerUpTimer = 180;
            }

            if (!powerUpActive) return;

            powerUpTimer--;
            if (powerUpTimer <= 0)
            {
                powerUpActive = false;
                powerUpX = powerUpY = -100;
                return;
            }

            if (playerX < powerUpX + powerUpW && playerX + playerW > powerUpX &&
                playerY < powerUpY + powerUpH && playerY + playerH > powerUpY)
            {
                powerUpActive = false;
                powerUpX = powerUpY = -100;
                powerUpEffectActive = true;
                powerUpEffectTimer = powerUpDuration;
                playerSpeed = boostedPlayerSpeed;
                PlayPowerUpSound();
                shakeTimer = 10;
                shakeAmount = 4;
            }

            if (powerUpEffectActive)
            {
                powerUpEffectTimer--;
                if (powerUpEffectTimer <= 0)
                {
                    powerUpEffectActive = false;
                    playerSpeed = normalPlayerSpeed;
                }
            }
        }

        private void UpdateDvdLogo()
        {
            dvdBoxX += dvdBoxVX;
            dvdBoxY += dvdBoxVY;

            if (dvdBoxX <= 0 || dvdBoxX + dvdBoxW >= surfaceWidth)
            {
                dvdBoxVX = -dvdBoxVX;
                PlayBounceSound();
            }

            if (dvdBoxY <= 0 || dvdBoxY + dvdBoxH >= surfaceHeight)
            {
                dvdBoxVY = -dvdBoxVY;
                PlayBounceSound();
            }
        }

        private void HandleCollisions()
        {
            if (playerX < dvdBoxX + dvdBoxW && playerX + playerW > dvdBoxX &&
                playerY < dvdBoxY + dvdBoxH && playerY + playerH > dvdBoxY)
            {
                dvdBoxColor = new Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);
                score++;
                combo++;
                comboTimer = comboTimeout;
                dvdBoxX = rand.Next(surfaceWidth - dvdBoxW);
                dvdBoxY = rand.Next(surfaceHeight - dvdBoxH);
                shakeTimer = 8;
                shakeAmount = 3;
                PlayCollisionSound();
            }
        }

        private void StartNewGame()
        {
            score = 0;
            timeLeft = 30f;
            combo = 0;
            comboTimer = 0;
            playerX = 150;
            playerY = 100;
            dvdBoxX = 10;
            dvdBoxY = 10;
            powerUpActive = false;
            powerUpEffectActive = false;
            playerSpeed = normalPlayerSpeed;
            shakeTimer = 0;
            shakeAmount = 0;
            gameState = GameState.Playing;
            if (_ambientHandle == null || !_ambientHandle.IsPlaying)
            {
                InitialiseAudio();
            }
        }

        private void StopAmbientLoop()
        {
            if (_ambientHandle != null && _ambientHandle.IsPlaying)
            {
                _ambientHandle.FadeTo(0.0f, 0.5);
            }
        }

        private void PlayPowerUpSound()
        {
            if (_sfxBus == null || _powerUpClip == null) return;
            _sfxBus.Play(_powerUpClip, new AudioPlaybackSettings
            {
                Volume = 0.75f,
                FadeInSeconds = 0.02
            });
        }

        private void PlayBounceSound()
        {
            if (_sfxBus == null) return;
            if (_impactCooldown > 0) return;

            var clip = AudioClip.CreateSquare(400 + rand.Next(300), 0.12, 0.35f);
            _sfxBus.Play(clip, new AudioPlaybackSettings
            {
                Volume = 0.55f,
                FadeInSeconds = 0.005
            });
            _impactCooldown = 0.05;
        }

        private void PlayCollisionSound()
        {
            if (_sfxBus == null) return;

            var clip = AudioClip.CreateNoise(0.1, 0.4f);
            _sfxBus.Play(clip, new AudioPlaybackSettings
            {
                Volume = 0.7f,
                FadeInSeconds = 0.01
            });
        }

        private int surfaceWidth => GameEnvironment.WindowX;
        private int surfaceHeight => GameEnvironment.WindowY;
    }
}
