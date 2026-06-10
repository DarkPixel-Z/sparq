// VitalityService.cs — the bridge between real self-care and squad power.
//
// Sparq is a *wellness* RPG: the point is that looking after yourself in real
// life makes your heroes stronger in the game. Vitality is the visible knob
// that does it. Healthy actions TODAY charge a battle buff that boosts the
// whole squad's ATK + HP for the day:
//
//   • Spoon check (mood log) today ........ +25%
//   • Quests completed today (0.10 each) .. up to +30%
//   • Active daily streak ................. +15%
//   • Pet well-cared (avg of 4 meters) .... up to +30%
//
// The raw vitality is clamped to 0..1 (100%); at full vitality the squad gets
// the full MAX_BUFF (+35% ATK & HP). Nothing is persisted — it's derived live
// from the existing save fields, so it always reflects "what you did today".

using UnityEngine;

namespace Sparq.Systems
{
    public static class VitalityService
    {
        // Squad stat bonus at 100% vitality (+35% ATK & HP).
        public const float MAX_BUFF = 0.35f;

        // Per-source contributions (clamped so the total never exceeds 1.0).
        private const float SPOON_WEIGHT   = 0.25f;
        private const float QUEST_PER       = 0.10f;
        private const float QUEST_CAP       = 0.30f;
        private const float STREAK_WEIGHT   = 0.15f;
        private const float PET_CARE_WEIGHT = 0.30f;

        /// <summary>Today's vitality, 0..1.</summary>
        public static float Today()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return 0f;

            float v = 0f;

            // Spoon check / mood logged today.
            try { if (Sparq.Systems.MoodService.LoggedToday()) v += SPOON_WEIGHT; } catch {}

            // Quests completed today.
            v += Mathf.Clamp(d.completedToday * QUEST_PER, 0f, QUEST_CAP);

            // Active daily streak.
            if (d.streak > 0) v += STREAK_WEIGHT;

            // Pet care — average of the four meters, only if the pet's alive.
            if (d.petAlive)
            {
                float care = (d.hunger + d.petHygiene + d.petDental + d.petSocial) / 400f;
                v += PET_CARE_WEIGHT * Mathf.Clamp01(care);
            }

            return Mathf.Clamp01(v);
        }

        /// <summary>Squad ATK/HP multiplier, e.g. 1.21 = +21%.</summary>
        public static float SquadStatMultiplier() => 1f + Today() * MAX_BUFF;

        /// <summary>Whole-number squad-power bonus for UI, e.g. 21 (%).</summary>
        public static int BuffPercent() => Mathf.RoundToInt(Today() * MAX_BUFF * 100f);

        /// <summary>Raw vitality as a whole percent, e.g. 60 (%).</summary>
        public static int VitalityPercent() => Mathf.RoundToInt(Today() * 100f);

        /// <summary>Short multi-line breakdown for a tooltip / popup.</summary>
        public static string Breakdown()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return "Take care of yourself today to power up your squad.";

            bool spoon = false;
            try { spoon = Sparq.Systems.MoodService.LoggedToday(); } catch {}
            int quests = Mathf.Max(0, d.completedToday);
            bool streak = d.streak > 0;
            float care = d.petAlive ? (d.hunger + d.petHygiene + d.petDental + d.petSocial) / 400f : 0f;

            string Check(bool on) => on ? "[x]" : "[  ]";
            return
                $"{Check(spoon)} Spoon check\n" +
                $"{Check(quests > 0)} Quests today ({quests})\n" +
                $"{Check(streak)} Streak\n" +
                $"{Check(care >= 0.6f)} Happy pet ({Mathf.RoundToInt(care * 100f)}%)";
        }
    }
}
