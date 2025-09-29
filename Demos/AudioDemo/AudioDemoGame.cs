using Asmo;
using Asmo.Audio;
using Asmo.Gfx;
using Asmo.Types;
using Asmo.Window.input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AudioDemo
{
    /// <summary>
    /// Interactive showcase for the shared audio engine. Demonstrates buses, looping clips, fades, and pan control.
    /// </summary>
    public sealed class AudioShowcaseGame : IConsoleGame
    {
        private Keyboard? _keyboard;
        private AudioEngine? _audio;
        private AudioBus? _musicBus;
        private AudioBus? _sfxBus;
        private AudioBus? _uiBus;
        private AudioHandle? _ambientHandle;
        private AudioHandle? _pulseHandle;
        private AudioClip? _ambientClip;
        private AudioClip? _pulseClip;
        private AudioClip[] _sfxClips = Array.Empty<AudioClip>();
        private AudioClip? _uiClip;
        private readonly Random _random = new();
        private readonly List<PendingStop> _pendingStops = new();
        private string _lastEvent = "Press Space to trigger a sound effect.";
        private float _sfxPan;
        private double _time;

        public void Init(Surface surface)
        {
            
            _keyboard = new Keyboard(surface.Window);
            _audio = new AudioEngine();
            AudioEngine.DiagnosticsEnabled = true;
            var logPath = Path.Combine(AppContext.BaseDirectory, "audio.log");
            var logEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            AudioEngine.DiagnosticsSink = msg =>
            {
                Console.WriteLine(msg);
                File.AppendAllText(logPath, msg + Environment.NewLine, logEncoding);
            };
            _musicBus = _audio.GetOrCreateBus("music");
            _musicBus.Volume = 0.45f;
            _sfxBus = _audio.GetOrCreateBus("sfx");
            _sfxBus.Volume = 0.85f;
            _uiBus = _audio.GetOrCreateBus("ui");
            _uiBus.Volume = 0.6f;
var testClip = AudioClip.CreateSine(440, 5.0, 0.3f);
_audio.MasterBus.Play(testClip, new AudioPlaybackSettings { Volume = 0.6f, FadeInSeconds = 0 });
            BuildClips();
        }

        public void Update(double deltaTime)
        {
            _audio?.Update(deltaTime);
            _time += deltaTime;
            ProcessPendingStops(deltaTime);

            if (_pulseHandle != null && _pulseHandle.IsPlaying)
            {
                float sweep = (float)Math.Sin(_time * 0.75) * 0.6f;
                _pulseHandle.SetPan(sweep);
            }

            if (_keyboard == null)
            {
                return;
            }

            HandleToggleInputs();
            HandleContinuousInputs(deltaTime);

            _keyboard.Update();
        }

        public void Draw(Surface surface)
        {
            surface.Clear(Colors.DarkBlue);

            surface.DrawText(20, 20, "Audio Engine Demo", Colors.Yellow);
            surface.DrawText(20, 40, "1 - Toggle ambient loop (music bus)", Colors.White);
            surface.DrawText(20, 52, "2 - Toggle pulse loop with auto-pan", Colors.White);
            surface.DrawText(20, 64, "Space - Randomized one-shot SFX", Colors.White);
            surface.DrawText(20, 76, "Backspace - Fade everything out", Colors.White);

            surface.DrawText(20, 100, $"Music volume [H/J]: {FormatBusVolume(_musicBus)}", Colors.Cyan);
            surface.DrawText(20, 112, $"SFX volume [N/M]: {FormatBusVolume(_sfxBus)}", Colors.Cyan);
            surface.DrawText(20, 124, $"Master volume [Up/Down]: {FormatBusVolume(_audio?.MasterBus)}", Colors.Cyan);
            surface.DrawText(20, 136, $"SFX pan [Left/Right]: {_sfxPan:+0.00;-0.00}", Colors.Cyan);

            surface.DrawText(20, 160, _lastEvent, Colors.Yellow);

            DrawVolumeBar(surface, 20, 180, "Music", _musicBus?.Volume ?? 0f, Colors.Magenta);
            DrawVolumeBar(surface, 20, 200, "SFX", _sfxBus?.Volume ?? 0f, Colors.Orange);
            DrawVolumeBar(surface, 20, 220, "Master", _audio?.MasterBus.Volume ?? 0f, Colors.Green);

            string ambientState = IsPlaying(_ambientHandle) ? "ON" : "off";
            string pulseState = IsPlaying(_pulseHandle) ? "ON" : "off";
            surface.DrawText(20, 248, $"Ambient loop: {ambientState}", Colors.White);
            surface.DrawText(20, 260, $"Pulse loop: {pulseState}", Colors.White);
        }

        private void BuildClips()
        {
            _ambientClip = AudioClip.CreateSine(220, 6.0, 0.18f);
            _pulseClip = AudioClip.CreateSquare(110, 0.5, 0.3f);
            _sfxClips = new[]
            {
                AudioClip.CreateSquare(880, 0.18, 0.35f),
                AudioClip.CreateSine(1320, 0.3, 0.3f),
                AudioClip.CreateNoise(0.22, 0.28f)
            };
            _uiClip = AudioClip.CreateSquare(1000, 0.1, 0.22f);
        }

        private void HandleToggleInputs()
        {
            if (_keyboard == null)
            {
                return;
            }

            if (_keyboard.IsKeyPressed(Keys.D1))
            {
                if (!IsPlaying(_ambientHandle))
                {
                    if (_ambientClip != null && _musicBus != null)
                    {
                        _ambientHandle = _musicBus.Play(_ambientClip, new AudioPlaybackSettings
                        {
                            Loop = true,
                            Volume = 0.5f,
                            FadeInSeconds = 1.0
                        });
                        PlayUiClick();
                        _lastEvent = "Ambient loop started";
                    }
                }
                else
                {
                    QueueStop(ref _ambientHandle, 0.6);
                    PlayUiClick();
                    _lastEvent = "Ambient loop fading out";
                }
            }

            if (_keyboard.IsKeyPressed(Keys.D2))
            {
                if (!IsPlaying(_pulseHandle))
                {
                    if (_pulseClip != null && _musicBus != null)
                    {
                        _pulseHandle = _musicBus.Play(_pulseClip, new AudioPlaybackSettings
                        {
                            Loop = true,
                            Volume = 0.45f,
                            FadeInSeconds = 0.3f
                        });
                        PlayUiClick();
                        _lastEvent = "Pulse loop started";
                    }
                }
                else
                {
                    QueueStop(ref _pulseHandle, 0.35);
                    PlayUiClick();
                    _lastEvent = "Pulse loop fading out";
                }
            }

            if (_keyboard.IsKeyPressed(Keys.Space))
            {
                TriggerSfxShot();
            }

            if (_keyboard.IsKeyPressed(Keys.Backspace))
            {
                StopAll();
                _lastEvent = "All sounds stopped";
            }
        }

        private void HandleContinuousInputs(double deltaTime)
        {
            if (_keyboard == null || _audio == null)
            {
                return;
            }

            float step = (float)(deltaTime * 0.8f);

            if (_keyboard.IsKeyDown(Keys.H))
            {
                AdjustBusVolume(_musicBus, -step);
            }

            if (_keyboard.IsKeyDown(Keys.J))
            {
                AdjustBusVolume(_musicBus, step);
            }

            if (_keyboard.IsKeyDown(Keys.N))
            {
                AdjustBusVolume(_sfxBus, -step);
            }

            if (_keyboard.IsKeyDown(Keys.M))
            {
                AdjustBusVolume(_sfxBus, step);
            }

            if (_keyboard.IsKeyDown(Keys.Down))
            {
                AdjustBusVolume(_audio.MasterBus, -step);
            }

            if (_keyboard.IsKeyDown(Keys.Up))
            {
                AdjustBusVolume(_audio.MasterBus, step);
            }

            if (_keyboard.IsKeyDown(Keys.Left))
            {
                _sfxPan = Math.Clamp(_sfxPan - step * 2f, -1f, 1f);
            }

            if (_keyboard.IsKeyDown(Keys.Right))
            {
                _sfxPan = Math.Clamp(_sfxPan + step * 2f, -1f, 1f);
            }
        }

        private void TriggerSfxShot()
        {
            if (_sfxBus == null || _sfxClips.Length == 0)
            {
                return;
            }

            var clip = _sfxClips[_random.Next(_sfxClips.Length)];
            float pan = Math.Clamp(_sfxPan + (float)(_random.NextDouble() - 0.5) * 0.4f, -1f, 1f);
            float volume = Math.Clamp(0.65f + (float)_random.NextDouble() * 0.35f, 0f, 1.4f);

            _sfxBus.Play(clip, new AudioPlaybackSettings
            {
                Volume = volume,
                Pan = pan,
                FadeInSeconds = 0.01
            });

            _lastEvent = $"SFX fired (vol {volume:0.00}, pan {pan:+0.00;-0.00})";
        }

        private void StopAll()
        {
            QueueStop(ref _ambientHandle, 0.4);
            QueueStop(ref _pulseHandle, 0.3);
            _sfxBus?.StopAll();
        }

        private void PlayUiClick()
        {
            if (_uiBus == null || _uiClip == null)
            {
                return;
            }

            _uiBus.Play(_uiClip, new AudioPlaybackSettings
            {
                Volume = 0.45f,
                FadeInSeconds = 0.01
            });
        }

        private void QueueStop(ref AudioHandle? handle, double fadeSeconds)
        {
            if (handle != null && handle.IsPlaying)
            {
                handle.FadeTo(0f, fadeSeconds);
                _pendingStops.Add(new PendingStop(handle, fadeSeconds + 0.1));
            }

            handle = null;
        }

        private void ProcessPendingStops(double deltaTime)
        {
            for (int i = _pendingStops.Count - 1; i >= 0; i--)
            {
                var pending = _pendingStops[i];
                pending.Remaining -= deltaTime;
                if (pending.Remaining <= 0)
                {
                    pending.Handle.Stop();
                    _pendingStops.RemoveAt(i);
                }
            }
        }

        private static bool IsPlaying(AudioHandle? handle) => handle != null && handle.IsPlaying;

        private static string FormatBusVolume(AudioBus? bus)
        {
            return bus == null ? "--" : bus.Volume.ToString("0.00");
        }

        private static void AdjustBusVolume(AudioBus? bus, float delta)
        {
            if (bus == null)
            {
                return;
            }

            bus.Volume = Math.Clamp(bus.Volume + delta, 0f, 2f);
        }

        private static void DrawVolumeBar(Surface surface, int x, int y, string label, float value, Color color)
        {
            surface.DrawText(x, y - 10, label, Colors.White);
            int width = 200;
            int height = 8;
            surface.DrawRect(x, y, width, height, Colors.DarkGray);
            int fill = (int)(width * Math.Clamp(value, 0f, 1f));
            if (fill > 0)
            {
                surface.DrawRect(x, y, fill, height, color);
            }
        }

        private sealed class PendingStop
        {
            public PendingStop(AudioHandle handle, double remaining)
            {
                Handle = handle;
                Remaining = remaining;
            }

            public AudioHandle Handle { get; }
            public double Remaining { get; set; }
        }
    }
}
