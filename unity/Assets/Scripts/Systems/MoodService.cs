using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Mood / "Spirit Stone" log. Persists to PlayerPrefs as a CSV
    /// of "<unix>:<mood>" entries. Lightweight, no external deps.
    /// </summary>
    public static class MoodService
    {
        public enum Mood { Calm, Focused, Tired, Overwhelmed, Proud }

        // Fantasy-themed crystal colors per mood
        public static readonly (Mood mood, string label, Color color)[] Crystals = new[]
        {
            (Mood.Calm,        "Calm",        new Color(0.55f, 0.85f, 1f)),
            (Mood.Focused,     "Focused",     new Color(1f,    0.85f, 0.35f)),
            (Mood.Tired,       "Weary",       new Color(0.65f, 0.55f, 0.85f)),
            (Mood.Overwhelmed, "Stormy",      new Color(0.85f, 0.40f, 0.45f)),
            (Mood.Proud,       "Triumphant",  new Color(0.55f, 0.85f, 0.45f)),
        };

        private const string KEY = "sparq.mood.log";
        private const int MAX_KEEP = 60; // ~2 months of daily entries

        public struct Entry { public long unix; public Mood mood; }

        public static void Log(Mood mood)
        {
            var list = LoadAll();
            list.Add(new Entry {
                unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                mood = mood
            });
            if (list.Count > MAX_KEEP) list.RemoveRange(0, list.Count - MAX_KEEP);
            Save(list);
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
        }

        /// <summary>
        /// Log (or replace) a mood for a specific calendar day. The new entry
        /// pins the time-of-day to noon UTC so it lands cleanly on that date,
        /// and any prior entries on that same date are removed first.
        /// </summary>
        public static void LogForDay(Mood mood, DateTime day)
        {
            var list = LoadAll();
            var target = day.Date;
            list.RemoveAll(e =>
                DateTimeOffset.FromUnixTimeSeconds(e.unix).UtcDateTime.Date == target);
            // Noon UTC on the target day → safe across timezones
            var stamp = new DateTime(target.Year, target.Month, target.Day, 12, 0, 0, DateTimeKind.Utc);
            list.Add(new Entry {
                unix = new DateTimeOffset(stamp).ToUnixTimeSeconds(),
                mood = mood
            });
            list.Sort((a, b) => a.unix.CompareTo(b.unix));
            if (list.Count > MAX_KEEP) list.RemoveRange(0, list.Count - MAX_KEEP);
            Save(list);
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
        }

        public static List<Entry> LoadAll()
        {
            var list = new List<Entry>();
            string raw = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(raw)) return list;
            foreach (var seg in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var parts = seg.Split(':');
                if (parts.Length != 2) continue;
                if (long.TryParse(parts[0], out long u)
                    && Enum.TryParse(parts[1], out Mood m))
                    list.Add(new Entry { unix = u, mood = m });
            }
            return list;
        }

        private static void Save(List<Entry> list)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(list[i].unix).Append(':').Append(list[i].mood);
            }
            PlayerPrefs.SetString(KEY, sb.ToString());
            PlayerPrefs.Save();
            // Any write invalidates the LoggedToday cache so the next read
            // re-checks. Cheap (just clears a sentinel DateTime).
            InvalidateLoggedTodayCache();
        }

        /// <summary>Days in a row with at least one mood entry.</summary>
        public static int StreakDays()
        {
            var list = LoadAll();
            if (list.Count == 0) return 0;
            var today = DateTime.UtcNow.Date;
            var seen = new HashSet<DateTime>();
            foreach (var e in list)
                seen.Add(DateTimeOffset.FromUnixTimeSeconds(e.unix).UtcDateTime.Date);

            int streak = 0;
            var d = today;
            while (seen.Contains(d)) { streak++; d = d.AddDays(-1); }
            return streak;
        }

        // Cache for LoggedToday — keyed by today's UTC date. Without this,
        // VitalityService.Today() (called multiple times per second by the lobby
        // refresher + AFK popup) would re-LoadAll() + parse the full mood log
        // on every call. Invalidated by Log/LogForDay below.
        private static DateTime _loggedTodayCachedDate;
        private static bool _loggedTodayCachedValue;

        public static bool LoggedToday()
        {
            var today = DateTime.UtcNow.Date;
            if (_loggedTodayCachedDate == today) return _loggedTodayCachedValue;

            bool found = false;
            foreach (var e in LoadAll())
                if (DateTimeOffset.FromUnixTimeSeconds(e.unix).UtcDateTime.Date == today)
                { found = true; break; }

            _loggedTodayCachedDate = today;
            _loggedTodayCachedValue = found;
            return found;
        }

        /// <summary>Invalidate the LoggedToday cache — call after writing a new entry.</summary>
        public static void InvalidateLoggedTodayCache()
        {
            _loggedTodayCachedDate = default;
        }
    }
}
