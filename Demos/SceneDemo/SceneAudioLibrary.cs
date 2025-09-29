using Asmo.Audio;

namespace SceneDemo
{
    internal sealed class SceneAudioLibrary
    {
        public SceneAudioLibrary()
        {
            MenuForward = AudioClip.CreateSquare(880, 0.12, 0.35f);
            MenuBack = AudioClip.CreateSquare(440, 0.1, 0.3f);
            PauseToggle = AudioClip.CreateNoise(0.15, 0.18f);
            AmbientLoop = AudioClip.CreateSine(220, 3.2, 0.18f);
            Bounce = AudioClip.CreateSquare(660, 0.08, 0.28f);
            Collect = AudioClip.CreateSquare(1320, 0.1, 0.4f);
        }

        public AudioClip MenuForward { get; }
        public AudioClip MenuBack { get; }
        public AudioClip PauseToggle { get; }
        public AudioClip AmbientLoop { get; }
        public AudioClip Bounce { get; }
        public AudioClip Collect { get; }
    }
}
