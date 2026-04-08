using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Sparq.Systems
{
    [Serializable]
    public class JournalEntry
    {
        public string id;
        public string dateISO;
        public string moodEmoji;
        public string text;
        public int    xpAwarded;
    }

    /// <summary>
    /// Handles daily journal entries. Awards XP once per day for writing.
    /// </summary>
    public class JournalSystem : MonoBehaviour
    {
        public static JournalSystem Instance { get; private set; }

        public UnityEvent<JournalEntry> OnEntrySaved;

        private List<JournalEntry> _entries = new();
        private const int    XP_PER_ENTRY = 10;
        private const string SAVE_KEY     = "sparq_journal";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        public IReadOnlyList<JournalEntry> Entries => _entries.AsReadOnly();

        /// <summary>Save a new journal entry. Returns false if already written today.</summary>
        public bool SaveEntry(string text, string moodEmoji)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            string todayISO = DateTime.Today.ToString("yyyy-MM-dd");
            bool alreadyWroteToday = _entries.Exists(e => e.dateISO == todayISO);

            var entry = new JournalEntry {
                id         = Guid.NewGuid().ToString(),
                dateISO    = DateTime.Now.ToString("o"),
                moodEmoji  = moodEmoji,
                text       = text,
                xpAwarded  = alreadyWroteToday ? 0 : XP_PER_ENTRY,
            };

            _entries.Insert(0, entry);

            if (!alreadyWroteToday)
                Sparq.Core.XPSystem.Instance.Award(XP_PER_ENTRY);

            OnEntrySaved?.Invoke(entry);
            Save();
            return true;
        }

        private void Load()
        {
            // TODO: deserialize from PlayerPrefs JSON
        }

        private void Save()
        {
            // TODO: serialize _entries to JSON
            PlayerPrefs.Save();
        }
    }
}
