namespace Asmo.Audio
{
    /// <summary>
    /// Handle for controlling a currently playing audio instance.
    /// </summary>
    public sealed class AudioHandle
    {
        private readonly AudioInstance _instance;
        private readonly AudioBus _owningBus;

        internal AudioHandle(AudioInstance instance, AudioBus owningBus)
        {
            _instance = instance;
            _owningBus = owningBus;
        }

        public bool IsPlaying => _instance.IsPlaying;

        public void Stop()
        {
            _instance.Stop();
            _owningBus.RemoveInstance(_instance);
        }

        public void SetVolume(float volume)
        {
            _instance.SetVolume(volume);
        }

        public void SetPan(float pan)
        {
            _instance.SetPan(pan);
        }

        public void FadeTo(float targetVolume, double durationSeconds)
        {
            _instance.FadeTo(targetVolume, durationSeconds);
        }
    }
}
