using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Daily login reward — the retention backbone. On the first lobby open each
    /// calendar day:
    ///   - Consecutive day  → streak advances, show the next day's reward
    ///   - 1-day gap        → reset to day 1
    ///   - Day 7            → big reward + a Mystery Egg, then the cycle restarts
    ///
    /// Logic is static + prefab-free; the UI is the procedural DailyBonusPopup
    /// (so it always shows, never depends on an unassigned prefab). Call
    /// DailyBonusManager.ShowIfDue() when the lobby opens.
    /// </summary>
    public class DailyBonusManager : MonoBehaviour
    {
        // Reward per day. Day 7 is the big one (+ an egg, granted in Grant()).
        public static readonly int[] DAILY_COINS = { 50, 75, 100, 150, 200, 300, 500 };
        public static readonly int[] DAILY_XP    = {  5, 10,  15,  20,  25,  30,  50 };

        // ── Entry point: show the daily reward if it hasn't been claimed today ──
        public static void ShowIfDue()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (data.lastDailyBonusDate == today) return;   // already claimed today

            int dayToShow = 1;
            if (!string.IsNullOrEmpty(data.lastDailyBonusDate)
                && System.DateTime.TryParse(data.lastDailyBonusDate, out var prev))
            {
                int gap = (System.DateTime.Now.Date - prev.Date).Days;
                if (gap <= 0) return;                                  // same/future day — ignore
                if (gap == 1) dayToShow = (data.dailyBonusStreak % 7) + 1;  // continue the cycle
                else dayToShow = 1;                                    // missed a day → reset
            }

            try { Sparq.UI.DailyBonusPopup.Show(dayToShow, Grant); }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DailyBonus] popup failed ({ex.Message}) — granting silently.");
                Grant(dayToShow);
            }
        }

        // ── Grant the day's reward + persist ──
        public static void Grant(int day)
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            int idx   = Mathf.Clamp(day - 1, 0, DAILY_COINS.Length - 1);
            int coins = DAILY_COINS[idx];
            int xp    = DAILY_XP[idx];

            data.sparqCoins += coins;
            Progression.GrantXp(data, xp);   // canonical XP + level-up curve

            // Day 7 also drops a Mystery Egg into the inventory.
            if (day >= 7)
            {
                if (data.eggInventory == null) data.eggInventory = new System.Collections.Generic.List<string>();
                data.eggInventory.Add("epic");
            }

            data.dailyBonusStreak   = day;
            data.lastDailyBonusDate = System.DateTime.Now.ToString("yyyy-MM-dd");
            // Save immediately (not debounced) so the date persists even if the
            // editor is stopped before the debounce timer fires.
            Sparq.Core.SaveService.Save();

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
            Debug.Log($"[DailyBonus] Day {day} claimed — +{coins} coins, +{xp} XP{(day >= 7 ? " +egg" : "")}.");
        }
    }
}
