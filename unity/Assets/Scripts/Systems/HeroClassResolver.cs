using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Resolves the player's hero CLASS — and therefore which chibi sprite
    /// to display — using two paths, in order:
    ///
    ///   1. PlayerData.heroClass (set by HeroSelectPanel) — explicit pick.
    ///   2. Equipped weapon's iconPath — legacy auto-detection fallback.
    ///
    /// Each class has IDLE_000-019 + ATTACK_000-019 frame sequences in its
    /// PNG Sequences folder. Returns base path + idle/attack prefixes the
    /// caller can use to load the full animation.
    /// </summary>
    public static class HeroClassResolver
    {
        public class HeroSprite
        {
            public string idleBase;    // path prefix up to the frame number, ending in `_`
            public string attackBase;  // same for attack
            public int    idleCount;
            public int    attackCount;
            public string className;
            public string classId;     // matches PlayerData.heroClass values
        }

        public static HeroSprite Resolve()
        {
            // 1) Explicit class pick wins
            var data = Sparq.Core.SaveService.Data;
            string chosen = data != null ? (data.heroClass ?? "") : "";
            if (!string.IsNullOrEmpty(chosen))
            {
                var byClass = ResolveByClass(chosen);
                if (byClass != null) return byClass;
            }

            // 2) Weapon-driven fallback (legacy)
            var weapon = EquipmentService.EquippedIn(EquipmentService.Slot.Weapon);
            string ic = weapon != null ? weapon.iconPath ?? "" : "";

            if (Contains(ic, "Wand"))    return Mage();
            if (Contains(ic, "Bow"))     return Archer();
            if (Contains(ic, "Dagger"))  return Assassin();
            if (Contains(ic, "Axe"))     return Mercenary();
            if (Contains(ic, "Spear"))   return AmazonWarrior();
            if (Contains(ic, "Sword") || Contains(ic, "Hammer")) return Paladin();

            // No weapon equipped → default Knight T1
            return Knight();
        }

        /// <summary>
        /// Look up a class by its id (matches PlayerData.heroClass).
        /// Returns null if id is unknown.
        /// </summary>
        public static HeroSprite ResolveByClass(string classId)
        {
            switch (classId)
            {
                case "knight":   return Knight();
                case "paladin":  return Paladin();
                case "archer":   return Archer();
                case "mage":     return Mage();
                case "assassin": return Assassin();
            }
            return null;
        }

        // ── Class factories ────────────────────────────────────────────────
        private static HeroSprite Knight() => new HeroSprite {
            classId = "knight", className = "Knight",
            idleBase   = "Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__IDLE_",
            attackBase = "Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__ATTACK_",
            idleCount = 20, attackCount = 20,
        };

        private static HeroSprite Paladin() => new HeroSprite {
            classId = "paladin", className = "Paladin",
            idleBase   = "Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Idle/0_Paladin_Idle_",
            attackBase = "Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Attack/0_Paladin_Attack_",
            idleCount = 18, attackCount = 12,
        };

        private static HeroSprite Archer() => new HeroSprite {
            classId = "archer", className = "Elf Archer",
            idleBase   = "Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Idle/0_Archer_Idle_",
            attackBase = "Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Shoot/0_Archer_Shoot_",
            idleCount = 18, attackCount = 12,
        };

        private static HeroSprite Mage() => new HeroSprite {
            classId = "mage", className = "Time Keeper (Mage)",
            idleBase   = "Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Idle/0_Time_Keeper_Idle_",
            attackBase = "Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Attack/0_Time_Keeper_Attack_",
            idleCount = 18, attackCount = 12,
        };

        private static HeroSprite Assassin() => new HeroSprite {
            classId = "assassin", className = "Assassin",
            idleBase   = "Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Idle/Idle_",
            attackBase = "Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Attack/Attack_",
            idleCount = 18, attackCount = 12,
        };

        // Weapon-only fallbacks (no picker entry — only reached via weapon)
        private static HeroSprite Mercenary() => new HeroSprite {
            classId = "mercenary", className = "Mercenary",
            idleBase   = "Assets/MercenariesChibi/Mercenaries_1/PNG/PNG Sequences/Idle/0_Mercenaries_Idle_",
            attackBase = "Assets/MercenariesChibi/Mercenaries_1/PNG/PNG Sequences/Attack/0_Mercenaries_Attack_",
            idleCount = 18, attackCount = 12,
        };

        private static HeroSprite AmazonWarrior() => new HeroSprite {
            classId = "amazon", className = "Amazon Warrior",
            idleBase   = "Assets/AmazonChibi/Amazon_Warrior_1/PNG/PNG Sequences/Idle/0_Amazon_Warrior_Idle_",
            attackBase = "Assets/AmazonChibi/Amazon_Warrior_1/PNG/PNG Sequences/Attack/0_Amazon_Warrior_Attack_",
            idleCount = 18, attackCount = 12,
        };

        private static bool Contains(string s, string needle)
        {
            return !string.IsNullOrEmpty(s) && s.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
