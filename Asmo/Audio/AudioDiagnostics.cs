using System;

namespace Asmo.Audio
{
    internal static class AudioDiagnostics
    {
        private static readonly object SyncRoot = new();
        private static Action<string> _sink = message => Console.WriteLine(message);
        private static bool _enabled;

        static AudioDiagnostics()
        {
            var env = Environment.GetEnvironmentVariable("ASMO_AUDIO_LOG");
            if (string.IsNullOrWhiteSpace(env))
            {
                _enabled = false;
            }
            else
            {
                _enabled = env.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                           env.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           env.Equals("yes", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static bool Enabled
        {
            get
            {
                lock (SyncRoot)
                {
                    return _enabled;
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _enabled = value;
                }
            }
        }

        public static Action<string> LogSink
        {
            get
            {
                lock (SyncRoot)
                {
                    return _sink;
                }
            }
            set
            {
                lock (SyncRoot)
                {
                    _sink = value ?? (_ => { });
                }
            }
        }

        public static void Log(string message)
        {
            Action<string> sink;
            bool enabled;
            lock (SyncRoot)
            {
                enabled = _enabled;
                sink = _sink;
            }

            if (!enabled)
                return;

            sink?.Invoke($"[Audio {DateTime.Now:HH:mm:ss.fff}] {message}");
        }
    }
}
