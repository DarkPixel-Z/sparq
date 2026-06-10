using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// Stores user-submitted reports and auto-flagged content for moderator
    /// review. Local-first: writes to disk. When we have a backend, this
    /// becomes the queue that syncs to the moderator dashboard.
    /// </summary>
    public static class ModerationQueue
    {
        public enum ReportReason
        {
            Harassment,
            Predator,
            Scam,
            DrugSlang,
            SelfHarm,
            Spam,
            Other,
        }

        [System.Serializable]
        public class Report
        {
            public long unixTime;
            public string reporterId;     // current user
            public string reportedPlayer; // target
            public string messageText;
            public ReportReason reason;
            public string notes;
            public bool autoFlagged;      // true if from ContentModerator block, false if user-submitted
        }

        [System.Serializable]
        private class ReportFile { public List<Report> reports = new List<Report>(); }

        private static string PathOnDisk =>
            System.IO.Path.Combine(Application.persistentDataPath, "sparq_moderation_queue.json");

        private static ReportFile _cache;

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>User-submitted report from the chat UI.</summary>
        public static void Submit(string reportedPlayer, string messageText,
                                  ReportReason reason, string notes = "")
        {
            EnsureLoaded();
            var r = new Report
            {
                unixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                reporterId = CurrentUser(),
                reportedPlayer = reportedPlayer ?? "",
                messageText = messageText ?? "",
                reason = reason,
                notes = notes ?? "",
                autoFlagged = false,
            };
            _cache.reports.Add(r);
            SaveDisk();
            Debug.Log($"[ModerationQueue] Report submitted: target='{reportedPlayer}' reason={reason}");
        }

        /// <summary>Auto-flagged report from ContentModerator (for moderator review).</summary>
        public static void AutoFlag(string reportedPlayer, string messageText,
                                    string reasonsCsv)
        {
            EnsureLoaded();
            var r = new Report
            {
                unixTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                reporterId = "system",
                reportedPlayer = reportedPlayer ?? "",
                messageText = messageText ?? "",
                reason = ReportReason.Other,
                notes = "Auto-flag: " + reasonsCsv,
                autoFlagged = true,
            };
            _cache.reports.Add(r);
            SaveDisk();
        }

        public static List<Report> All()
        {
            EnsureLoaded();
            return new List<Report>(_cache.reports);
        }

        public static int Count
        {
            get { EnsureLoaded(); return _cache.reports.Count; }
        }

        public static void ClearAll()
        {
            EnsureLoaded();
            _cache.reports.Clear();
            SaveDisk();
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNAL
        // ─────────────────────────────────────────────────────────────────

        private static void EnsureLoaded()
        {
            if (_cache != null) return;
            _cache = new ReportFile();
            try
            {
                if (System.IO.File.Exists(PathOnDisk))
                {
                    string json = System.IO.File.ReadAllText(PathOnDisk);
                    _cache = JsonUtility.FromJson<ReportFile>(json) ?? new ReportFile();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ModerationQueue] Load failed: {ex.Message}");
            }
        }

        private static void SaveDisk()
        {
            try
            {
                string json = JsonUtility.ToJson(_cache);
                System.IO.File.WriteAllText(PathOnDisk, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ModerationQueue] Save failed: {ex.Message}");
            }
        }

        private static string CurrentUser()
        {
            try { var d = Sparq.Core.SaveService.Data; return d?.playerName ?? "anonymous"; }
            catch { return "anonymous"; }
        }
    }
}
