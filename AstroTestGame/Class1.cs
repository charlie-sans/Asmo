using Asmo;
using Asmo.Sound;
using Asmo.Window.input;
using CSCore.SoundOut;
using Megadrive;
using Microsoft.Xna.Framework;
using System;
using System.Threading;
using Asmo.Types;
using Asmo.Gfx;
using CSCore.Codecs;
using CSCore;

namespace AstroTestGame
{
    public class Game : Asmo.Gfx.IConsoleGame
    {
        private ChipTunePlayer _player;
        private SimpleMixer _SimpleMixer = new SimpleMixer();
        Asmo.Sound.Instrument square = (freq, dur, amp) => new SimpleSquareWaveSource(freq, dur, amp);
        Asmo.Sound.Instrument noise = (freq, dur, amp) => SoundSynth.WhiteNoise(dur, amp);
        Asmo.Sound.Instrument kick = (freq, dur, amp) => SoundSynth.DrumKick(1f, 440f, 40f, 1f);
        IWaveSource factory = CodecFactory.Instance.GetCodec(Path.Join(GameEnvironment.AssetRoot, "Assets/Sound/Demo.wav"));
        Keyboard kb;
        int i = 0;

        // DVD logo fields
        int dvdBoxX = 10, dvdBoxY = 10;
        int dvdBoxW = 60, dvdBoxH = 30;
        int dvdBoxVX = 2, dvdBoxVY = 2;
        Asmo.Types.Color dvdBoxColor = Asmo.Gfx.Colors.White;
        Sprite disk = new Sprite(10, 10, new Asmo.Types.Color[][] {
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.Transparent, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Transparent },
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.White },
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.White },
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.Blue, Asmo.Gfx.Colors.White },
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.Transparent, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Transparent },
            new Asmo.Types.Color[] { Asmo.Gfx.Colors.Transparent, Asmo.Gfx.Colors.Transparent, Asmo.Gfx.Colors.White, Asmo.Gfx.Colors.Transparent, Asmo.Gfx.Colors.Transparent
                },
        });
        // Player fields
        int playerX = 150, playerY = 100;
        int playerW = 16, playerH = 16;
        int playerSpeed = 3;
        Asmo.Types.Color playerColor = Asmo.Gfx.Colors.Yellow;
        int score = 0;
        Random rand = new Random();

        // Power-up fields
        int powerUpX = -100, powerUpY = -100;
        int powerUpW = 12, powerUpH = 12;
        bool powerUpActive = false;
        int powerUpTimer = 0;
        int powerUpDuration = 180; // frames (3 seconds at 60fps)
        int powerUpEffectTimer = 0;
        bool powerUpEffectActive = false;
        int normalPlayerSpeed = 3;
        int boostedPlayerSpeed = 6;
        // Timer/game over
        float timeLeft = 30f; // 30 seconds (float, not frames)
        int combo = 0;
        int comboTimer = 0;
        int comboTimeout = 60; // 1 second window

        // Screen shake fields
        int shakeTimer = 0;
        int shakeAmount = 0;

        enum GameState { StartScreen, Playing, GameOver }
        GameState gameState = GameState.StartScreen;
        int highScore = 0;
                WasapiOut soundOut = new WasapiOut(); 
        public Game()
        {
            _player = new ChipTunePlayer();
            _player.BasePitch = 440f; // A4

        }
        public void Init(Asmo.Gfx.Surface surface)
        {
          
            kb = new Keyboard(surface.Window);
            try
            {

                //var vgmSource = new Megadrive.VGMSong(Path.Join(GameEnvironment.AssetRoot, "Assets/Sound/demooldd.vgm"));
                //vgmSource.Play();

                // Create the wave source (already done)
                IWaveSource factory = CodecFactory.Instance.GetCodec(Path.Join(GameEnvironment.AssetRoot, "Assets/Sound/Demo.wav"));

                // Create the output device

                // Initialize with the wave source
                soundOut.Initialize(factory);
                soundOut.Volume = 0.5f;
                // Play the file
                soundOut.Play();


            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing audio: " + ex.Message);
            }
        }
            
        public void Update(double deltaTime)
        {
            if (soundOut.PlaybackState != PlaybackState.Playing)
            {
                soundOut.Play();
            }

            switch (gameState)
            {
                case GameState.StartScreen:
                    if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
                    {
                        score = 0; timeLeft = 30f; combo = 0; comboTimer = 0;
                        playerX = 150; playerY = 100; dvdBoxX = 10; dvdBoxY = 10;
                        powerUpActive = false; powerUpEffectActive = false; playerSpeed = normalPlayerSpeed;
                        shakeTimer = 0; shakeAmount = 0;
                        gameState = GameState.Playing;
                    }
                    return;
                case GameState.GameOver:
                    if (kb.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter))
                    {
                        gameState = GameState.StartScreen;
                    }
                    return;
                case GameState.Playing:
                    break;
            }
            // Timer (decrement by elapsed seconds)
            timeLeft -= (float)deltaTime;
            if (timeLeft <= 0f)
            {
                if (score > highScore) highScore = score;
                gameState = GameState.GameOver;
                timeLeft = 0f;
                return;
            }
            // Player movement
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.A)) playerX -= playerSpeed;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.D)) playerX += playerSpeed;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.W)) playerY -= playerSpeed;
            if (kb.IsKeyDown(OpenTK.Windowing.GraphicsLibraryFramework.Keys.S)) playerY += playerSpeed;
            // Clamp player to screen
            playerX = Math.Clamp(playerX, 0, surfaceWidth - playerW);
            playerY = Math.Clamp(playerY, 0, surfaceHeight - playerH);

            // DVD logo box movement
            dvdBoxX += dvdBoxVX;
            dvdBoxY += dvdBoxVY;
            // Bounce off edges
            if (dvdBoxX <= 0 || dvdBoxX + dvdBoxW >=  surfaceWidth)
                dvdBoxVX = -dvdBoxVX;
            if (dvdBoxY <= 0 || dvdBoxY + dvdBoxH >= surfaceHeight)
                dvdBoxVY = -dvdBoxVY;

            // Power-up spawn logic
            if (!powerUpActive && rand.Next(0, 300) == 0) // ~every 5 seconds
            {
                powerUpX = rand.Next(surfaceWidth - powerUpW);
                powerUpY = rand.Next(surfaceHeight - powerUpH);
                powerUpActive = true;
                powerUpTimer = 180;
            }
            if (powerUpActive)
            {
                powerUpTimer--;
                if (powerUpTimer <= 0)
                {
                    powerUpActive = false;
                    powerUpX = powerUpY = -100;
                }
                // Player collects power-up
                if (playerX < powerUpX + powerUpW && playerX + playerW > powerUpX &&
                    playerY < powerUpY + powerUpH && playerY + playerH > powerUpY)
                {
                    powerUpActive = false;
                    powerUpX = powerUpY = -100;
                    powerUpEffectActive = true;
                    powerUpEffectTimer = powerUpDuration;
                    playerSpeed = boostedPlayerSpeed;
                    _player.PlayBeep(1200f, 0.15f, 0.7f, square);
                    shakeTimer = 10; shakeAmount = 4;
                }
            }
            // Power-up effect duration
            if (powerUpEffectActive)
            {
                powerUpEffectTimer--;
                if (powerUpEffectTimer <= 0)
                {
                    powerUpEffectActive = false;
                    playerSpeed = normalPlayerSpeed;
                }
            }
            // Collision detection
            if (playerX < dvdBoxX + dvdBoxW && playerX + playerW > dvdBoxX &&
                playerY < dvdBoxY + dvdBoxH && playerY + playerH > dvdBoxY)
            {
                dvdBoxColor = new Asmo.Types.Color(rand.Next(256), rand.Next(256), rand.Next(256), 255);
                _player.PlayOneShotFromInstrument(300f + rand.Next(200), 0.13f, 0.7f, noise);
                score++;
                combo++;
                comboTimer = comboTimeout;
                dvdBoxX = rand.Next(surfaceWidth - dvdBoxW);
                dvdBoxY = rand.Next(surfaceHeight - dvdBoxH);
                shakeTimer = 8; shakeAmount = 3;
            }
            // Combo timer
            if (combo > 0)
            {
                comboTimer--;
                if (comboTimer <= 0) combo = 0;
            }
            // Screen shake timer
            if (shakeTimer > 0) shakeTimer--;
            _player.UpdateMixer();
        }
        public void Draw(Asmo.Gfx.Surface surface)
        {
            int shakeX = 0, shakeY = 0;
            if (shakeTimer > 0)
            {
                shakeX = rand.Next(-shakeAmount, shakeAmount + 1);
                shakeY = rand.Next(-shakeAmount, shakeAmount + 1);
            }
            surface.Clear(Asmo.Gfx.Colors.DarkGreen);
            if (gameState == GameState.StartScreen)
            {
                surface.DrawText(surface.Width / 2 - 60, surface.Height / 2 - 30, "ASTRO TEST GAME", Asmo.Gfx.Colors.Yellow);
                surface.DrawText(surface.Width / 2 - 50, surface.Height / 2 - 10, $"High Score: {highScore}", Asmo.Gfx.Colors.Cyan);
                surface.DrawText(surface.Width / 2 - 70, surface.Height / 2 + 10, "Press Enter to Start", Asmo.Gfx.Colors.White);
                return;
            }
            if (gameState == GameState.GameOver)
            {
                surface.DrawText(surface.Width / 2 - 50 + shakeX, surface.Height / 2 - 20 + shakeY, "GAME OVER", Asmo.Gfx.Colors.Red);
                surface.DrawText(surface.Width / 2 - 60 + shakeX, surface.Height / 2 + 0 + shakeY, $"Final Score: {score}", Asmo.Gfx.Colors.Yellow);
                surface.DrawText(surface.Width / 2 - 60 + shakeX, surface.Height / 2 + 20 + shakeY, $"High Score: {highScore}", Asmo.Gfx.Colors.Cyan);
                surface.DrawText(surface.Width / 2 - 70 + shakeX, surface.Height / 2 + 40 + shakeY, "Press Enter for Menu", Asmo.Gfx.Colors.White);
                return;
            }
            // Instructions
            surface.DrawText(10 + shakeX, 40 + shakeY, "Arrow keys: Move | Touch DVD logo! | Grab power-ups!", Asmo.Gfx.Colors.White);
            // Score
            surface.DrawText(10 + shakeX, 30 + shakeY, $"Score: {score}", Asmo.Gfx.Colors.Yellow);
            // Timer
            surface.DrawText(10 + shakeX, 10 + shakeY, $"Time: {MathF.Ceiling(timeLeft)}", Asmo.Gfx.Colors.White);
            // Combo
            if (combo > 1)
                surface.DrawText(10 + shakeX, 65 + shakeY, $"Combo: {combo}x!", Asmo.Gfx.Colors.Cyan);
            // DVD logo box
            surface.DrawRect(dvdBoxX + shakeX, dvdBoxY + shakeY, dvdBoxW, dvdBoxH, dvdBoxColor);
            surface.DrawSprite(disk, dvdBoxX + 5 + shakeX, dvdBoxY + 5 + shakeY);
            surface.DrawText(dvdBoxX + 10 + shakeX, dvdBoxY + dvdBoxH / 2 - 4 + shakeY, "DVD", Asmo.Gfx.Colors.Black);
            // Player
            surface.DrawRect(playerX + shakeX, playerY + shakeY, playerW, playerH, playerColor);
            // Power-up
            if (powerUpActive)
            {
                surface.DrawRect(powerUpX + shakeX, powerUpY + shakeY, powerUpW, powerUpH, Asmo.Gfx.Colors.Magenta);
                surface.DrawText(powerUpX - 2 + shakeX, powerUpY - 10 + shakeY, "P", Asmo.Gfx.Colors.White);
            }
            // Power-up effect indicator
            if (powerUpEffectActive)
            {
                surface.DrawText(10 + shakeX, 50 + shakeY, "Speed Boost!", Asmo.Gfx.Colors.Magenta);
            }
        }
        // Helper to get surface size in Update
        int surfaceWidth => 384;
        int surfaceHeight => 256;
    }
}
