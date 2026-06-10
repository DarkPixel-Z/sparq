using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Curated avatar library — the default option for every player and the
    /// fallback when a custom avatar is missing, pending moderation, or rejected.
    ///
    /// Why presets exist alongside custom upload:
    /// 1. Risk floor — children, accidental uploads, players who don't want to
    ///    deal with photo-picker friction. Always a safe option.
    /// 2. Fallback for Path B "pending" / "rejected" states so other players
    ///    never see a placeholder broken-image icon.
    /// 3. Brand-controlled — keep the home screen feeling like Sparq, not like
    ///    a random Discord chat.
    ///
    /// Adding new presets: drop the sprite in Resources/Avatars/, append to
    /// the ALL list, and use a stable id (don't rename — saves reference it).
    /// </summary>
    public static class PresetAvatars
    {
        [System.Serializable]
        public class Preset
        {
            public string id;          // stable id, never rename
            public string label;       // shown to player in the picker
            public string resourcePath; // path under Resources/, no extension
            public string category;    // "class" | "pet" | "vibe" | "seasonal"
            public int    minLevel;    // 0 = always available; otherwise unlock at this level
            public Preset(string id, string label, string resourcePath, string category, int minLevel = 0)
            {
                this.id = id;
                this.label = label;
                this.resourcePath = resourcePath;
                this.category = category;
                this.minLevel = minLevel;
            }
        }

        // Each preset must have a matching sprite at Assets/Resources/<resourcePath>.
        // Sprites should be square, 256x256 minimum (high-DPI), transparent background.
        public static readonly List<Preset> ALL = new List<Preset>
        {
            // Default fallback — must exist
            new Preset("default",      "Default",        "Avatars/default",       "vibe"),

            // Class-tinted (auto-selected by HeroClassResolver if no custom set)
            new Preset("knight_01",    "Knight",         "Avatars/knight_01",     "class"),
            new Preset("archer_01",    "Archer",         "Avatars/archer_01",     "class"),
            new Preset("mage_01",      "Mage",           "Avatars/mage_01",       "class"),
            new Preset("assassin_01",  "Assassin",       "Avatars/assassin_01",   "class"),

            // Pets — Karu / Mochi cameos
            new Preset("karu_pink",    "Karu",           "Avatars/karu_pink",     "pet"),
            new Preset("mochi_blue",   "Mochi",          "Avatars/mochi_blue",    "pet"),

            // Vibes (mood / aesthetic, unlocked at any level)
            new Preset("flame",        "Flame",          "Avatars/flame",         "vibe"),
            new Preset("crystal",      "Crystal",        "Avatars/crystal",       "vibe"),
            new Preset("star",         "Star",           "Avatars/star",          "vibe"),
            new Preset("heart",        "Heart",          "Avatars/heart",         "vibe"),

            // Locked tiers (unlock as the player progresses)
            new Preset("gold_crown",   "Gold Crown",     "Avatars/gold_crown",    "vibe", minLevel: 10),
            new Preset("dragon",       "Dragon Slayer",  "Avatars/dragon",        "vibe", minLevel: 25),
            new Preset("legend",       "Legend",         "Avatars/legend",        "vibe", minLevel: 50),
        };

        private static Dictionary<string, Preset> _byId;
        public static Preset GetById(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, Preset>();
                foreach (var p in ALL) _byId[p.id] = p;
            }
            return _byId.TryGetValue(id ?? "", out var found) ? found : _byId["default"];
        }

        /// <summary>Load a preset sprite at runtime. Returns null if the asset is missing.</summary>
        public static Sprite LoadSprite(string id)
        {
            var preset = GetById(id);
            return preset == null ? null : Resources.Load<Sprite>(preset.resourcePath);
        }

        /// <summary>Default preset id keyed off the player's chosen hero class.</summary>
        public static string DefaultForClass(string heroClass)
        {
            switch ((heroClass ?? "").ToLowerInvariant())
            {
                case "knight":   return "knight_01";
                case "archer":   return "archer_01";
                case "mage":     return "mage_01";
                case "assassin": return "assassin_01";
                default:         return "default";
            }
        }

        /// <summary>True if this preset is unlocked for a player at the given level.</summary>
        public static bool IsUnlocked(Preset p, int playerLevel) => p != null && playerLevel >= p.minLevel;
    }
}
