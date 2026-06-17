using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// Rate limiter for chat / safety violations.
    ///
    /// Two layers:
    ///   1) Per-second send throttle (no spamming)
    ///   2) Strike system: N blocked messages within window → temp mute
    ///
    /// Mute durations escalate with offenses:
    ///   1st mute: 1 minute
    ///   2nd: 5 minutes
    ///   3rd: 30 minutes
    ///   4th+: 24 hours
    ///
    /// State persists across sessions via PlayerPrefs.
    /// </summary>
    public static class RateLimiter
    {
        // Tunables
        private const int    STRIKE_THRESHOLD = 3;       // blocks in window → mute
        private const float  STRIKE_WINDOW_SEC = 60f;    // 60-second rolling window
        private const float  MIN_SEND_INTERVAL = 0.8f;   // anti-spam: at most 1 msg / 800ms

        // PlayerPrefs keys
        private const string KEY_OFFENSE_COUNT = "sparq.safety.offenses";
        private const string KEY_MUTE_UNTIL   = "sparq.safety.mute_until";
        private const string KEY_STRIKE_LIST  = "sparq.safety.strikes";   // CSV of unix timestamps

        // Runtime state
        private static float _lastSendTime = -999f;
        private static List<long> _strikes;   // unix timestamps of recent blocked messages

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>True if the user is currently muted.</summary>
        public static bool IsMuted() => RemainingMuteSec() > 0;

        public static int RemainingMuteSec()
        {
            long until = (long)System.Convert.ToDouble(PlayerPrefs.GetString(KEY_MUTE_UNTIL, "0"));
            long now = NowUnix();
            return until > now ? (int)(until - now) : 0;
        }

        /// <summary>
        /// Tester-mode wipe — clears any persisted mute and strike history.
        /// During the closed-test round, the moderation pipeline is brand
        /// new; testers were getting silently muted by typing threat or
        /// self-harm patterns while exercising the safety surfaces and then
        /// couldn't send anything else. Wire this in panel openers to reset
        /// at the start of every chat session. Remove the call site once
        /// testing is over.
        /// </summary>
        public static void ClearAllRestrictions()
        {
            PlayerPrefs.DeleteKey(KEY_MUTE_UNTIL);
            PlayerPrefs.DeleteKey(KEY_OFFENSE_COUNT);
            PlayerPrefs.DeleteKey(KEY_STRIKE_LIST);
            PlayerPrefs.Save();
            _lastSendTime = -999f;
            _strikes = null;
            Debug.Log("[RateLimiter] All restrictions cleared (tester mode).");
        }

        /// <summary>Check if message can be sent. Returns reason if rejected.</summary>
        public static bool CanSend(out string reason)
        {
            if (IsMuted())
            {
                int secs = RemainingMuteSec();
                reason = $"You're muted for {FormatDuration(secs)}.";
                return false;
            }
            if (Time.unscaledTime - _lastSendTime < MIN_SEND_INTERVAL)
            {
                reason = "Slow down — wait a moment between messages.";
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>Record a successful send (resets the per-message timer).</summary>
        public static void RecordSend()
        {
            _lastSendTime = Time.unscaledTime;
        }

        /// <summary>Record a BLOCKED message. Adds a strike; may trigger mute.</summary>
        public static void RecordViolation()
        {
            LoadStrikes();
            long now = NowUnix();
            _strikes.Add(now);
            // Prune to window
            _strikes.RemoveAll(t => now - t > STRIKE_WINDOW_SEC);
            SaveStrikes();

            if (_strikes.Count >= STRIKE_THRESHOLD)
            {
                ApplyMute();
                _strikes.Clear();
                SaveStrikes();
            }
        }

        /// <summary>Manual unmute (e.g., admin/parent action).</summary>
        public static void Unmute()
        {
            PlayerPrefs.SetString(KEY_MUTE_UNTIL, "0");
            PlayerPrefs.Save();
            Debug.Log("[RateLimiter] Manually unmuted.");
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNAL
        // ─────────────────────────────────────────────────────────────────

        private static void ApplyMute()
        {
            int offenseCount = PlayerPrefs.GetInt(KEY_OFFENSE_COUNT, 0) + 1;
            PlayerPrefs.SetInt(KEY_OFFENSE_COUNT, offenseCount);

            int muteSec = offenseCount switch
            {
                1 => 60,             // 1 min
                2 => 300,            // 5 min
                3 => 1800,           // 30 min
                _ => 86400,          // 24 hours
            };
            long until = NowUnix() + muteSec;
            PlayerPrefs.SetString(KEY_MUTE_UNTIL, until.ToString());
            PlayerPrefs.Save();

            Debug.LogWarning($"[RateLimiter] User muted for {FormatDuration(muteSec)} (offense #{offenseCount}).");
        }

        private static void LoadStrikes()
        {
            if (_strikes != null) return;
            _strikes = new List<long>();
            string csv = PlayerPrefs.GetString(KEY_STRIKE_LIST, "");
            if (string.IsNullOrEmpty(csv)) return;
            foreach (var s in csv.Split(','))
                if (long.TryParse(s, out long t)) _strikes.Add(t);
        }
        private static void SaveStrikes()
        {
            PlayerPrefs.SetString(KEY_STRIKE_LIST,
                string.Join(",", _strikes));
            PlayerPrefs.Save();
        }

        private static long NowUnix() =>
            System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private static string FormatDuration(int sec)
        {
            if (sec < 60) return $"{sec}s";
            if (sec < 3600) return $"{sec / 60}m";
            return $"{sec / 3600}h";
        }
    }
}
