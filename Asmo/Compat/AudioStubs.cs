// CSCore removed; stubs file no longer necessary but kept in case of build flags.
#if USE_AUDIO_STUBS
// Temporary audio stubs (compile only if USE_AUDIO_STUBS defined)
namespace Asmo.Audio
{
    public sealed class AudioEngine { public void Update(double dt) {} public AudioBus Master => new AudioBus(); public AudioBus GetOrCreateBus(string name, AudioBus? parent = null) => new AudioBus(); public AudioHandle PlayClip(AudioClip clip, string busName="master", AudioPlaybackSettings? settings=null) => new AudioHandle(); }
    public sealed class AudioBus { public AudioHandle Play(AudioClip clip, AudioPlaybackSettings? settings=null) => new AudioHandle(); public AudioBus CreateChildBus(string name)=> new AudioBus(); public float Volume { get; set; } }
    public sealed class AudioClip { public static AudioClip Load(string path)=> new AudioClip(); }
    public sealed class AudioHandle { public bool IsPlaying => false; public void Stop() {} }
    public sealed class AudioPlaybackSettings { public bool Loop; public float Volume = 1f; public float Pan = 0f; public static AudioPlaybackSettings Default => new(); }
}
namespace Asmo.Sound { public delegate object Instrument(float f, float d, float a=0.2f); }
#endif