using System.Collections.Generic;
using UnityEngine;
using Sparq.Models;

namespace Sparq.Systems
{
    /// <summary>
    /// Tracks the player's active quests. Daily picks N fresh quests from
    /// the QuestCatalog daily pool, filtered by the user's energy (Spoon
    /// Check). Awards XP on completion, updates streak (with Grace Day
    /// shields), and hands off the tutorial to UnaController.
    ///
    /// Quest *schema* (cadence, category, xp, sensitivity, packId) lives
    /// in QuestCatalog.cs. Quest *copy* (titles, completion lines, streak
    /// milestone messages) lives in Resources/sparq-content.json and is
    /// loaded by QuestContent.cs.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        // 4 daily quests by default. SparqLimits.MAX_DAILY_QUESTS_SHOWN is the
        // ceiling (5) — we surface fewer to keep the home screen scannable.
        private const int DAILY_QUEST_COUNT = 4;

        public event System.Action<Sparq.Core.CustomTask> OnQuestCompleted;
        public event System.Action OnDailyReset;
        /// <summary>Fires when a food item drops from a completed quest.</summary>
        public event System.Action<string> OnFoodDropped;
        /// <summary>Fires when a streak milestone hits — UI shows the
        /// Buddy reaction copy from QuestContent.</summary>
        public event System.Action<Sparq.Core.CustomTask, int, string> OnStreakMilestone;

        // Set true when a Grace Day shield absorbs a missed day during the daily
        // reset. The lobby reads + clears it via ConsumeGraceDaySavedFlag() to
        // show a "Streak saved!" toast — making the safety net visible.
        private static bool _graceDayJustUsed;
        public static bool ConsumeGraceDaySavedFlag()
        {
            if (!_graceDayJustUsed) return false;
            _graceDayJustUsed = false;
            return true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            CheckDailyReset();
        }

        // ─────────────────────────────────────────────────────────────────
        // DAILY RESET — picks fresh quests on day rollover, runs streak
        // logic with Grace Day shields.
        // ─────────────────────────────────────────────────────────────────

        public void CheckDailyReset()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            if (data.customTasks == null) data.customTasks = new List<Sparq.Core.CustomTask>();

            string today = System.DateTime.Now.ToString("yyyy-MM-dd");

