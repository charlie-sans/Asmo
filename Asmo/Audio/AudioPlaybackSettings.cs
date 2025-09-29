namespace Asmo.Audio
{
    public struct AudioPlaybackSettings
    {
        public bool Loop { get; set; }
        public float Volume { get; set; }
        public float Pan { get; set; }
        public double FadeInSeconds { get; set; }

        public static AudioPlaybackSettings Default => new()
        {
            Loop = false,
            Volume = 1f,
            Pan = 0f,
            FadeInSeconds = 0
        };
    }
}
