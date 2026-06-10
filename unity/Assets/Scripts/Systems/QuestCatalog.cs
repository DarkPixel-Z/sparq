// QuestCatalog.cs — the authoritative quest/pack/achievement registry,
// ported 1-for-1 from sparq-quests.ts. Holds *schema only* (id, cadence,
// category, xp, energy, sensitivity, pack, milestones); the actual copy
// (titles, descriptions, completion lines, streak messages) lives in
// Resources/sparq-content.json and is loaded by Sparq.Systems.QuestContent.
//
// Add new quests here AND in sparq-content.json (matching id). The two
// stay aligned by id.

using System.Collections.Generic;
using System.Linq;
using Sparq.Models;

namespace Sparq.Systems
{
    public static class QuestCatalog
    {
        // ── QUESTS ───────────────────────────────────────────────────────
        public static readonly Dictionary<string, Quest> Quests = new Dictionary<string, Quest>
        {
            // ── DAILY ───────────────────────────────────────────────────
            ["three_second_pause"] = new Quest {
                id = "three_second_pause", cadence = QuestCadence.Daily,
                category = QuestCategory.Pause, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30, 90 },
            },
            ["phone_off_hour"] = new Quest {
                id = "phone_off_hour", cadence = QuestCadence.Daily,
                category = QuestCategory.Focus, xp = 10, minEnergy = EnergyLevel.Medium,
                streakMilestones = new[] { 7, 30 },
            },
            ["future_note"] = new Quest {
                id = "future_note", cadence = QuestCadence.Daily,
                category = QuestCategory.Reflection, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 }, generatesData = true,
            },
            ["hard_thing_first"] = new Quest {
                id = "hard_thing_first", cadence = QuestCadence.Daily,
                category = QuestCategory.Initiation, xp = 15, minEnergy = EnergyLevel.Medium,
                streakMilestones = new[] { 5, 30 },
            },
            ["move_20"] = new Quest {
                id = "move_20", cadence = QuestCadence.Daily,
                category = QuestCategory.Movement, xp = 10, minEnergy = EnergyLevel.Medium,
                streakMilestones = new[] { 7, 30 },
            },
            ["wind_down"] = new Quest {
                id = "wind_down", cadence = QuestCadence.Daily,
                category = QuestCategory.Sleep, xp = 10, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 },
            },

            // ── DAILY — ADHD / AUTISM SPECIFIC ──────────────────────────
            ["two_minute_start"] = new Quest {
                id = "two_minute_start", cadence = QuestCadence.Daily,
                category = QuestCategory.Initiation, xp = 10, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 10, 30 },
            },
            ["sensory_reset"] = new Quest {
                id = "sensory_reset", cadence = QuestCadence.Daily,
                category = QuestCategory.Sensory, xp = 10, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 },
            },
            ["transition_buffer"] = new Quest {
                id = "transition_buffer", cadence = QuestCadence.Daily,
                category = QuestCategory.Sensory, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7 },
            },
            ["spoon_check"] = new Quest {
                id = "spoon_check", cadence = QuestCadence.Daily,
                category = QuestCategory.Meta, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 }, generatesData = true,
            },
            ["hyperfocus_capture"] = new Quest {
                id = "hyperfocus_capture", cadence = QuestCadence.Daily,
                category = QuestCategory.Meta, xp = 15, minEnergy = EnergyLevel.Low,
                generatesData = true,
            },
            ["body_doubling"] = new Quest {
                id = "body_doubling", cadence = QuestCadence.Daily,
                category = QuestCategory.Social, xp = 15, minEnergy = EnergyLevel.Medium,
                streakMilestones = new[] { 5, 15 },
            },

            // ── WEEKLY MISSIONS ─────────────────────────────────────────
            ["skip_the_scroll"] = new Quest {
                id = "skip_the_scroll", cadence = QuestCadence.Weekly,
                category = QuestCategory.Focus, xp = 50, minEnergy = EnergyLevel.Medium,
            },
            ["show_up"] = new Quest {
                id = "show_up", cadence = QuestCadence.Weekly,
                category = QuestCategory.Social, xp = 50, minEnergy = EnergyLevel.Medium,
            },
            ["pause_log"] = new Quest {
                id = "pause_log", cadence = QuestCadence.Weekly,
                category = QuestCategory.Pause, xp = 75, minEnergy = EnergyLevel.Low,
                generatesData = true,
            },
            ["save_dont_spend"] = new Quest {
                id = "save_dont_spend", cadence = QuestCadence.Weekly,
                category = QuestCategory.Finance, xp = 40, minEnergy = EnergyLevel.Low,
                generatesData = true,
            },
            ["hard_conversation"] = new Quest {
                id = "hard_conversation", cadence = QuestCadence.Weekly,
                category = QuestCategory.Social, xp = 100, minEnergy = EnergyLevel.High,
            },

            // ── FLOOR ──────────────────────────────────────────────────
            ["open_sparq"] = new Quest {
                id = "open_sparq", cadence = QuestCadence.AlwaysAvailable,
                category = QuestCategory.Floor, xp = SparqLimits.FLOOR_QUEST_XP,
                minEnergy = EnergyLevel.Low,
            },

            // ── AFTER THE STORM PACK (breakup recovery) ─────────────────
            ["ten_minute_window"] = new Quest {
                id = "ten_minute_window", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 10, minEnergy = EnergyLevel.Low,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 7, 14 },
            },
            ["dont_check"] = new Quest {
                id = "dont_check", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 15, minEnergy = EnergyLevel.Low,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 7, 14 },
            },
            ["phone_a_friend"] = new Quest {
                id = "phone_a_friend", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 10, minEnergy = EnergyLevel.Medium,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },
            ["body_before_brain"] = new Quest {
                id = "body_before_brain", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 10, minEnergy = EnergyLevel.Medium,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },
            ["one_thing_future_you"] = new Quest {
                id = "one_thing_future_you", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 10, minEnergy = EnergyLevel.Low,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },
            ["drama_check"] = new Quest {
                id = "drama_check", cadence = QuestCadence.Daily,
                category = QuestCategory.Recovery, xp = 5, minEnergy = EnergyLevel.Low,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },
            ["no_contact_week"] = new Quest {
                id = "no_contact_week", cadence = QuestCadence.Weekly,
                category = QuestCategory.Recovery, xp = 100, minEnergy = EnergyLevel.Medium,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },
            ["inventory"] = new Quest {
                id = "inventory", cadence = QuestCadence.Weekly,
                category = QuestCategory.Recovery, xp = 75, minEnergy = EnergyLevel.Low,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
                generatesData = true,
            },
            ["three_hard_things"] = new Quest {
                id = "three_hard_things", cadence = QuestCadence.Weekly,
                category = QuestCategory.Recovery, xp = 100, minEnergy = EnergyLevel.Medium,
                packId = "after_the_storm", sensitivity = Sensitivity.Sensitive,
            },

            // ── TEEN DRAMA (always-available, not pack-gated) ──────────
            ["side_step"] = new Quest {
                id = "side_step", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 },
            },
            ["mind_your_own"] = new Quest {
                id = "mind_your_own", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 10, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 },
            },
            ["receipt_test"] = new Quest {
                id = "receipt_test", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 5, minEnergy = EnergyLevel.Low,
                streakMilestones = new[] { 7, 30 },
            },

            // ── SAFETY ─────────────────────────────────────────────────
            ["talk_to_someone"] = new Quest {
                id = "talk_to_someone", cadence = QuestCadence.AlwaysAvailable,
                category = QuestCategory.Safety, xp = 25, minEnergy = EnergyLevel.Low,
                sensitivity = Sensitivity.CrisisAdjacent,
            },

            // ── CALIBRATED TRUST PACK ──────────────────────────────────
            ["words_vs_actions"] = new Quest {
                id = "words_vs_actions", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 10, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 7, 30 }, generatesData = true,
            },
            ["cost_check"] = new Quest {
                id = "cost_check", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 5, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 7, 30 },
            },
            ["gut_note"] = new Quest {
                id = "gut_note", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 5, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                generatesData = true,
            },
            ["receipt_read"] = new Quest {
                id = "receipt_read", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 10, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                generatesData = true,
            },
            ["one_question_not_three"] = new Quest {
                id = "one_question_not_three", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 5, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 14 },
            },
            ["pressure_check"] = new Quest {
                id = "pressure_check", cadence = QuestCadence.Daily,
                category = QuestCategory.SocialDrama, xp = 10, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                streakMilestones = new[] { 14 },
            },
            ["inner_circle_audit"] = new Quest {
                id = "inner_circle_audit", cadence = QuestCadence.Weekly,
                category = QuestCategory.SocialDrama, xp = 75, minEnergy = EnergyLevel.Medium,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                generatesData = true,
            },
            ["the_no_mission"] = new Quest {
                id = "the_no_mission", cadence = QuestCadence.Weekly,
                category = QuestCategory.SocialDrama, xp = 75, minEnergy = EnergyLevel.Medium,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
            },
            ["thirty_day_pattern"] = new Quest {
                id = "thirty_day_pattern", cadence = QuestCadence.Weekly,
                category = QuestCategory.SocialDrama, xp = 100, minEnergy = EnergyLevel.Low,
                packId = "calibrated_trust", sensitivity = Sensitivity.Sensitive,
                generatesData = true,
            },
        };

        // ── QUEST PACKS ─────────────────────────────────────────────────
        public static readonly Dictionary<string, QuestPack> Packs = new Dictionary<string, QuestPack>
        {
            ["after_the_storm"] = new QuestPack {
                id = "after_the_storm",
                activationMode = "user_initiated",
                durationDays = 28,
                requiresAcknowledgment = true,
                safetyCheckAfterDays = SparqLimits.RECOVERY_PACK_SAFETY_CHECK_DAYS,
                questIds = new List<string> {
                    "ten_minute_window", "dont_check", "phone_a_friend",
                    "body_before_brain", "one_thing_future_you", "drama_check",
                    "no_contact_week", "inventory", "three_hard_things",
                },
            },
            ["calibrated_trust"] = new QuestPack {
                id = "calibrated_trust",
                activationMode = "user_initiated",
                durationDays = 30,
                requiresAcknowledgment = true,
                questIds = new List<string> {
                    "words_vs_actions", "cost_check", "gut_note", "receipt_read",
                    "one_question_not_three", "pressure_check", "inner_circle_audit",
                    "the_no_mission", "thirty_day_pattern",
                },
            },
        };

        // ── ACHIEVEMENTS ───────────────────────────────────────────────
        public static readonly Dictionary<string, Achievement> Achievements = new Dictionary<string, Achievement>
        {
            ["event_horizon"]      = new Achievement { id = "event_horizon",      xp = 25,  buddyEffect = "faint_glow_24h",         oneTime = true  },
            ["slow_light"]         = new Achievement { id = "slow_light",         xp = 100, buddyEffect = "star_trail_7d",          oneTime = false },
            ["orbit_lock"]         = new Achievement { id = "orbit_lock",         xp = 150, buddyEffect = "ring_icon_permanent",    oneTime = false },
            ["dark_matter"]        = new Achievement { id = "dark_matter",        xp = 75,  buddyEffect = "sleeping_animation",     oneTime = true  },
            ["nebula"]             = new Achievement { id = "nebula",             xp = 100, buddyEffect = "nebula_profile_bg",      oneTime = true  },
            ["supernova"]          = new Achievement { id = "supernova",          xp = 250, buddyEffect = "explosion_animation",    oneTime = false },
            ["den_builder"]        = new Achievement { id = "den_builder",        xp = 125, buddyEffect = "den_scene",              oneTime = true  },
            ["foxfire"]            = new Achievement { id = "foxfire",            xp = 150, buddyEffect = "eyes_spark_48h",         oneTime = true  },

            // ADHD/autism-specific
            ["den"]                = new Achievement { id = "den",                xp = 50,  buddyEffect = "den_glow",               oneTime = true },
            ["signal_through_noise"]= new Achievement { id = "signal_through_noise", xp = 75, buddyEffect = "companion_silhouette", oneTime = true },
            ["threshold"]          = new Achievement { id = "threshold",          xp = 100, buddyEffect = "doorway_animation",      oneTime = true },
            ["wavelength"]         = new Achievement { id = "wavelength",         xp = 75,  buddyEffect = "pattern_overlay",        oneTime = true },
            ["cartographer"]       = new Achievement { id = "cartographer",       xp = 100, buddyEffect = "map_unlock",             oneTime = true },

            // After the Storm pack achievements
            ["eye_of_the_storm"]   = new Achievement { id = "eye_of_the_storm",   xp = 100, buddyEffect = "storm_calm",             oneTime = true },
            ["radio_silence"]      = new Achievement { id = "radio_silence",      xp = 150, buddyEffect = "quiet_galaxy_7d",        oneTime = true },
            ["gravity_well"]       = new Achievement { id = "gravity_well",       xp = 125, buddyEffect = "orbit_freed",            oneTime = true },
            ["new_sky"]            = new Achievement { id = "new_sky",            xp = 300, buddyEffect = "dawn_permanent_star",    oneTime = true },

            // Calibrated Trust pack achievements
            ["the_receipt"]        = new Achievement { id = "the_receipt",        xp = 50,  buddyEffect = "magnifier_glow",         oneTime = true },
            ["compass_calibration"]= new Achievement { id = "compass_calibration",xp = 100, buddyEffect = "compass_needle",         oneTime = true },
            ["held_the_line"]      = new Achievement { id = "held_the_line",      xp = 125, buddyEffect = "anchor_steady",          oneTime = true },
            ["pace_setter"]        = new Achievement { id = "pace_setter",        xp = 125, buddyEffect = "measured_stars",         oneTime = true },
            ["open_eyes"]          = new Achievement { id = "open_eyes",          xp = 200, buddyEffect = "clear_vision_permanent", oneTime = true },
        };

        // ── HELPERS ─────────────────────────────────────────────────────

        /// <summary>Returns quests appropriate for a given energy level
        /// (low surfaces the smallest set; high surfaces everything).</summary>
        public static IEnumerable<Quest> QuestsForEnergy(EnergyLevel level)
        {
            foreach (var q in Quests.Values)
                if ((int)q.minEnergy <= (int)level) yield return q;
        }

        /// <summary>Daily quest pool minus the floor quest. Optionally filter
        /// by sensitivity (default: only General — pack quests excluded).</summary>
        public static List<Quest> DailyPool(bool includeSensitive = false, string activePackId = null)
        {
            var list = new List<Quest>();
            foreach (var q in Quests.Values)
            {
                if (q.cadence != QuestCadence.Daily) continue;

                // Pack quests only when their pack is active
                if (!string.IsNullOrEmpty(q.packId))
                {
                    if (q.packId != activePackId) continue;
                }

                if (!includeSensitive && q.sensitivity != Sensitivity.General) continue;

                list.Add(q);
            }
            return list;
        }

        public static List<Quest> WeeklyPool(bool includeSensitive = false, string activePackId = null)
        {
            var list = new List<Quest>();
            foreach (var q in Quests.Values)
            {
                if (q.cadence != QuestCadence.Weekly) continue;
                if (!string.IsNullOrEmpty(q.packId) && q.packId != activePackId) continue;
                if (!includeSensitive && q.sensitivity != Sensitivity.General) continue;
                list.Add(q);
            }
            return list;
        }

        public static Quest Get(string id) =>
            (id != null && Quests.TryGetValue(id, out var q)) ? q : null;

        public static QuestPack GetPack(string id) =>
            (id != null && Packs.TryGetValue(id, out var p)) ? p : null;

        public static Achievement GetAchievement(string id) =>
            (id != null && Achievements.TryGetValue(id, out var a)) ? a : null;
    }
}