            if (data.lastQuestResetDate != today)
            {
                // Local guard: did THIS reset consume a shield? Used below to
                // block the weekly grant in the same pass. Must be a local —
                // the static _graceDayJustUsed flag below persists across app
                // sessions until the lobby reads it, so it can't double as a
                // "this-reset-only" signal.
                bool shieldJustConsumed = false;

                // Streak logic with Grace Days (= streakShields). One missed
                // day is absorbed; two+ resets if no shield is held.
                if (!string.IsNullOrEmpty(data.lastActiveDate) && !string.IsNullOrEmpty(data.lastQuestResetDate))
                {
                    if (System.DateTime.TryParse(data.lastQuestResetDate, out var prev))
                    {
                        var daysGap = (System.DateTime.Now.Date - prev.Date).Days;
                        if (daysGap == 1 && data.completedToday >= 1)
                        {
                            data.streak++;
                            if (data.streak > data.longestStreak) data.longestStreak = data.streak;
                        }
                        else if (daysGap >= 2)
                        {
                            if (data.streakShields > 0)
                            {
                                data.streakShields--;
                                // A shield absorbed the missed day — surface this so
                                // the safety net is visible (the lobby shows a toast).
                                _graceDayJustUsed = true;
                                shieldJustConsumed = true;
                            }
                            else data.streak = 0;
                        }
                    }
                }

                // Weekly Grace Day grant — every 7 days played, bank one
                // (capped). Free safety net for ADHD/low-energy users.
                // Skip the grant if a shield was just consumed in THIS same
                // reset — otherwise the user could skip 2 days at any streak
                // divisible by 7 (7, 14, 21…) and net-zero the shield cost,
                // gaming the safety net indefinitely.
                if (!shieldJustConsumed)
                    GrantWeeklyGraceDayIfDue(data, today);

                RegenerateDailyQuests(data);
                data.completedToday = 0;
                data.lastQuestResetDate = today;
                Sparq.Core.SaveService.ScheduleSave();

                Debug.Log($"[QuestManager] Daily reset → {data.customTasks.Count} fresh quests. Streak: {data.streak}, Grace Days: {data.streakShields}");
                OnDailyReset?.Invoke();
            }
            else if (data.customTasks.Count == 0)
            {
                // Safety net: same day but no quests (first launch ever).
                RegenerateDailyQuests(data);
                data.lastQuestResetDate = today;
                Sparq.Core.SaveService.ScheduleSave();
            }
        }

        private void GrantWeeklyGraceDayIfDue(Sparq.Core.PlayerData data, string today)
        {
            // Use the streak itself as the weekly cadence — every 7 streak
            // days we add one shield, capped by SparqLimits.
            if (data.streak > 0 && data.streak % 7 == 0)
            {
                if (data.streakShields < SparqLimits.MAX_GRACE_DAYS_BANKED)
                {
                    data.streakShields++;
                    Debug.Log($"[QuestManager] Granted a Grace Day (now {data.streakShields}).");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // QUEST GENERATION — pull from QuestCatalog, filter by energy, seed
        // the RNG by today's date so the daily set is stable per user/day.
        // ─────────────────────────────────────────────────────────────────

        private void RegenerateDailyQuests(Sparq.Core.PlayerData data)
        {
            data.customTasks.Clear();

            var energy = InferEnergyLevel();
            var activePack = data.activeQuestPackId; // null/empty if none
            var pool = QuestCatalog.DailyPool(includeSensitive: false, activePackId: activePack);

            // Filter by energy: low-energy days surface smaller quests.
            var eligible = new List<Quest>();
            foreach (var q in pool)
                if ((int)q.minEnergy <= (int)energy) eligible.Add(q);
            if (eligible.Count == 0) eligible = pool;   // safety: never empty

            // Seeded shuffle so the same user sees a consistent daily pick.
            var seed = System.DateTime.Now.Date.GetHashCode();
            var rng = new System.Random(seed);
            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (eligible[i], eligible[j]) = (eligible[j], eligible[i]);
            }

            int n = Mathf.Min(DAILY_QUEST_COUNT, eligible.Count);
            for (int i = 0; i < n; i++)
            {
                var q = eligible[i];
                data.customTasks.Add(new Sparq.Core.CustomTask {
                    name   = QuestContent.GetShortLabel(q.id),
                    xp     = q.xp,
                    done   = false,
                    questId = q.id,
                });
            }

            // Always include the floor quest (Open Sparq) auto-completed at
            // the start of the day. It's the "you opened the app" credit
            // — protects users on bad days from a zero-completion record.
            EnsureFloorQuestSatisfied(data);
        }

        private void EnsureFloorQuestSatisfied(Sparq.Core.PlayerData data)
        {
            var floor = QuestCatalog.Get("open_sparq");
            if (floor == null) return;
            // Add it as a pre-completed task — visible proof on bad days
            // that something was logged. XP is intentionally tiny.
            data.customTasks.Add(new Sparq.Core.CustomTask {
                name    = QuestContent.GetShortLabel(floor.id),
                xp      = floor.xp,
                done    = true,
                questId = floor.id,
            });
            // Credit the XP for opening the app once per day — gated by its own
            // date key so the three callers of RegenerateDailyQuests (daily
            // rollover, same-day safety-net at L112 when customTasks is empty,
            // and ForceRefresh) can't re-grant floor.xp within the same day.
            const string FLOOR_XP_KEY = "sparq.quests.floorXpDate";
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(FLOOR_XP_KEY, "") != today)
            {
                Sparq.Systems.Progression.GrantXp(data, floor.xp);
                PlayerPrefs.SetString(FLOOR_XP_KEY, today);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Maps today's logged Spoon Check (MoodService) to an
        /// EnergyLevel. No log today → Medium (don't penalize for not
        /// having checked in).</summary>
        public static EnergyLevel InferEnergyLevel()
        {
            try
            {
                var entries = Sparq.Systems.MoodService.LoadAll();
                if (entries == null || entries.Count == 0) return EnergyLevel.Medium;
                // last entry of today, else most recent. Entry is a struct,
                // so use a nullable wrapper to express "no pick yet".
                var today = System.DateTime.UtcNow.Date;
                Sparq.Systems.MoodService.Entry? pick = null;
                foreach (var e in entries)
                {
                    var d = System.DateTimeOffset.FromUnixTimeSeconds(e.unix).UtcDateTime.Date;
                    if (d == today) pick = e;
                }
                if (!pick.HasValue) pick = entries[entries.Count - 1];

                switch (pick.Value.mood)
                {
                    case Sparq.Systems.MoodService.Mood.Tired:        return EnergyLevel.Low;
                    case Sparq.Systems.MoodService.Mood.Overwhelmed:  return EnergyLevel.Low;
                    case Sparq.Systems.MoodService.Mood.Calm:         return EnergyLevel.Medium;
                    case Sparq.Systems.MoodService.Mood.Focused:      return EnergyLevel.High;
                    case Sparq.Systems.MoodService.Mood.Proud:        return EnergyLevel.High;
                }
            }
            catch {}
            return EnergyLevel.Medium;
        }

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Manual refresh — for a debug button or settings "refresh quests" option.</summary>
        public void ForceRefresh()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            RegenerateDailyQuests(data);
            data.completedToday = 0;
            Sparq.Core.SaveService.ScheduleSave();
            OnDailyReset?.Invoke();
            Debug.Log("[QuestManager] Forced refresh.");
        }

        public IList<Sparq.Core.CustomTask> GetActiveQuests()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return new List<Sparq.Core.CustomTask>();
            return data.customTasks;
        }

        /// <summary>Mark a quest as done and award its XP. Idempotent.</summary>
        public void CompleteQuest(Sparq.Core.CustomTask quest)
        {
            if (quest == null || quest.done) return;
            quest.done = true;

            var data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.totalTasksDone++;
                data.completedToday++;
                data.lastActiveDate = System.DateTime.Now.ToString("yyyy-MM-dd");

                // XP + level-up via the single canonical curve.
                Sparq.Systems.Progression.GrantXp(data, quest.xp);

                // Rival catches up slightly
                data.fitchXP += Mathf.FloorToInt(quest.xp * 0.4f);

                // First quest complete → tutorial is done, Una fades out
                if (!data.onboardingComplete && data.totalTasksDone >= 1)
                {
                    Sparq.UI.UnaController.CompleteOnboarding();
                }

                Sparq.Core.SaveService.ScheduleSave();
            }

            // Streak milestone — surface the Buddy line from JSON content.
            CheckStreakMilestone(quest, data);

            // Food drop chance — closes the pet-care loop.
            string droppedFood = null;
            try
            {
                if (UnityEngine.Random.value < 0.35f)
                    droppedFood = Sparq.Systems.PetService.RollFoodDrop();
            }
            catch {}

            if (!string.IsNullOrEmpty(droppedFood))
                Debug.Log($"[Quest] Completed '{quest.name}' (+{quest.xp} XP) — dropped {droppedFood}");
            else
                Debug.Log($"[Quest] Completed '{quest.name}' (+{quest.xp} XP)");

            OnQuestCompleted?.Invoke(quest);
            if (!string.IsNullOrEmpty(droppedFood)) OnFoodDropped?.Invoke(droppedFood);
        }

        private void CheckStreakMilestone(Sparq.Core.CustomTask quest, Sparq.Core.PlayerData data)
        {
            if (data == null || string.IsNullOrEmpty(quest?.questId)) return;
            var def = QuestCatalog.Get(quest.questId);
            if (def?.streakMilestones == null) return;
            foreach (var m in def.streakMilestones)
            {
                if (data.streak == m)
                {
                    var msg = QuestContent.GetStreakMessage(def.id, m);
                    if (!string.IsNullOrEmpty(msg))
                    {
                        Debug.Log($"[Quest] Streak milestone {m} on '{def.id}': {msg}");
                        OnStreakMilestone?.Invoke(quest, m, msg);
                    }
                    break;
                }
            }
        }
    }
}
