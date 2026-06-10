// QuestContent.cs — loads and indexes Resources/sparq-content.json (the
// editable quest/achievement/notification copy). Schema (cadence, xp,
// category, etc.) lives in QuestCatalog.cs; *strings* live here.
//
// JSON shape (abbreviated):
// {
//   "_meta": { ... },
//   "quests": {
//     "three_second_pause": {
//        "title": "The 3-Second Pause",
//        "description": "...",
//        "shortLabel": "Pause once today",
//        "onComplete": "...",
//        "streakMessages": { "7": "...", "30": "...", "90": "..." }
//     },
//     ...
//   },
//   "achievements": { "<id>": { "title", "unlockCondition", "unlockCopy" }, ... },
//   "notifications": { "<key>": "copy", ... },
//   "disclaimers":   { "<key>": "copy", ... }
// }
//
// Hot-swap-friendly: call QuestContent.Reload() in dev to pick up edits
// without restarting Play Mode.

using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Sparq.Systems
{
    public static class QuestContent
    {
        // ── JSON-mirroring DTOs ─────────────────────────────────────────
        [System.Serializable]
        public class QuestCopy
        {
            public string title;
            public string description;
            public string shortLabel;
            public string onComplete;
            public Dictionary<string, string> streakMessages;
        }

        [System.Serializable]
        public class AchievementCopy
        {
            public string title;
            public string unlockCondition;
            public string unlockCopy;
        }

        [System.Serializable]
        public class ContentRoot
        {
            public Dictionary<string, object> _meta;
            public Dictionary<string, QuestCopy> quests;
            public Dictionary<string, AchievementCopy> achievements;
            // Values are usually strings but the file intentionally includes
            // a few documentation entries that are arrays (e.g.
            // FORBIDDEN_EXAMPLES_DO_NOT_USE). Use `object` so parsing is
            // tolerant; lookup helpers below filter out non-string values.
            public Dictionary<string, object> notifications;
            public Dictionary<string, object> disclaimers;
        }

        private const string RESOURCE_NAME = "sparq-content";
        private static ContentRoot _content;
        private static bool _loadAttempted;

        // ── Public API ──────────────────────────────────────────────────

        public static string GetTitle(string questId)        => GetQuest(questId)?.title       ?? PrettyFallback(questId);
        public static string GetDescription(string questId)  => GetQuest(questId)?.description ?? "";
        public static string GetShortLabel(string questId)   => GetQuest(questId)?.shortLabel  ?? GetTitle(questId);
        public static string GetOnComplete(string questId)   => GetQuest(questId)?.onComplete  ?? "Nice work.";

        /// <summary>Returns the streak milestone copy if one is defined for
        /// this exact day count, otherwise empty. Caller decides whether
        /// to suppress the default Buddy reaction.</summary>
        public static string GetStreakMessage(string questId, int days)
        {
            var q = GetQuest(questId);
            if (q?.streakMessages == null) return "";
            return q.streakMessages.TryGetValue(days.ToString(), out var s) ? s : "";
        }

        public static string GetAchievementTitle(string achievementId)     => GetAch(achievementId)?.title           ?? PrettyFallback(achievementId);
        public static string GetAchievementUnlockCopy(string achievementId)=> GetAch(achievementId)?.unlockCopy      ?? "";
        public static string GetAchievementCondition(string achievementId) => GetAch(achievementId)?.unlockCondition ?? "";

        public static string GetNotification(string key) => Tryget(_content?.notifications, key);
        public static string GetDisclaimer(string key)   => Tryget(_content?.disclaimers,   key);

        /// <summary>Returns all quest IDs that have copy in the content
        /// file. Useful for auditing — should align 1:1 with QuestCatalog.</summary>
        public static IEnumerable<string> AllQuestIds()
        {
            Load();
            return _content?.quests?.Keys ?? (IEnumerable<string>)System.Array.Empty<string>();
        }

        /// <summary>Force a fresh load from Resources. Dev-only — useful
        /// after hand-editing the JSON.</summary>
        public static void Reload()
        {
            _content = null; _loadAttempted = false; Load();
        }

        // ── Internals ───────────────────────────────────────────────────

        private static QuestCopy GetQuest(string id)
        {
            Load();
            if (_content?.quests == null || string.IsNullOrEmpty(id)) return null;
            return _content.quests.TryGetValue(id, out var q) ? q : null;
        }

        private static AchievementCopy GetAch(string id)
        {
            Load();
            if (_content?.achievements == null || string.IsNullOrEmpty(id)) return null;
            return _content.achievements.TryGetValue(id, out var a) ? a : null;
        }

        // Tolerant lookup — accepts the `object`-valued dictionaries the
        // notifications/disclaimers sections deserialize into. Returns "" if
        // the entry is missing OR isn't a plain string (e.g. array entries
        // like FORBIDDEN_EXAMPLES_DO_NOT_USE are silently skipped).
        private static string Tryget(Dictionary<string, object> d, string k)
        {
            if (d == null || k == null) return "";
            if (!d.TryGetValue(k, out var v) || v == null) return "";
            return v is string s ? s : "";
        }

        private static void Load()
        {
            if (_content != null || _loadAttempted) return;
            _loadAttempted = true;
            try
            {
                var asset = Resources.Load<TextAsset>(RESOURCE_NAME);
                if (asset == null || string.IsNullOrEmpty(asset.text))
                {
                    Debug.LogWarning($"[QuestContent] Resources/{RESOURCE_NAME}.json not found — using id-fallbacks.");
                    return;
                }
                _content = JsonConvert.DeserializeObject<ContentRoot>(asset.text);
                int qs = _content?.quests?.Count ?? 0;
                int ac = _content?.achievements?.Count ?? 0;
                Debug.Log($"[QuestContent] Loaded {qs} quest entries, {ac} achievement entries.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[QuestContent] Failed to load/parse {RESOURCE_NAME}.json: {ex.Message}");
            }
        }

        // "three_second_pause" -> "Three Second Pause" if no JSON match.
        private static string PrettyFallback(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            var parts = id.Split('_');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            return string.Join(" ", parts);
        }
    }
}
