using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sparq.Safety
{
    /// <summary>
    /// Context-aware safety classifier. Scores messages on multiple
    /// signal dimensions and flags COMBINATIONS that keyword filters miss.
    ///
    /// This is a heuristic stand-in for a real ML model. The interface
    /// (`Score(text, context) → SafetyScore`) is designed so the inside can
    /// be replaced by an embedded model (Sentence-BERT, MobileBERT, etc.)
    /// later without changing callers.
    ///
    /// SIGNALS scored:
    ///   - INTIMACY      : pet names, sweetness, "you're special", isolation
    ///   - DEMANDS       : "send me", "show me", "do you want to"
    ///   - PERSONAL_INFO : age/location/school questions
    ///   - SECRECY       : "don't tell", "between us", "private"
    ///   - URGENCY       : "now", "quickly", "tonight", "right now"
    ///   - FLATTERY      : "you're mature", "you're different", "older than"
    ///   - CONTROL       : "delete this", "if you tell"
    ///
    /// Predator grooming = HIGH SCORES IN MULTIPLE DIMENSIONS over a short
    /// conversation. A single signal is often innocent. Multiple together
    /// across messages is the actual warning.
    /// </summary>
    public static class ContextClassifier
    {
        public class SafetyScore
        {
            public float Intimacy;      // 0..1
            public float Demands;
            public float PersonalInfo;
            public float Secrecy;
            public float Urgency;
            public float Flattery;
            public float Control;

            /// <summary>Composite risk — weighted sum, 0..1.</summary>
            public float CompositeRisk
            {
                get
                {
                    // Predator grooming pattern: intimacy + demands + secrecy + flattery
                    // are the textbook "Sears 6 stages of grooming" signals.
                    float s = Intimacy   * 0.20f
                            + Demands    * 0.20f
                            + PersonalInfo*0.15f
                            + Secrecy    * 0.20f
                            + Urgency    * 0.05f
                            + Flattery   * 0.10f
                            + Control    * 0.10f;
                    return Mathf.Clamp01(s);
                }
            }

            public bool HighRisk => CompositeRisk >= 0.55f;
            public bool MediumRisk => CompositeRisk >= 0.30f && CompositeRisk < 0.55f;

            public List<string> TopSignals
            {
                get
                {
                    var pairs = new (string name, float val)[]
                    {
                        ("Intimacy", Intimacy), ("Demands", Demands),
                        ("PersonalInfo", PersonalInfo), ("Secrecy", Secrecy),
                        ("Urgency", Urgency), ("Flattery", Flattery),
                        ("Control", Control),
                    };
                    var list = new List<string>();
                    foreach (var p in pairs)
                        if (p.val >= 0.4f) list.Add($"{p.name}({p.val:F2})");
                    return list;
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Signal patterns (lowercase)
        // ─────────────────────────────────────────────────────────────────
        private static readonly string[] IntimacyTokens = {
            "you're special", "you are special", "you are different", "you're different",
            "no one understands you", "no one gets you", "i understand you",
            "we have a connection", "we click", "you're mine", "my babygirl",
            "my princess", "my kitten", "i love you", "love you",
        };
        private static readonly string[] DemandTokens = {
            "send me", "show me", "send a", "send pics", "send pix",
            "i want to see", "let me see", "show your", "take a pic",
        };
        private static readonly string[] PersonalInfoTokens = {
            "how old", "what's your age", "what age", "where do you live",
            "what school", "what city", "what state", "what country",
            "what grade", "alone right now", "home alone", "parents home",
        };
        private static readonly string[] SecrecyTokens = {
            "don't tell", "do not tell", "between us", "our secret",
            "keep this private", "delete this", "delete the message",
            "burn after reading", "ephemeral", "snap me so it disappears",
        };
        private static readonly string[] UrgencyTokens = {
            "right now", "tonight", "in a minute", "quickly", "before they",
            "hurry", "asap", "don't wait", "now",
        };
        private static readonly string[] FlatteryTokens = {
            "you look older", "you act older", "you're mature for", "mature for your age",
            "more mature than", "wise beyond", "older than you look",
            "wow you're pretty", "you're so pretty", "you're so cute",
            "so beautiful", "so handsome", "model material",
        };
        private static readonly string[] ControlTokens = {
            "if you tell", "if you say", "you'll get in trouble", "you'll be in trouble",
            "i'll get mad", "i won't talk to you", "i'll block you",
            "you owe me", "after all i did", "i was nice",
        };

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        public static SafetyScore Score(string text)
        {
            var score = new SafetyScore();
            if (string.IsNullOrWhiteSpace(text)) return score;
            string s = text.ToLower();

            score.Intimacy    = MatchScore(s, IntimacyTokens);
            score.Demands     = MatchScore(s, DemandTokens);
            score.PersonalInfo= MatchScore(s, PersonalInfoTokens);
            score.Secrecy     = MatchScore(s, SecrecyTokens);
            score.Urgency     = MatchScore(s, UrgencyTokens);
            score.Flattery    = MatchScore(s, FlatteryTokens);
            score.Control     = MatchScore(s, ControlTokens);

            // Boost composites:
            // - ALL CAPS in long message → +urgency
            if (text.Length > 12 && IsMostlyCaps(text))
                score.Urgency = Mathf.Min(1f, score.Urgency + 0.3f);
            // - Excessive punctuation (e.g. "??!?!?!?!") → +urgency
            if (Regex.IsMatch(text, @"[!?]{4,}"))
                score.Urgency = Mathf.Min(1f, score.Urgency + 0.2f);

            return score;
        }

        /// <summary>
        /// Score a multi-turn conversation (recent messages from same sender).
        /// A single innocuous message + several earlier signals = high risk.
        /// </summary>
        public static SafetyScore ScoreConversation(IEnumerable<string> recentMessages)
        {
            var agg = new SafetyScore();
            int count = 0;
            foreach (var m in recentMessages)
            {
                if (m == null) continue;
                count++;
                var s = Score(m);
                // Take the MAX of each signal across messages — predator behavior
                // shows up as "highest demand seen so far" not the average.
                agg.Intimacy    = Mathf.Max(agg.Intimacy, s.Intimacy);
                agg.Demands     = Mathf.Max(agg.Demands, s.Demands);
                agg.PersonalInfo= Mathf.Max(agg.PersonalInfo, s.PersonalInfo);
                agg.Secrecy     = Mathf.Max(agg.Secrecy, s.Secrecy);
                agg.Urgency     = Mathf.Max(agg.Urgency, s.Urgency);
                agg.Flattery    = Mathf.Max(agg.Flattery, s.Flattery);
                agg.Control     = Mathf.Max(agg.Control, s.Control);
            }
            return agg;
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNAL
        // ─────────────────────────────────────────────────────────────────

        // 0..1 score for "how strongly do tokens match in this text".
        // Multiple matches don't double-count beyond a soft saturation.
        private static float MatchScore(string lower, string[] tokens)
        {
            int hits = 0;
            foreach (var t in tokens) if (lower.Contains(t)) hits++;
            if (hits == 0) return 0;
            // Saturate: 1 hit = 0.45, 2 = 0.70, 3 = 0.88, 4+ = 1.0
            return Mathf.Clamp01(1f - Mathf.Pow(0.6f, hits));
        }

        private static bool IsMostlyCaps(string s)
        {
            int letters = 0, caps = 0;
            foreach (var ch in s)
            {
                if (char.IsLetter(ch)) { letters++; if (char.IsUpper(ch)) caps++; }
            }
            return letters > 0 && caps >= letters * 0.7f;
        }
    }
}
