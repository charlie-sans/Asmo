using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Asmo.Window
{
    internal class GameHistory
    {
        private const int MaxEntries = 12;
        private static readonly string HistoryDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Asmo");
        private static readonly string HistoryPath = Path.Combine(HistoryDir, "recent_games.json");

        public class Entry
        {
            public string Path { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public DateTime LastPlayed { get; set; }
        }

        private static readonly List<Entry> _entries = new();
        private static bool _dirty = false;
        private static DateTime _lastSave = DateTime.MinValue;
        private static readonly TimeSpan SaveThrottle = TimeSpan.FromSeconds(2);

        public static IReadOnlyList<Entry> Entries => _entries;

        public static void Load()
        {
            try
            {
                Directory.CreateDirectory(HistoryDir);
                if (File.Exists(HistoryPath))
                {
                    var json = File.ReadAllText(HistoryPath);
                    var data = JsonSerializer.Deserialize<List<Entry>>(json);
                    if (data != null)
                    {
                        _entries.Clear();
                        // Filter missing files, order by last played desc
                        foreach (var e in data.Where(e => !string.IsNullOrWhiteSpace(e.Path)).OrderByDescending(e => e.LastPlayed))
                        {
                            if (File.Exists(e.Path) || Directory.Exists(System.IO.Path.GetDirectoryName(e.Path)))
                                _entries.Add(e);
                        }
                    }
                }
            }
            catch { /* ignore corrupt file */ }
        }

        public static void AddOrUpdate(string path, string displayName)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var existing = _entries.FirstOrDefault(e => string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.LastPlayed = DateTime.UtcNow;
                existing.DisplayName = displayName;
            }
            else
            {
                _entries.Add(new Entry
                {
                    Path = path,
                    DisplayName = displayName,
                    LastPlayed = DateTime.UtcNow
                });
            }
            // Reorder & trim
            var ordered = _entries.OrderByDescending(e => e.LastPlayed).Take(MaxEntries).ToList();
            _entries.Clear();
            _entries.AddRange(ordered);
            _dirty = true;
            TrySave();
        }

        public static void Clear()
        {
            _entries.Clear();
            _dirty = true;
            TrySave(true);
        }

        public static void Tick()
        {
            if (_dirty) TrySave();
        }

        private static void TrySave(bool force = false)
        {
            if (!_dirty && !force) return;
            if (!force && DateTime.UtcNow - _lastSave < SaveThrottle) return;
            try
            {
                Directory.CreateDirectory(HistoryDir);
                var json = JsonSerializer.Serialize(_entries);
                File.WriteAllText(HistoryPath, json);
                _dirty = false;
                _lastSave = DateTime.UtcNow;
            }
            catch { /* ignore */ }
        }
    }
}
