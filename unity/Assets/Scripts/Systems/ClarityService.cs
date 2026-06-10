using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Clarity = the "Tome of Wisdom". A static deck of practice cards
    /// (grounding / breathing / one-line journaling / sensory checks).
    /// Each card practiced grants XP and increments a counter.
    /// </summary>
    public static class ClarityService
    {
        public class Card
        {
            public string id;
            public string title;
            public string body;
            public string glyph;        // ASCII glyph (single letter — TMP-safe)
            public Color tint;
        }

        // Curated starter deck. Fantasy-flavored verbs, grounded practices.
        public static readonly Card[] Deck = new[]
        {
            new Card { id = "breath4",    title = "Cycle of Four",
                body = "Breathe in for four. Hold for four.\nOut for four. Hold for four.\nThree cycles.",
                glyph = "B", tint = new Color(0.55f, 0.85f, 1f) },

            new Card { id = "ground5",    title = "Anchor of the Senses",
                body = "Name 5 things you see, 4 you feel,\n3 you hear, 2 you smell, 1 you taste.\nReturn to the room.",
                glyph = "G", tint = new Color(0.55f, 0.85f, 0.45f) },

            new Card { id = "scroll1",    title = "One True Sentence",
                body = "Write a single honest sentence about today.\nNo fixing. No editing. Just one true line.",
                glyph = "S", tint = new Color(1f,    0.85f, 0.35f) },

            new Card { id = "stretch",    title = "Loosen the Armor",
                body = "Stand. Roll your shoulders.\nReach up, reach wide, fold forward.\nThirty seconds.",
                glyph = "L", tint = new Color(0.85f, 0.55f, 0.40f) },

            new Card { id = "name3",      title = "Name the Foe",
                body = "Name what you're feeling — out loud or in writing.\n'I am ____.' Naming weakens it.",
                glyph = "N", tint = new Color(0.85f, 0.40f, 0.50f) },

            new Card { id = "small",      title = "The Smallest Step",
                body = "Pick the tiniest version of the next task.\nDo only that. The mountain begins with one stone.",
                glyph = "T", tint = new Color(0.65f, 0.55f, 0.85f) },
        };

        private const string KEY_PRACTICED = "sparq.clarity.practiced";

        public static int TotalPracticed => PlayerPrefs.GetInt(KEY_PRACTICED, 0);

        public static int PracticedCount(string cardId)
            => PlayerPrefs.GetInt(CountKey(cardId), 0);

        public static void Practice(string cardId)
        {
            PlayerPrefs.SetInt(CountKey(cardId), PracticedCount(cardId) + 1);
            PlayerPrefs.SetInt(KEY_PRACTICED, TotalPracticed + 1);
            PlayerPrefs.Save();

            // Grant XP via existing save data
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null)
                {
                    Progression.GrantXp(data, 5);   // single canonical curve
                    Sparq.Core.SaveService.Save();
                }
            }
            catch {}
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.LevelUp); } catch {}
        }

        private static string CountKey(string cardId) => $"sparq.clarity.{cardId}";

        public static Card FindById(string id)
        {
            foreach (var c in Deck) if (c.id == id) return c;
            return null;
        }
    }
}
