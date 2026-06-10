using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Persistent player save data. Schema mirrors the WebView app state so
    /// users migrating from web keep all their progress.
    /// Stored as JSON in PlayerPrefs locally, mirrored to Firestore in the cloud.
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        // ── Identity ──────────────────────────────────────────────────────────
        public string playerName = "Sparq User";
        public string ndProfile  = "ADHD";         // ADHD | Autism | Dyslexia | Anxiety | Multiple | Prefer not to say
        public string pronouns   = "they/them";

        // ── Avatar (profile picture) ──────────────────────────────────────────
        // The player avatar drives PlayerProfileBar (top of home screen) + leaderboards,
        // friends list (when social lands), and tap-battle banners.
        //
        // Two sources, evaluated in priority order:
        //   1. customAvatarPath: a player-uploaded image stored locally
        //      (Application.persistentDataPath/avatar_<uuid>.png). Set only after
        //      moderation approves (or immediately, in Path-A local-only mode).
        //   2. avatarPresetId: a key from PresetAvatars (e.g. "knight_01", "slime_pink").
        //
        // moderationState: relevant only when customAvatarPath is set AND cloud is wired:
        //   "local"    — Path A: never goes to cloud. Only the owner sees it. Safe.
        //   "pending"  — Path B: uploaded; awaiting auto/manual review. Other players
        //                see avatarPresetId. Owner sees their own pic with a small badge.
        //   "approved" — Path B: cleared by moderation. Visible to everyone.
        //   "rejected" — Path B: blocked by moderation. customAvatarPath cleared client-side,
        //                reverts to preset. Rejection reason in moderationNote (admin only).
        public string avatarPresetId      = "default";
        public string customAvatarPath    = "";        // empty = use preset
        public string moderationState     = "local";   // "local" | "pending" | "approved" | "rejected"
        public string moderationNote      = "";        // admin-set reason if rejected (not shown to player)
        public long   avatarUpdatedAtUnix = 0;         // ms since epoch — when current avatar set

        // ── Hero Class ────────────────────────────────────────────────────────
        // Empty = not picked yet, fall back to weapon-driven detection.
        // "knight" | "archer" | "mage" | "assassin"
        public string heroClass  = "";

        // ── XP & Level ────────────────────────────────────────────────────────
        public int totalXP        = 0;
        public int currentXP      = 0;
        public int level          = 1;
        public int xpToNextLevel  = 100;

        // ── Streaks ───────────────────────────────────────────────────────────
        public int    streak        = 0;
        public int    longestStreak = 0;
        public string lastActiveDate = "";
        public int    streakShields  = 0;

        // ── Pet (Tamagotchi-style needs) ──────────────────────────────────────
        public string activePet = "karu";          // "karu" | "mochi"
        public string petName   = "Karu";
        public int    petTaps   = 0;
        // Four care meters, each 0-100. Decay over real time; player tops
        // them up via Feed / Bathe / Brush / Socialize actions. Any meter
        // sitting at 0 for petGraceSeconds → pet dies (petAlive = false).
        public int    hunger        = 100;         // food
        public int    petHygiene    = 100;         // baths
        public int    petDental     = 100;         // brushed teeth
        public int    petSocial     = 100;         // play / chat / cuddles
        public long   lastHungerTick = 0;          // legacy field — kept for migrations
        public long   petLastNeedTickUnix = 0;     // last time we applied decay
        public long   petZeroNeedSinceUnix = 0;    // when ANY meter first hit 0 (death timer)
        public bool   petAlive      = true;
        public int    petLevel      = 1;
        public int    petXP         = 0;
        // Highest evolution stage the player has been congratulated for (0..3).
        // -1 = never shown yet (first run). The lobby fires a "PET EVOLVED!"
        // celebration the first time the current stage exceeds this.
        public int    petLastShownStage = -1;
        // Care-streak system — Sims-style reward for keeping pets alive
        // and happy long-term. Streak ticks +1 each calendar day if all
        // four meters are ≥ 60 at the midnight check. Resets to 0 on
        // pet death or if any meter sat at 0 across that day.
        public int    petCareStreakDays = 0;
        public string petLastCareCheckDate = "";   // yyyy-MM-dd
        // Egg inventory — strings like "common", "rare", "epic", "legendary", "mythic".
        // Hatched into a random species weighted by rarity.
        public List<string> eggInventory = new List<string>();
        // Stage IDs where the 3 hidden mythic eggs have already been
        // collected — prevents re-collection on revisit.
        public List<string> mythicEggsFound = new List<string>();

        // ── Currency & Economy ────────────────────────────────────────────────
        public int sparqCoins = 0;

        // ── Idle / AFK rewards ─────────────────────────────────────────────────
        // Unix seconds of the last time idle (AFK) rewards were collected. The
        // squad "earns" coins + XP over real time while away (capped); claimed
        // via the AFK Rewards popup when the explore map opens.
        public long afkLastClaimUnix = 0;

        // ── World Explore progression ──────────────────────────────────────────
        // Index into WorldExplorePanel.ZONES of the furthest zone the squad has
        // reached. Each zone is a longer map with its own backdrop/tint, rising
        // enemy difficulty, and better loot. Advances when a zone's boss falls.
        public int worldZoneIndex = 0;

        // ── Ally roster ────────────────────────────────────────────────────────
        // Each zone-boss clear unlocks the corresponding ally (id matches
        // AllyRoster.ALL[zoneIndex].id). Player can swap the active one via the
        // Ally Roster panel. Default "paladin" is the starter (Greenwood unlock).
        public System.Collections.Generic.List<string> unlockedAllyIds =
            new System.Collections.Generic.List<string> { "paladin" };
        public string activeAllyId = "paladin";

        // ── Daily Login Bonus ─────────────────────────────────────────────────
        public string lastDailyBonusDate = "";   // last day claimed (yyyy-MM-dd)
        public int    dailyBonusStreak   = 0;    // consecutive days claimed (1-7, then resets)

        // ── Rivals ────────────────────────────────────────────────────────────
        public int currentRivalIndex  = 0;       // which rival in the roster
        public int rivalsDefeated     = 0;       // total rivals defeated (achievement)

        // ── Quests / Tasks ────────────────────────────────────────────────────
        public int totalTasksDone  = 0;
        public int completedToday  = 0;
        public string lastQuestResetDate = ""; // yyyy-MM-dd of last daily reset
        public List<CustomTask> customTasks = new List<CustomTask>();
        /// <summary>ID of the user-initiated quest pack currently active
        /// (e.g. "after_the_storm"). Empty when no pack is active. Packs
        /// surface their own quests on top of the daily pool.</summary>
        public string activeQuestPackId = "";
        public long   activeQuestPackStartedUnix = 0;

        // ── Journal ───────────────────────────────────────────────────────────
        public int    journalCount = 0;
        public string journalMood  = "😄";

        // ── Rival / Combat ────────────────────────────────────────────────────
        public int  currentEnemyIndex = 0;
        public int  fitchXP           = 72;         // current enemy's XP bar
        public List<string> defeatedEnemies = new List<string>();
        public int  voltBeats          = 0;
        public long voltFreezeUntil    = 0;
        public long tapBattleCooldownUntil = 0;

        // ── Achievements & Collectibles ───────────────────────────────────────
        public List<string> unlockedAchievements = new List<string>();
        public List<string> eggs = new List<string>();

        // ── Shop & Inventory ──────────────────────────────────────────────────
        public List<string> purchases = new List<string>();
        public EquippedSlots equippedItems = new EquippedSlots();
        public List<InventoryCount> potionInventory = new List<InventoryCount>();
        public List<InventoryCount> foodInventory   = new List<InventoryCount>();
        public List<InventoryCount> treatInventory  = new List<InventoryCount>();
        public List<InventoryCount> keyInventory    = new List<InventoryCount>();

        // ── Adventures ────────────────────────────────────────────────────────
        public List<string> completedAdventures = new List<string>();
        public List<AdventureProgress> adventureProgress = new List<AdventureProgress>();

        // ── Custom Reminders ──────────────────────────────────────────────────
        public List<CustomReminder> customReminders = new List<CustomReminder>();

        // ── Focus Dungeon ─────────────────────────────────────────────────────
        public int focusSessions     = 0;
        public int focusMinutesTotal = 0;

        // ── Guild ─────────────────────────────────────────────────────────────
        public string guildName        = "";   // Empty = not in a guild
        public string guildTag         = "";   // 2-4 char clan tag
        public long   guildFoundedUnix = 0;

        // ── Settings ──────────────────────────────────────────────────────────
        public bool soundEnabled      = true;
        public bool reducedMotion     = false;
        public bool onboardingComplete = false;
    }

    // ── Helper structs (need to be Serializable for JsonUtility) ─────────────
    [System.Serializable]
    public class EquippedSlots
    {
        public string hat        = "";
        public string accessory  = "";
        public string background = "";
    }

    [System.Serializable]
    public class CustomTask
    {
        public string name;
        public int    xp = 20;
        public bool   done = false;
        /// <summary>Catalog ID (from QuestCatalog) so completion can look
        /// up category / sensitivity / streak milestones / Buddy reaction.
        /// Empty for legacy tasks or user-created custom quests.</summary>
        public string questId = "";
    }

    [System.Serializable]
    public class CustomReminder
    {
        public string id;
        public string title;
        public string time;
        public string freq;          // daily | weekdays | monthly | once
        public string color;
        public bool   enabled = true;
        public int    dayOfMonth;
        public string date;
        public string startDate;
        public string endDate;
    }

    [System.Serializable]
    public class InventoryCount
    {
        public string id;
        public int    count;
    }

    [System.Serializable]
    public class AdventureProgress
    {
        public string adventureId;
        public int    stepIndex;
    }
}
