using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Reminders persistence. Stores a list of reminders in PlayerPrefs as a
    /// pipe-delimited string. Lightweight, no scheduling — just data + queries.
    /// (Real OS notifications would be wired via Unity Notifications later.)
    /// </summary>
    public static class RemindService
    {
        [Serializable]
        public class Reminder
        {
            public string id;
            public string title;
            public int    hour;     // 0-23
            public int    minute;   // 0-59
            public string days;     // 7 chars MTWTFSS, '1' = on, '0' = off
            public bool   enabled;
        }

        private const string KEY = "sparq.reminders.v1";
        // Tracks which reminders have already alerted today so we don't
        // double-fire. Format: "yyyy-MM-dd:id|yyyy-MM-dd:id|...". Resets
        // implicitly each calendar day.
        private const string FIRED_KEY = "sparq.reminders.fired.v1";
        public static event Action OnChanged;
        /// <summary>Fires when a reminder's scheduled time arrives.</summary>
        public static event Action<Reminder> OnFired;

        private static List<Reminder> _cache;
        private static HashSet<string> _firedToday;
        private static string _firedDate;

        public static List<Reminder> All()
        {
            if (_cache != null) return _cache;
            _cache = new List<Reminder>();
            string raw = PlayerPrefs.GetString(KEY, "");
            if (string.IsNullOrEmpty(raw)) { Seed(); return _cache; }

            foreach (var seg in raw.Split('|'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var p = seg.Split(';');
                if (p.Length < 6) continue;
                _cache.Add(new Reminder
                {
                    id      = p[0],
                    title   = p[1],
                    hour    = int.TryParse(p[2], out int h) ? h : 9,
                    minute  = int.TryParse(p[3], out int m) ? m : 0,
                    days    = p[4],
                    enabled = p[5] == "1",
                });
            }
            return _cache;
        }

        public static void Add(string title, int hour, int minute, string days = "1111111")
        {
            All();
            _cache.Add(new Reminder
            {
                id = Guid.NewGuid().ToString("N").Substring(0, 8),
                title = title,
                hour = Mathf.Clamp(hour, 0, 23),
                minute = Mathf.Clamp(minute, 0, 59),
                days = days,
                enabled = true,
            });
            Save();
            OnChanged?.Invoke();
        }

        public static void Toggle(string id)
        {
            All();
            foreach (var r in _cache) if (r.id == id) { r.enabled = !r.enabled; break; }
            Save();
            OnChanged?.Invoke();
        }

        public static void Delete(string id)
        {
            All();
            _cache.RemoveAll(r => r.id == id);
            Save();
            OnChanged?.Invoke();
        }

        public static List<Reminder> Today()
        {
            int dayIdx = ((int)DateTime.Now.DayOfWeek + 6) % 7; // Mon=0..Sun=6
            var result = new List<Reminder>();
            foreach (var r in All())
            {
                if (!r.enabled) continue;
                if (string.IsNullOrEmpty(r.days) || r.days.Length < 7) continue;
                if (r.days[dayIdx] == '1') result.Add(r);
            }
            result.Sort((a, b) => (a.hour * 60 + a.minute).CompareTo(b.hour * 60 + b.minute));
            return result;
        }

        private static void Save()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _cache.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var r = _cache[i];
                sb.Append(r.id).Append(';')
                  .Append(r.title?.Replace(';', ',').Replace('|', '/')).Append(';')
                  .Append(r.hour).Append(';').Append(r.minute).Append(';')
                  .Append(r.days).Append(';').Append(r.enabled ? '1' : '0');
            }
            PlayerPrefs.SetString(KEY, sb.ToString());
            PlayerPrefs.Save();
        }

        // First-launch sample reminders so the panel isn't empty
        private static void Seed()
        {
            Add("Take morning meds",    8,  0,  "1111100");
            Add("Drink water",         11, 30,  "1111111");
            Add("Stand & stretch",     14,  0,  "1111100");
            Add("Wind down for bed",   22,  0,  "1111111");
        }

        // ── Fire tracking — which reminders have already alerted today ──

        private static void EnsureFiredLoaded()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (_firedToday != null && _firedDate == today) return;
            _firedToday = new HashSet<string>();
            _firedDate = today;
            string raw = PlayerPrefs.GetString(FIRED_KEY, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var seg in raw.Split('|'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                int colon = seg.IndexOf(':');
                if (colon <= 0 || colon >= seg.Length - 1) continue;
                if (seg.Substring(0, colon) == today)
                    _firedToday.Add(seg.Substring(colon + 1));
            }
        }

        public static bool HasFiredToday(string id)
        {
            EnsureFiredLoaded();
            return id != null && _firedToday.Contains(id);
        }

        public static void MarkFiredToday(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            EnsureFiredLoaded();
            if (!_firedToday.Add(id)) return;
            // Rewrite with today's entries only (drops yesterday's).
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var fid in _firedToday)
            {
                if (!first) sb.Append('|');
                sb.Append(_firedDate).Append(':').Append(fid);
                first = false;
            }
            PlayerPrefs.SetString(FIRED_KEY, sb.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Returns reminders whose scheduled time has passed today
        /// (in local time) and which haven't fired yet today. Caller is
        /// expected to MarkFiredToday(id) after presenting the alert so it
        /// doesn't repeat on the next tick.</summary>
        public static List<Reminder> DueNow()
        {
            var now = DateTime.Now;
            int dayIdx = ((int)now.DayOfWeek + 6) % 7;   // Mon=0..Sun=6
            int nowMin = now.Hour * 60 + now.Minute;
            var result = new List<Reminder>();
            foreach (var r in All())
            {
                if (r == null || !r.enabled) continue;
                if (string.IsNullOrEmpty(r.days) || r.days.Length < 7) continue;
                if (r.days[dayIdx] != '1') continue;
                int targetMin = r.hour * 60 + r.minute;
                if (nowMin < targetMin) continue;
                if (HasFiredToday(r.id)) continue;
                result.Add(r);
            }
            // Earliest-due first so the user gets them in order.
            result.Sort((a, b) => (a.hour * 60 + a.minute).CompareTo(b.hour * 60 + b.minute));
            return result;
        }

        /// <summary>Convenience wrapper — marks fired AND fires OnFired so
        /// any UI overlay can render the alert.</summary>
        public static void Fire(Reminder r)
        {
            if (r == null) return;
            MarkFiredToday(r.id);
            OnFired?.Invoke(r);
        }

        /// <summary>
        /// Silently marks every enabled reminder whose scheduled time already
        /// passed today by more than <paramref name="graceMinutes"/> as
        /// fired-today, WITHOUT raising OnFired. Call once at app launch so the
        /// player isn't flooded with a backlog of missed reminders the moment
        /// they log in — only reminders that arrive while the app is open (or
        /// within the small grace window) will actually alert.
        /// </summary>
        public static void SuppressOverdueBacklog(int graceMinutes)
        {
            var now = DateTime.Now;
            int dayIdx = ((int)now.DayOfWeek + 6) % 7;   // Mon=0..Sun=6
            int cutoff = now.Hour * 60 + now.Minute - Mathf.Max(0, graceMinutes);
            foreach (var r in All())
            {
                if (r == null || !r.enabled) continue;
                if (string.IsNullOrEmpty(r.days) || r.days.Length < 7) continue;
                if (r.days[dayIdx] != '1') continue;
                int targetMin = r.hour * 60 + r.minute;
                // Older than the grace window → suppress silently so it
                // won't surface on this login (or later today).
                if (targetMin <= cutoff && !HasFiredToday(r.id))
                    MarkFiredToday(r.id);
            }
        }

        public static string DayBitsToShort(string days)
        {
            // "1111100" → "Mon-Fri", "1111111" → "Daily", others list initials
            if (string.IsNullOrEmpty(days)) return "";
            if (days == "1111111") return "Daily";
            if (days == "1111100") return "Weekdays";
            if (days == "0000011") return "Weekends";
            string[] init = { "M", "T", "W", "T", "F", "S", "S" };
            var picked = new List<string>();
            for (int i = 0; i < 7 && i < days.Length; i++) if (days[i] == '1') picked.Add(init[i]);
            return string.Join(" ", picked);
        }

        public static string FormatTime(int hour, int minute)
        {
            int h12 = hour % 12; if (h12 == 0) h12 = 12;
            string suf = hour < 12 ? "am" : "pm";
            return $"{h12}:{minute:D2} {suf}";
        }
    }
}
