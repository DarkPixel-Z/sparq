// Progression.cs — the single source of truth for hero XP & leveling.
//
// Before this existed, five systems each ran their own level-up loop with TWO
// different curves (`level*100` in quests/daily, `xpToNextLevel*1.25` in
// battle/loot/AFK), so the curve you got depended on which system happened to
// level you up. Everything now routes through Progression.GrantXp so leveling
// is consistent, deterministic, and tunable in one place.
//
// Curve: gentle early (fast first levels = good first-run feel), compounding
// ~18%/level after. Tune CURVE_BASE / BASE_XP here and the whole game follows.

using UnityEngine;

namespace Sparq.Systems
{
    public static class Progression
    {
        private const float BASE_XP    = 100f;   // XP for level 1 → 2 (matches PlayerData default)
        private const float CURVE_BASE = 1.18f;  // each level needs ~18% more than the last

        /// <summary>XP required to advance FROM `level` TO level+1.</summary>
        public static int XpToNext(int level)
        {
            if (level < 1) level = 1;
            return Mathf.RoundToInt(BASE_XP * Mathf.Pow(CURVE_BASE, level - 1));
        }

        /// <summary>Cumulative XP to reach `level` from level 1 (for tooling/UI).</summary>
        public static int TotalXpToReach(int level)
        {
            int sum = 0;
            for (int l = 1; l < Mathf.Max(1, level); l++) sum += XpToNext(l);
            return sum;
        }

        /// <summary>
        /// Grant XP and resolve level-ups against the canonical curve. Updates
        /// totalXP, currentXP, level, and xpToNextLevel consistently. Returns the
        /// number of levels gained (for "LEVEL UP!" feedback).
        /// </summary>
        public static int GrantXp(Sparq.Core.PlayerData d, int amount)
        {
            if (d == null || amount <= 0) return 0;
            d.totalXP   += amount;
            d.currentXP += amount;

            // Heal a stale/zero threshold from older saves before looping.
            if (d.xpToNextLevel <= 0) d.xpToNextLevel = XpToNext(d.level);

            int gained = 0;
            while (d.currentXP >= d.xpToNextLevel && gained < 1000)
            {
                d.currentXP   -= d.xpToNextLevel;
                d.level++;
                gained++;
                d.xpToNextLevel = XpToNext(d.level);
            }
            return gained;
        }

        // Migration latch — runs once per session, unless a fresh save is
        // dropped in by CloudSave (in which case ResetMigrationLatch() forces a
        // re-run against the new data).
        private static bool _migrated;

        /// <summary>
        /// Force the next MigrateLegacyThreshold call to run again. Use after
        /// replacing SaveService.Data wholesale (cloud merge, debug load) —
        /// otherwise a fresh save carrying a legacy `xpToNextLevel` would
        /// silently skip migration because the latch had already flipped.
        /// </summary>
        public static void ResetMigrationLatch() { _migrated = false; }

        /// <summary>
        /// Bring legacy saves onto the canonical curve. Before the curve unification,
        /// quests used `level*100` and battle/loot/AFK used `xpToNextLevel*1.25`,
        /// leaving returning players with thresholds that no longer match
        /// XpToNext(level). Without this, a level-10 returning player could see
        /// `xpToNextLevel = 1000` while the canonical value is ~459 — they earn
        /// XP that should level them up and nothing happens until the next actual
        /// level-up self-heals it. Idempotent + per-session-latched; safe to call
        /// on every lobby open.
        /// </summary>
        public static void MigrateLegacyThreshold(Sparq.Core.PlayerData d)
        {
            if (_migrated || d == null) return;
            _migrated = true;

            bool thresholdHealed = false;
            int canonical = XpToNext(d.level);
            if (d.xpToNextLevel != canonical)
            {
                UnityEngine.Debug.Log($"[Progression] Migrating xpToNextLevel " +
                    $"{d.xpToNextLevel} → {canonical} (level {d.level}).");
                d.xpToNextLevel = canonical;
                thresholdHealed = true;
            }

            // Flush any backlog: if currentXP was sitting above the canonical
            // threshold (e.g. earned under a higher legacy threshold), level up.
            int gained = 0;
            while (d.currentXP >= d.xpToNextLevel && d.xpToNextLevel > 0 && gained < 1000)
            {
                d.currentXP -= d.xpToNextLevel;
                d.level++;
                gained++;
                d.xpToNextLevel = XpToNext(d.level);
            }
            // Persist whenever we changed anything — a pure threshold heal
            // (gained == 0) still needs to hit disk or an OS kill before any
            // other Save would lose the correction and reload the legacy value.
            if (gained > 0)
                UnityEngine.Debug.Log($"[Progression] Migration flushed {gained} pending level(s).");
            if (gained > 0 || thresholdHealed)
                try { Sparq.Core.SaveService.Save(); } catch {}
        }
    }
}
