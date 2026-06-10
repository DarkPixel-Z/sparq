// QuestTypes.cs — ported from sparq-quests.ts (the TypeScript schema in
// the design package). Pure data + types, no UnityEngine deps so the same
// shape can be shared across runtime, editor tooling, and any future tools.
//
// See SPARQ_DESIGN_NOTES.md for the why behind the constants.
//
// Content (titles, descriptions, completion lines, streak milestone
// messages) lives in Resources/sparq-content.json and is loaded at runtime
// by Sparq.Systems.QuestContent.

using System;
using System.Collections.Generic;

namespace Sparq.Models
{
    // ── Enums ────────────────────────────────────────────────────────────

    public enum QuestCadence { Daily, Weekly, AlwaysAvailable }

    public enum QuestCategory
    {
        Pause,         // 3-Second Pause, Pause Log
        Focus,         // Phone-Off Hour, Skip the Scroll
        Reflection,    // Future Note, journaling
        Initiation,    // Hard Thing First, Two-Minute Start
        Movement,      // Move 20
        Sleep,         // Wind Down
        Social,        // Show Up, Hard Conversation, Body Doubling
        Finance,       // Save Don't Spend
        Sensory,       // Sensory Reset, Transition Buffer (autism)
        Meta,          // Spoon Check, Hyperfocus Capture
        Recovery,      // After the Storm pack — breakup/grief
        SocialDrama,   // Side Step, Mind Your Own, Receipt Test
        Safety,        // Talk to Someone — crisis-adjacent
        Floor,         // Open Sparq
    }

    public enum EnergyLevel { Low, Medium, High }

    public enum UserState
    {
        FirstTime,
        StreakActive,
        ReturningAfterBreak,
        Milestone,
        LowEnergyDay,
        Normal,
    }

    public enum Sensitivity { General, Sensitive, CrisisAdjacent }

    // ── Quest schema ─────────────────────────────────────────────────────

    [Serializable]
    public class Quest
    {
        public string id;
        public QuestCadence cadence;
        public QuestCategory category;
        public int xp;
        /// <summary>Minimum energy level required to surface this quest.
        /// Low = always shows.</summary>
        public EnergyLevel minEnergy;
        /// <summary>Streak milestones in days that unlock special Buddy reactions.</summary>
        public int[] streakMilestones;
        /// <summary>If true, this quest generates structured data beyond a timestamp.</summary>
        public bool generatesData;
        /// <summary>Optional pack ID. Pack quests are surfaced only when the pack is active.</summary>
        public string packId;
        /// <summary>Content sensitivity. Gates underage access and triggers disclaimers.</summary>
        public Sensitivity sensitivity = Sensitivity.General;
    }

    [Serializable]
    public class QuestPack
    {
        public string id;
        /// <summary>User-initiated only. Sparq never auto-activates a pack
        /// based on detected mood (see SPARQ_DESIGN_NOTES.md).</summary>
        public string activationMode = "user_initiated";
        public int durationDays;
        public List<string> questIds;
        /// <summary>If true, requires the user to acknowledge a disclaimer before activation.</summary>
        public bool requiresAcknowledgment;
        /// <summary>Auto-surface a safety quest if user has the pack active longer than this many days.</summary>
        public int safetyCheckAfterDays;
    }

    [Serializable]
    public class Achievement
    {
        public string id;
        /// <summary>XP reward on first unlock.</summary>
        public int xp;
        /// <summary>Buddy visual effect ID — references an asset/animation in the client.</summary>
        public string buddyEffect;
        /// <summary>Whether this is a one-time achievement or repeatable.</summary>
        public bool oneTime;
    }

    [Serializable]
    public class BuddyReaction
    {
        /// <summary>Matrix key: "{category}_{state}" or "{questId}_complete".</summary>
        public string key;
        /// <summary>Reference to the copy ID in sparq-content.json.</summary>
        public string copyId;
    }

    // ── Safety / compliance ──────────────────────────────────────────────

    [Serializable]
    public class UserConsent
    {
        public string tosAcceptedVersion;
        public long   tosAcceptedAtUnix;
        public string privacyAcceptedVersion;
        public long   privacyAcceptedAtUnix;
        /// <summary>Has user seen the "Sparq is not medical/mental health treatment" notice?</summary>
        public bool notTreatmentAcknowledged;

        // For users under MIN_AGE_UNRESTRICTED, parental consent record.
        public string parentalConsentMethod;   // "email_verification" | "signed_form" | "in_person"
        public long   parentalConsentGrantedAtUnix;
        public string parentalConsentParentName;
    }

    [Serializable]
    public class CrisisResource
    {
        public string region;            // "CA", "CA-MB", "US", "GLOBAL", etc.
        public string name;
        public string type;              // "call" | "text" | "chat" | "web"
        public string contact;
        public bool   alwaysAvailable;
        public string population;        // optional: "youth", "lgbtq", "indigenous"
    }

    // ── System limits ────────────────────────────────────────────────────
    // Mirror of SPARQ_LIMITS in sparq-quests.ts. DO NOT CHANGE without
    // reviewing SPARQ_DESIGN_NOTES.md.

    public static class SparqLimits
    {
        /// <summary>Hard cap on notifications per user per day. System-enforced.</summary>
        public const int MAX_NOTIFICATIONS_PER_DAY = 2;

        /// <summary>Maximum taps from home screen to complete a daily quest.</summary>
        public const int MAX_TAPS_TO_COMPLETE_DAILY = 2;

        /// <summary>Grace Days granted per week regardless of activity.</summary>
        public const int GRACE_DAYS_GRANTED_PER_WEEK = 1;

        /// <summary>Maximum Grace Days a user can hold at once.</summary>
        public const int MAX_GRACE_DAYS_BANKED = 3;

        /// <summary>XP for the floor quest (Open Sparq). Low but nonzero.</summary>
        public const int FLOOR_QUEST_XP = 2;

        /// <summary>Maximum daily quests surfaced at once. Keeps the home screen scannable.</summary>
        public const int MAX_DAILY_QUESTS_SHOWN = 5;

        /// <summary>Maximum weekly missions active at once.</summary>
        public const int MAX_WEEKLY_MISSIONS_SHOWN = 3;

        /// <summary>Minimum age for unrestricted account. Below this requires parental consent.</summary>
        public const int MIN_AGE_UNRESTRICTED = 16;

        /// <summary>Hard minimum age. Below this, account cannot be created at all.</summary>
        public const int MIN_AGE_ABSOLUTE = 13;

        /// <summary>Days a recovery pack can run before safety check quest is auto-surfaced.</summary>
        public const int RECOVERY_PACK_SAFETY_CHECK_DAYS = 42;
    }

    /// <summary>Screens that MUST display the "not treatment" disclaimer.
    /// Mirror of SCREENS_REQUIRING_DISCLAIMER in sparq-quests.ts.</summary>
    public static class SparqDisclaimerScreens
    {
        public static readonly string[] All =
        {
            "onboarding_welcome",
            "recovery_pack_activation",
            "crisis_resources",
            "reflection_entry",
            "spoon_check_history",
        };
    }
}
