// AllyRoster.cs — the 8-strong ally collection that powers slot 3 of the squad.
//
// One ally per zone, unlocked on first zone-boss clear (paired with the
// signature legendary drop). Player can swap the active ally via the Ally
// Roster panel. SquadBattle.MakeAllyFighter reads from here so the chosen
// ally actually shows up in fights.
//
// Roster index aligns with WorldExplorePanel.ZONES — ALL[i] is the ally
// unlocked by clearing zone i. The starter "paladin" is granted from Greenwood
// so a brand-new player still has an ally in slot 3 (matches the old hardcoded
// behaviour). Past the defined list, no new allies (endless zones don't grant).

using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    public static class AllyRoster
    {
        // Leohpaz SFX paths — duplicated as plain strings so this file doesn't
        // depend on SquadBattle's private consts. Stable; one source of truth
        // would be nicer but they rarely change.
        private const string LEO       = "Assets/Leohpaz/RPG_Essentials_Free/";
        private const string SFX_SLASH = LEO + "10_Battle_SFX/22_Slash_04.wav";
        private const string SFX_FIRE  = LEO + "8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav";
        private const string SFX_WIND  = LEO + "8_Atk_Magic_SFX/25_Wind_01.wav";
        private const string SFX_EARTH = LEO + "8_Atk_Magic_SFX/30_Earth_02.wav";
        private const string SFX_THUNDER= LEO + "8_Atk_Magic_SFX/18_Thunder_02.wav";
        private const string SFX_HEAL  = LEO + "8_Buffs_Heals_SFX/02_Heal_02.wav";

        public class Ally
        {
            public string id, name, classId;
            public string idleBase, atkBase;      // PNG sequence path templates
            public int    idleCount, atkCount;
            public int    baseHp, hpPerLevel;
            public int    baseAtk, atkPerLevel;
            public int    baseDef, defPerLevel;
            public float  attackInterval;
            public float  critChance;
            // Ult
            public string ultName, ultVfx, ultSfx;
            public Color  ultColor;
            public float  ultDmgMult;
            public bool   ultIsAOE, ultIsHeal;
            // For the roster card portrait
            public string blurb;
        }

        // Order MATCHES WorldExplorePanel.ZONES so ALL[i] unlocks with zone i.
        public static readonly Ally[] ALL =
        {
            new Ally {
                id="paladin",   name="Paladin",      classId="paladin",
                idleBase="Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Idle/0_Paladin_Idle_",
                atkBase ="Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Attack/0_Paladin_Attack_",
                idleCount=8, atkCount=6,
                baseHp=95, hpPerLevel=10, baseAtk=10, atkPerLevel=2, baseDef=5, defPerLevel=1,
                attackInterval=2.0f, critChance=0.10f,
                ultName="Holy Smite", ultVfx="thunder", ultColor=new Color(1f,0.85f,0.30f),
                ultDmgMult=2.2f, ultIsAOE=true, ultSfx=SFX_THUNDER,
                blurb="Steady tank with a healing edge — the starter.",
            },
            new Ally {
                id="ranger",    name="Ranger",       classId="archer",
                idleBase="Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Idle/0_Archer_Idle_",
                atkBase ="Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Shoot/0_Archer_Shoot_",
                idleCount=8, atkCount=6,
                baseHp=85, hpPerLevel=9, baseAtk=13, atkPerLevel=2, baseDef=3, defPerLevel=1,
                attackInterval=1.9f, critChance=0.14f,
                ultName="Piercing Volley", ultVfx="wind", ultColor=new Color(0.55f,0.95f,0.55f),
                ultDmgMult=2.4f, ultIsAOE=true, ultSfx=SFX_WIND,
                blurb="Quick bowfire and crit-leaning damage.",
            },
            new Ally {
                id="shadowblade",name="Shadowblade", classId="assassin",
                idleBase="Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Idle/Idle_",
                atkBase ="Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Attack/Attack_",
                idleCount=8, atkCount=6,
                baseHp=80, hpPerLevel=8, baseAtk=15, atkPerLevel=2, baseDef=3, defPerLevel=1,
                attackInterval=1.7f, critChance=0.18f,
                ultName="Shadow Strike", ultVfx="slash", ultColor=new Color(0.55f,0.30f,0.65f),
                ultDmgMult=2.8f, ultIsAOE=false, ultSfx=SFX_SLASH,
                blurb="Glass-cannon dagger crits; targets one foe HARD.",
            },
            new Ally {
                id="pyromancer", name="Pyromancer",  classId="mage",
                idleBase="Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Idle/0_Time_Keeper_Idle_",
                atkBase ="Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Attack/0_Time_Keeper_Attack_",
                idleCount=8, atkCount=6,
                baseHp=90, hpPerLevel=9, baseAtk=14, atkPerLevel=2, baseDef=4, defPerLevel=1,
                attackInterval=2.0f, critChance=0.12f,
                ultName="Meteor", ultVfx="fire", ultColor=new Color(1f,0.45f,0.20f),
                ultDmgMult=2.6f, ultIsAOE=true, ultSfx=SFX_FIRE,
                blurb="Fireballs and an AoE meteor ult.",
            },
            new Ally {
                id="skald",      name="Skald",       classId="knight",
                idleBase="Assets/SamuraiChibi/Samurai_1/PNG/PNG Sequences/Idle/0_Samurai_Idle_",
                atkBase ="Assets/SamuraiChibi/Samurai_1/PNG/PNG Sequences/Run Slashing/0_Samurai_Run Slashing_",
                idleCount=8, atkCount=6,
                baseHp=100, hpPerLevel=11, baseAtk=12, atkPerLevel=2, baseDef=6, defPerLevel=1,
                attackInterval=2.1f, critChance=0.10f,
                ultName="Frost Cleave", ultVfx="slash", ultColor=new Color(0.55f,0.85f,1f),
                ultDmgMult=2.5f, ultIsAOE=true, ultSfx=SFX_SLASH,
                blurb="Heavy frontliner. Slow swings, big hits.",
            },
            new Ally {
                id="sandstalker",name="Sand Stalker",classId="assassin",
                idleBase="Assets/PersianWarriorChibi/Persian_and_Arab_Warriors_1/PNG/PNG Sequences/Idle/0_Persian_and_Arab_Warriors_Idle_",
                atkBase ="Assets/PersianWarriorChibi/Persian_and_Arab_Warriors_1/PNG/PNG Sequences/Run Slashing/0_Persian_and_Arab_Warriors_Run Slashing_",
                idleCount=8, atkCount=6,
                baseHp=95, hpPerLevel=10, baseAtk=14, atkPerLevel=2, baseDef=5, defPerLevel=1,
                attackInterval=1.9f, critChance=0.13f,
                ultName="Sand Storm", ultVfx="earth", ultColor=new Color(0.95f,0.78f,0.45f),
                ultDmgMult=2.5f, ultIsAOE=true, ultSfx=SFX_EARTH,
                blurb="Balanced desert warrior, AoE earth ult.",
            },
            new Ally {
                id="sorceress",  name="Sorceress",   classId="mage",
                idleBase="Assets/MedusaChibi/Medusa_1/PNG/PNG Sequences/Idle/0_Medusa_Idle_",
                atkBase ="Assets/MedusaChibi/Medusa_1/PNG/PNG Sequences/Run Slashing/0_Medusa_Run Slashing_",
                idleCount=8, atkCount=6,
                baseHp=92, hpPerLevel=9, baseAtk=15, atkPerLevel=2, baseDef=4, defPerLevel=1,
                attackInterval=2.0f, critChance=0.14f,
                ultName="Hex Wave", ultVfx="thunder", ultColor=new Color(0.72f,0.55f,1f),
                ultDmgMult=2.7f, ultIsAOE=true, ultSfx=SFX_THUNDER,
                blurb="Arcane caster with hex-tinted AoE.",
            },
            new Ally {
                id="dragonslayer",name="Dragonslayer",classId="knight",
                idleBase="Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__IDLE_",
                atkBase ="Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__ATTACK_",
                idleCount=8, atkCount=6,
                baseHp=110, hpPerLevel=12, baseAtk=16, atkPerLevel=2, baseDef=7, defPerLevel=1,
                attackInterval=2.0f, critChance=0.14f,
                ultName="Greatsword Cleave", ultVfx="slash", ultColor=new Color(1f,0.55f,0.30f),
                ultDmgMult=3.0f, ultIsAOE=true, ultSfx=SFX_SLASH,
                blurb="Endgame slayer — top-tier hp + atk.",
            },
        };

        public static Ally ById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var a in ALL) if (a.id == id) return a;
            return null;
        }

        /// <summary>Roster index (0..7) for an ally id, or -1 if unknown.</summary>
        public static int ZoneIndexOf(string id)
        {
            for (int i = 0; i < ALL.Length; i++) if (ALL[i].id == id) return i;
            return -1;
        }

        public static List<string> UnlockedIds()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return new List<string>();
            if (d.unlockedAllyIds == null) d.unlockedAllyIds = new List<string>();
            // First-ever player or wiped save → guarantee the starter is in.
            if (d.unlockedAllyIds.Count == 0) d.unlockedAllyIds.Add("paladin");
            return d.unlockedAllyIds;
        }

        public static bool IsUnlocked(string id) => UnlockedIds().Contains(id);

        public static Ally Active()
        {
            var d = Sparq.Core.SaveService.Data;
            string id = (d != null && !string.IsNullOrEmpty(d.activeAllyId)) ? d.activeAllyId : "paladin";
            // Safety: if the saved active ally isn't unlocked (shouldn't happen,
            // but guard against tampered/old saves), fall back to the starter.
            if (!IsUnlocked(id)) id = "paladin";
            return ById(id) ?? ALL[0];
        }

        public static void SetActive(string id)
        {
            if (!IsUnlocked(id)) return;
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            d.activeAllyId = id;
            try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
        }

        /// <summary>Returns true if this id was NEWLY unlocked (caller can celebrate).</summary>
        public static bool TryUnlock(string id)
        {
            if (string.IsNullOrEmpty(id) || ById(id) == null) return false;
            var ids = UnlockedIds();
            if (ids.Contains(id)) return false;
            ids.Add(id);
            try { Sparq.Core.SaveService.ScheduleSave(); } catch {}
            return true;
        }

        /// <summary>Ally id granted by clearing zone `zoneIndex` (0..7), or null.</summary>
        public static string AllyForZone(int zoneIndex)
        {
            if (zoneIndex < 0 || zoneIndex >= ALL.Length) return null;
            return ALL[zoneIndex].id;
        }

        /// <summary>
        /// Backfill: if the player already cleared zones BEFORE this system
        /// existed (worldZoneIndex > 0 but roster never unlocked them), grant
        /// the allies they earned retroactively. Idempotent — safe to call on
        /// every lobby open.
        /// </summary>
        public static void BackfillFromZoneProgress()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            int furthest = Mathf.Clamp(d.worldZoneIndex, 0, ALL.Length);
            // worldZoneIndex N means they cleared zones 0..N-1 (and are in N).
            for (int i = 0; i < furthest; i++) TryUnlock(ALL[i].id);
        }
    }
}
