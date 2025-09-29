using System;
using Asmo.Audio;

namespace SceneDemo
{
    internal static class SceneDiagnostics
    {
        public static void Log(string message)
        {
            if (!AudioEngine.DiagnosticsEnabled)
            {
                return;
            }

            var sink = AudioEngine.DiagnosticsSink;
            sink?.Invoke($"[SceneDemo] {DateTime.Now:HH:mm:ss.fff} {message}");
        }
    }
}
