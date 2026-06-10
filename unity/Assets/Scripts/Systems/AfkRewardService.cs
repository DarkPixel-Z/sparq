// AfkRewardService.cs — idle / AFK ("away from keyboard") earnings.
//
// The hero squad keeps "earning" coins + XP over real time while the player is
// away, capped at MAX_AFK_HOURS. Rewards accrue from the last claim timestamp
// (PlayerData.afkLastClaimUnix) and are handed out via the AfkRewardPopup when
// the explore map opens. Rate scales with player level so it stays meaningful
// as the player progresses.

using UnityEngine;

namespace Sparq.Systems
{
    public static class AfkRewardService
    {
        // Offline earnings stop accumulating after this many hours (classic
        // idle-game cap so players still return, but aren't punished for life).
        public const float MAX_AFK_HOURS = 8f;

        // Don't bother showing the popup for trivially short absences.
        public const float MIN_CLAIM_SECONDS = 60f;

        // Per-hour rates, scaled by level.
        public static int CoinsPerHour()
        {
            int lvl = 1;
            try { lvl = Mathf.Max(1, Sparq.Core.SaveService.Data?.level ?? 1); } catch {}
            return Mathf.Max(80, lvl * 140);
        }

        public static int XpPerHour()
        {
            int lvl = 1;
            try { lvl = Mathf.Max(1, Sparq.Core.SaveService.Data?.level ?? 1); } catch {}
            // Trimmed from level*25 → level*18 so ACTIVE play (battles/quests) is
            // the main leveling driver; idle is a meaningful bonus, not dominant.
            return Mathf.Max(12, lvl * 18);
        }

        // Wellness → power, even while away: today's Vitality boosts idle earnings
        // (up to +35% at full vitality). Mirrors the squad battle buff.
        public static float VitalityMultiplier()
        {
            try { return Sparq.Systems.VitalityService.SquadStatMultiplier(); } catch { return 1f; }
        }

        // Effective (vitality-boosted) per-hour rates — used by the HUD rate chip.
        public static int EffectiveCoinsPerHour() => Mathf.RoundToInt(CoinsPerHour() * VitalityMultiplier());
        public static int EffectiveXpPerHour()    => Mathf.RoundToInt(XpPerHour() * VitalityMultiplier());

        // Today's Vitality bonus as a whole percent (0 = none), for UI.
        public static int VitalityBonusPercent() => Mathf.RoundToInt((VitalityMultiplier() - 1f) * 100f);

        private static long NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Seconds elapsed since the last claim, clamped to [0, MAX_AFK_HOURS].
        // Also lazily initialises the timestamp on first ever call.
        public static float ElapsedSeconds()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return 0f;
            if (d.afkLastClaimUnix <= 0)
            {
                d.afkLastClaimUnix = NowUnix();
                try { Sparq.Core.SaveService.Save(); } catch {}
                return 0f;
            }
            float secs = NowUnix() - d.afkLastClaimUnix;
            if (secs < 0f) secs = 0f;   // clock skew guard
            return Mathf.Min(secs, MAX_AFK_HOURS * 3600f);
        }

        // Rewards waiting to be claimed right now.
        public static (int coins, int xp, float hours, bool capped) Pending()
        {
            float secs = ElapsedSeconds();
            float hours = secs / 3600f;
            float vit = VitalityMultiplier();
            int coins = Mathf.RoundToInt(CoinsPerHour() * hours * vit);
            int xp    = Mathf.RoundToInt(XpPerHour() * hours * vit);
            bool capped = secs >= MAX_AFK_HOURS * 3600f - 1f;
            return (coins, xp, hours, capped);
        }

        public static bool HasClaimable()
        {
            return ElapsedSeconds() >= MIN_CLAIM_SECONDS && Pending().coins > 0;
        }

        // Grants the pending coins + XP (× multiplier, e.g. 2 for a watch-ad
        // doubler) with level-up resolution, and resets the timer. Returns what
        // was granted so the UI can show it.
        public static (int coins, int xp, int levelsGained) Claim(int multiplier = 1)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return (0, 0, 0);
            if (multiplier < 1) multiplier = 1;

            var p = Pending();
            int coins = p.coins * multiplier;
            int xp    = p.xp * multiplier;
            int levelsGained = 0;

            d.sparqCoins += coins;

            // Single canonical XP/level curve (shared with battle/quest/loot).
            if (xp > 0) levelsGained = Progression.GrantXp(d, xp);

            d.afkLastClaimUnix = NowUnix();
            try { Sparq.Core.SaveService.Save(); } catch {}
            return (coins, xp, levelsGained);
        }

        // Resets the timer without granting (call when leaving the idle map so
        // the next session's "away" window starts fresh after a manual session).
        public static void Touch()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            d.afkLastClaimUnix = NowUnix();
            try { Sparq.Core.SaveService.Save(); } catch {}
        }

        // Human-readable "Xh Ym" for the popup.
        public static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            if (h > 0) return $"{h}h {m}m";
            if (m > 0) return $"{m}m";
            return $"{total}s";
        }
    }
}
