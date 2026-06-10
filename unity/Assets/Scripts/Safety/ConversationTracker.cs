using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// Multi-turn grooming detection.
    ///
    /// Single-message checks (ContentModerator) catch the blunt predator. They
    /// MISS the patient one — who keeps every individual message innocuous and
    /// lets the grooming pattern accumulate across a whole conversation:
    /// a little flattery here, a personal-info question there, a "don't tell"
    /// later. No single message trips a keyword filter.
    ///
    /// ConversationTracker keeps a rolling window of recent INCOMING messages
    /// per sender and feeds them to ContextClassifier.ScoreConversation(),
    /// which aggregates the signal across messages. When a sender's aggregate
    /// risk crosses HIGH, the caller can warn the user + auto-flag.
    ///
    /// In-memory only — a rolling conversational window, reset per app session.
    /// A real backend would persist + analyse this server-side; this is the
    /// client-side early-warning layer.
    /// </summary>
    public static class ConversationTracker
    {
        // Tunables
        private const int   MAX_MESSAGES   = 10;     // ring-buffer cap per sender
        private const float WINDOW_MINUTES = 30f;    // messages older than this drop out
        private const int   MIN_FOR_PATTERN = 3;     // need a few messages before a "pattern" means anything

        public enum RiskLevel { None, Watch, High }

        public class ConversationVerdict
        {
            public string       Sender;
            public RiskLevel    Level;
            public float        CompositeRisk;        // 0..1 from ScoreConversation
            public List<string> TopSignals = new List<string>();
            public int          MessageCount;
            /// <summary>True only on the message that first pushes this sender into HIGH —
            /// so callers warn / auto-flag exactly once per sender per session.</summary>
            public bool         FirstTimeHigh;
        }

        private class Entry { public string text; public float time; }

        private static readonly Dictionary<string, List<Entry>> _bySender =
            new Dictionary<string, List<Entry>>();
        // Senders we've already raised a HIGH alert for (de-dupes the warning).
        private static readonly HashSet<string> _flaggedHigh = new HashSet<string>();

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Record an incoming message from `sender` and return the current
        /// conversation-level risk for that sender's recent message window.
        /// </summary>
        public static ConversationVerdict RecordIncoming(string sender, string text)
        {
            var v = new ConversationVerdict { Sender = sender ?? "", Level = RiskLevel.None };
            if (string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(text))
                return v;

            string key = sender.Trim().ToLower();
            if (!_bySender.TryGetValue(key, out var list))
            {
                list = new List<Entry>();
                _bySender[key] = list;
            }

            float now = Time.unscaledTime;
            list.Add(new Entry { text = text, time = now });

            // Prune: drop messages outside the time window, then cap the count.
            float cutoff = now - WINDOW_MINUTES * 60f;
            list.RemoveAll(e => e.time < cutoff);
            while (list.Count > MAX_MESSAGES) list.RemoveAt(0);

            v.MessageCount = list.Count;

            // A "pattern" needs more than one or two messages to be meaningful.
            if (list.Count < MIN_FOR_PATTERN) return v;

            var texts = new List<string>(list.Count);
            foreach (var e in list) texts.Add(e.text);

            var score = ContextClassifier.ScoreConversation(texts);
            v.CompositeRisk = score.CompositeRisk;
            v.TopSignals = score.TopSignals;

            if (score.HighRisk)
            {
                v.Level = RiskLevel.High;
                // HashSet.Add returns true only the first time → de-dupes alerts.
                v.FirstTimeHigh = _flaggedHigh.Add(key);
            }
            else if (score.MediumRisk)
            {
                v.Level = RiskLevel.Watch;
            }

            return v;
        }

        /// <summary>Drop a sender's history + alert state (e.g. after the user blocks them).</summary>
        public static void Clear(string sender)
        {
            if (string.IsNullOrWhiteSpace(sender)) return;
            string key = sender.Trim().ToLower();
            _bySender.Remove(key);
            _flaggedHigh.Remove(key);
        }

        public static void ClearAll()
        {
            _bySender.Clear();
            _flaggedHigh.Clear();
        }
    }
}
