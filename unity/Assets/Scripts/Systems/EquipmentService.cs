using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Equipment / gear system. Items have stats, rarity, and slots.
    /// Player has an inventory + equipped slots. Stats roll up into total
    /// ATK / DEF / HP that BattleScene can read.
    /// </summary>
    public static class EquipmentService
    {
        public enum Slot { Weapon, Helm, Chest, Boots, Trinket }
        public enum Rarity { Common, Rare, Epic, Legendary }

        [System.Serializable]
        public class Item
        {
            public string id;
            public string name;
            public string iconPath;
            public Slot slot;
            public Rarity rarity;
            public int atk;
            public int def;
            public int hp;
        }

        // Catalog — 50+ items across all slots, using FantasyIconPack T1/T2 art
        // (FP_ prefix) and Layer Lab Gear/Crown/Gem icons.
        private static readonly Item[] CATALOG = new[]
        {
            // ───────── Weapons (23 incl. tomes/extras below) ─────────
            new Item { id="sword_wood",     name="Wooden Sword",   iconPath="FP_SwordWood",     slot=Slot.Weapon, rarity=Rarity.Common,    atk=4 },
            new Item { id="sword_iron",     name="Iron Sword",     iconPath="FP_SwordT1",       slot=Slot.Weapon, rarity=Rarity.Common,    atk=8 },
            new Item { id="sword_steel",    name="Steel Sword",    iconPath="FP_SwordT1",       slot=Slot.Weapon, rarity=Rarity.Rare,      atk=14 },
            new Item { id="sword_runic",    name="Runic Blade",    iconPath="FP_SwordT2",       slot=Slot.Weapon, rarity=Rarity.Epic,      atk=22 },
            new Item { id="sword_legend",   name="Legend's Edge",  iconPath="FP_SwordT2",       slot=Slot.Weapon, rarity=Rarity.Legendary, atk=36, hp=10 },
            new Item { id="dagger_quick",   name="Quick Dagger",   iconPath="FP_DaggerT1",      slot=Slot.Weapon, rarity=Rarity.Common,    atk=6 },
            new Item { id="dagger_shadow",  name="Shadow Fang",    iconPath="FP_DaggerT2",      slot=Slot.Weapon, rarity=Rarity.Epic,      atk=18 },
            new Item { id="axe_wood",       name="Woodcutter Axe", iconPath="FP_AxeT1",         slot=Slot.Weapon, rarity=Rarity.Common,    atk=10 },
            new Item { id="axe_great",      name="Greataxe",       iconPath="FP_AxeT2",         slot=Slot.Weapon, rarity=Rarity.Epic,      atk=24 },
            new Item { id="axe_double",     name="Twin Cleaver",   iconPath="FP_AxeDoubleT2",   slot=Slot.Weapon, rarity=Rarity.Legendary, atk=32 },
            new Item { id="bow_hunter",     name="Hunter Bow",     iconPath="FP_BowT1",         slot=Slot.Weapon, rarity=Rarity.Rare,      atk=12 },
            new Item { id="bow_long",       name="Longbow",        iconPath="FP_BowT2",         slot=Slot.Weapon, rarity=Rarity.Epic,      atk=20 },
            new Item { id="hammer_war",     name="War Hammer",     iconPath="FP_HammerT1",      slot=Slot.Weapon, rarity=Rarity.Rare,      atk=14, def=2 },
            new Item { id="hammer_titan",   name="Titan Maul",     iconPath="FP_HammerT2",      slot=Slot.Weapon, rarity=Rarity.Legendary, atk=30, def=4 },
            new Item { id="spear_iron",     name="Iron Spear",     iconPath="FP_SpearT1",       slot=Slot.Weapon, rarity=Rarity.Rare,      atk=13 },
            new Item { id="wand_apprentice",name="Apprentice Wand",iconPath="FP_WandT1",        slot=Slot.Weapon, rarity=Rarity.Common,    atk=6, hp=4 },
            new Item { id="wand_archmage",  name="Archmage Staff", iconPath="FP_WandT2",        slot=Slot.Weapon, rarity=Rarity.Legendary, atk=28, hp=12 },

            // ───────── Helms (8 incl. extras below) ─────────
            new Item { id="helm_cloth",     name="Cloth Hood",     iconPath="FP_HelmetT1",      slot=Slot.Helm, rarity=Rarity.Common,    def=2 },
            new Item { id="helm_iron",      name="Iron Helm",      iconPath="FP_HelmetT1",      slot=Slot.Helm, rarity=Rarity.Rare,      def=5 },
            new Item { id="helm_steel",     name="Steel Helm",     iconPath="FP_HelmetT2",      slot=Slot.Helm, rarity=Rarity.Epic,      def=8 },
            new Item { id="helm_crowned",   name="Crowned Helm",   iconPath="Crown_1",          slot=Slot.Helm, rarity=Rarity.Epic,      def=8, atk=2 },
            new Item { id="helm_royal",     name="Royal Crown",    iconPath="Crown_2",          slot=Slot.Helm, rarity=Rarity.Legendary, def=12, atk=4, hp=10 },
            new Item { id="helm_dragon",    name="Dragon Helm",    iconPath="FP_HelmetT2",      slot=Slot.Helm, rarity=Rarity.Legendary, def=14, hp=15 },

            // ───────── Chest (8 incl. extras below) ─────────
            new Item { id="chest_cloth",    name="Cloth Tunic",    iconPath="FP_ArmorT1",       slot=Slot.Chest, rarity=Rarity.Common,    def=4 },
            new Item { id="chest_leather",  name="Leather Vest",   iconPath="FP_ArmorT1",       slot=Slot.Chest, rarity=Rarity.Rare,      def=8, hp=10 },
            new Item { id="chest_chain",    name="Chainmail",      iconPath="FP_ArmorT2",       slot=Slot.Chest, rarity=Rarity.Rare,      def=10, hp=8 },
            new Item { id="chest_plate",    name="Plate Armor",    iconPath="FP_ArmorT2",       slot=Slot.Chest, rarity=Rarity.Epic,      def=14, hp=20 },
            new Item { id="chest_runic",    name="Runic Plate",    iconPath="FP_ArmorT2",       slot=Slot.Chest, rarity=Rarity.Epic,      def=16, hp=18, atk=2 },
            new Item { id="chest_dragon",   name="Dragonscale",    iconPath="FP_ArmorT2",       slot=Slot.Chest, rarity=Rarity.Legendary, def=22, hp=40 },

            // ───────── Boots (8 incl. extras below) — Backpack icon stand-in ─────────
            new Item { id="boots_cloth",    name="Cloth Boots",    iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Common,    hp=5 },
            new Item { id="boots_iron",     name="Iron Greaves",   iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Rare,      hp=10, def=3 },
            new Item { id="boots_swift",    name="Swift Boots",    iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Rare,      hp=12, def=2 },
            new Item { id="boots_giant",    name="Giant Stompers", iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Epic,      hp=22, def=4 },
            new Item { id="boots_runic",    name="Runic Treads",   iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Epic,      hp=18, def=5, atk=2 },
            new Item { id="boots_dragon",   name="Dragon Stride",  iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Legendary, hp=30, def=6, atk=4 },

            // ───────── Trinkets — colored gems for visual variety ─────────
            new Item { id="ring_iron",      name="Iron Ring",      iconPath="FP_GemBlue",       slot=Slot.Trinket, rarity=Rarity.Common,    atk=2 },
            new Item { id="ring_ruby",      name="Ruby Ring",      iconPath="FP_GemRed",        slot=Slot.Trinket, rarity=Rarity.Rare,      atk=4, hp=8 },
            new Item { id="ring_sapphire",  name="Sapphire Ring",  iconPath="FP_GemBlue",       slot=Slot.Trinket, rarity=Rarity.Rare,      def=4, hp=10 },
            new Item { id="ring_emerald",   name="Emerald Ring",   iconPath="FP_GemGreen",      slot=Slot.Trinket, rarity=Rarity.Epic,      atk=6, def=2, hp=10 },
            new Item { id="ring_dragon",    name="Dragon Ring",    iconPath="FP_GemYellow",     slot=Slot.Trinket, rarity=Rarity.Legendary, atk=8, def=4, hp=15 },
            new Item { id="gem_ruby",       name="Ruby Charm",     iconPath="FP_GemRed",        slot=Slot.Trinket, rarity=Rarity.Epic,      atk=10, hp=8 },
            new Item { id="gem_sapphire",   name="Sapphire Charm", iconPath="FP_GemBlue",       slot=Slot.Trinket, rarity=Rarity.Epic,      def=8, hp=12 },
            new Item { id="gem_emerald",    name="Emerald Charm",  iconPath="FP_GemGreen",      slot=Slot.Trinket, rarity=Rarity.Epic,      hp=20, def=4 },
            new Item { id="gem_topaz",      name="Topaz Charm",    iconPath="FP_GemYellow",     slot=Slot.Trinket, rarity=Rarity.Epic,      atk=6, def=6 },
            new Item { id="amulet_token",   name="Crown Token",    iconPath="Badge_Token_Crown",slot=Slot.Trinket, rarity=Rarity.Legendary, atk=10, def=10, hp=20 },

            // ───────── Tomes — mage weapons (4) ─────────
            new Item { id="tome_apprentice",name="Apprentice Tome",iconPath="FP_TomeBlue",      slot=Slot.Weapon, rarity=Rarity.Common,    atk=5, hp=3 },
            new Item { id="tome_arcane",    name="Arcane Tome",    iconPath="FP_TomeGreen",     slot=Slot.Weapon, rarity=Rarity.Rare,      atk=11, hp=6 },
            new Item { id="tome_inferno",   name="Inferno Grimoire",iconPath="FP_TomeRed",      slot=Slot.Weapon, rarity=Rarity.Epic,      atk=19, hp=8 },
            new Item { id="tome_celestial", name="Celestial Codex", iconPath="FP_TomeYellow",   slot=Slot.Weapon, rarity=Rarity.Legendary, atk=30, hp=14 },
            // ───────── Extra weapons (3) ─────────
            new Item { id="dagger_iron",    name="Iron Dagger",    iconPath="FP_DaggerT1",      slot=Slot.Weapon, rarity=Rarity.Rare,      atk=10 },
            new Item { id="axe_hatchets",   name="Twin Hatchets",  iconPath="FP_AxeDoubleT1",   slot=Slot.Weapon, rarity=Rarity.Rare,      atk=13 },
            new Item { id="bow_elven",      name="Elven Bow",      iconPath="FP_BowT2",         slot=Slot.Weapon, rarity=Rarity.Legendary, atk=26, def=2 },

            // ───────── Extra helms (2) ─────────
            new Item { id="helm_bronze",    name="Bronze Cap",     iconPath="FP_HelmetT1",      slot=Slot.Helm, rarity=Rarity.Common,    def=3 },
            new Item { id="helm_horned",    name="Horned Helm",    iconPath="FP_HelmetT2",      slot=Slot.Helm, rarity=Rarity.Rare,      def=6, atk=2 },

            // ───────── Extra chest (2) ─────────
            new Item { id="chest_scale",    name="Scale Mail",     iconPath="FP_ArmorT1",       slot=Slot.Chest, rarity=Rarity.Rare,      def=9, hp=6 },
            new Item { id="chest_warlord",  name="Warlord Plate",  iconPath="FP_ArmorT2",       slot=Slot.Chest, rarity=Rarity.Legendary, def=20, hp=30, atk=3 },

            // ───────── Extra boots (2) ─────────
            new Item { id="boots_leather",  name="Leather Boots",  iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Common,    hp=6, def=1 },
            new Item { id="boots_phantom",  name="Phantom Steps",  iconPath="FP_Backpack",      slot=Slot.Boots, rarity=Rarity.Legendary, hp=26, def=5, atk=3 },

            // ───────── Shields — defensive trinkets (4) ─────────
            new Item { id="shield_buckler", name="Buckler",        iconPath="FP_ShieldSmallT1", slot=Slot.Trinket, rarity=Rarity.Common,    def=4 },
            new Item { id="shield_kite",    name="Kite Shield",    iconPath="FP_ShieldSmallT2", slot=Slot.Trinket, rarity=Rarity.Rare,      def=8, hp=8 },
            new Item { id="shield_tower",   name="Tower Shield",   iconPath="FP_ShieldLargeT1", slot=Slot.Trinket, rarity=Rarity.Epic,      def=14, hp=12 },
            new Item { id="shield_aegis",   name="Aegis",          iconPath="FP_ShieldLargeT2", slot=Slot.Trinket, rarity=Rarity.Legendary, def=20, hp=20, atk=2 },

            // ───────── Signature zone-boss drops (8) — one guaranteed Legendary
            // per zone, granted the first time you beat that zone's boss. Mixes
            // slots so completing the set fills your whole loadout.
            new Item { id="sig_greenwood",   name="Greenwood Greatbow",   iconPath="FP_BowT2",         slot=Slot.Weapon, rarity=Rarity.Legendary, atk=28, def=2 },
            new Item { id="sig_mossfen",     name="Mossfen Vestments",    iconPath="FP_ArmorT2",       slot=Slot.Chest,  rarity=Rarity.Legendary, def=22, hp=35 },
            new Item { id="sig_shadowmoor",  name="Shadowmoor Crown",     iconPath="Crown_2",          slot=Slot.Helm,   rarity=Rarity.Legendary, def=14, atk=4, hp=12 },
            new Item { id="sig_emberpeak",   name="Emberpeak Maul",       iconPath="FP_HammerT2",      slot=Slot.Weapon, rarity=Rarity.Legendary, atk=34, def=5 },
            new Item { id="sig_frostspire",  name="Frostspire Plate",     iconPath="FP_ArmorT2",       slot=Slot.Chest,  rarity=Rarity.Legendary, def=26, hp=45 },
            new Item { id="sig_duskwastes",  name="Dusk Wanderer's Stride",iconPath="FP_Backpack",     slot=Slot.Boots,  rarity=Rarity.Legendary, hp=32, def=7, atk=5 },
            new Item { id="sig_voidreach",   name="Voidreach Pendant",    iconPath="FP_GemYellow",     slot=Slot.Trinket,rarity=Rarity.Legendary, atk=12, def=12, hp=25 },
            new Item { id="sig_dragonspire", name="Dragonspire Greataxe", iconPath="FP_AxeT2",         slot=Slot.Weapon, rarity=Rarity.Legendary, atk=40, def=4, hp=10 },
        };

        // Player state — persisted via PlayerPrefs
        private const string KEY_OWNED    = "sparq.equip.owned";    // "id1,id2,id3..."
        private const string KEY_EQUIPPED = "sparq.equip.equipped"; // "Slot:id;Slot:id"
        private const string KEY_LEVELS   = "sparq.equip.levels";   // "id:lvl;id:lvl"

        // ── Forge (item upgrade) ─────────────────────────────────────────
        public const int MAX_LEVEL = 5;
        /// <summary>Flat +15% per level. Level 0 = base, level 5 = +75%.</summary>
        public const float LEVEL_STAT_MULT = 0.15f;
        /// <summary>Coin cost to upgrade from level N to N+1.</summary>
        public static int UpgradeCost(int currentLevel) => 100 * (currentLevel + 1);
        public static event System.Action<string, int> OnUpgraded;   // (itemId, newLevel)

        public static List<Item> Catalog => new List<Item>(CATALOG);

        public static Item ById(string id)
        {
            foreach (var c in CATALOG) if (c.id == id) return c;
            return null;
        }

        // ───────── Inventory ─────────
        public static List<string> OwnedIds()
        {
            string raw = PlayerPrefs.GetString(KEY_OWNED, "");
            if (string.IsNullOrEmpty(raw))
            {
                // Starter inventory: cloth set + iron sword
                var starter = new List<string> { "sword_iron", "helm_cloth", "chest_cloth", "boots_cloth", "ring_iron" };
                SaveOwned(starter);
                EquipBest();
                return starter;
            }
            return new List<string>(raw.Split(','));
        }

        public static List<Item> OwnedItems()
        {
            var ids = OwnedIds();
            var list = new List<Item>();
            foreach (var id in ids)
            {
                var item = ById(id);
                if (item != null) list.Add(item);
            }
            return list;
        }

        public static void Grant(string itemId)
        {
            if (ById(itemId) == null) return;
            var ids = OwnedIds();
            ids.Add(itemId);
            SaveOwned(ids);
        }

        private static void SaveOwned(List<string> ids)
        {
            PlayerPrefs.SetString(KEY_OWNED, string.Join(",", ids));
            PlayerPrefs.Save();
        }

        // ───────── Equip ─────────
        public static Dictionary<Slot, string> Equipped()
        {
            var dict = new Dictionary<Slot, string>();
            string raw = PlayerPrefs.GetString(KEY_EQUIPPED, "");
            if (string.IsNullOrEmpty(raw)) return dict;
            foreach (var seg in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var parts = seg.Split(':');
                if (parts.Length != 2) continue;
                if (System.Enum.TryParse(parts[0], out Slot s)) dict[s] = parts[1];
            }
            return dict;
        }

        public static Item EquippedIn(Slot slot)
        {
            var d = Equipped();
            if (d.TryGetValue(slot, out string id)) return ById(id);
            return null;
        }

        public static void Equip(string itemId)
        {
            var item = ById(itemId);
            if (item == null) return;
            var d = Equipped();
            d[item.slot] = itemId;
            SaveEquipped(d);
        }

        public static void Unequip(Slot slot)
        {
            var d = Equipped();
            if (d.Remove(slot)) SaveEquipped(d);
        }

        private static void SaveEquipped(Dictionary<Slot, string> d)
        {
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var kv in d)
            {
                if (!first) sb.Append(';');
                sb.Append(kv.Key).Append(':').Append(kv.Value);
                first = false;
            }
            PlayerPrefs.SetString(KEY_EQUIPPED, sb.ToString());
            PlayerPrefs.Save();
        }

        public static void EquipBest()
        {
            // Auto-equip the best item in each slot
            var owned = OwnedItems();
            var byRarity = new Dictionary<Slot, Item>();
            foreach (var i in owned)
            {
                if (!byRarity.TryGetValue(i.slot, out var cur) || (int)i.rarity > (int)cur.rarity)
                    byRarity[i.slot] = i;
            }
            foreach (var kv in byRarity) Equip(kv.Value.id);
        }

        // ───────── Forge: per-item level tracking & upgrade ─────────

        private static Dictionary<string, int> _levelsCache;

        private static Dictionary<string, int> LoadLevels()
        {
            if (_levelsCache != null) return _levelsCache;
            _levelsCache = new Dictionary<string, int>();
            string raw = PlayerPrefs.GetString(KEY_LEVELS, "");
            if (string.IsNullOrEmpty(raw)) return _levelsCache;
            foreach (var seg in raw.Split(';'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var p = seg.Split(':');
                if (p.Length != 2) continue;
                if (int.TryParse(p[1], out int lv) && lv > 0) _levelsCache[p[0]] = lv;
            }
            return _levelsCache;
        }

        private static void SaveLevels()
        {
            if (_levelsCache == null) return;
            var sb = new System.Text.StringBuilder();
            bool first = true;
            foreach (var kv in _levelsCache)
            {
                if (kv.Value <= 0) continue;
                if (!first) sb.Append(';');
                sb.Append(kv.Key).Append(':').Append(kv.Value);
                first = false;
            }
            PlayerPrefs.SetString(KEY_LEVELS, sb.ToString());
            PlayerPrefs.Save();
        }

        /// <summary>Current forge level for an item id (0..MAX_LEVEL).</summary>
        public static int LevelOf(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            var d = LoadLevels();
            return d.TryGetValue(itemId, out int lv) ? Mathf.Clamp(lv, 0, MAX_LEVEL) : 0;
        }

        /// <summary>Stats with the level multiplier applied.</summary>
        public static (int atk, int def, int hp) EffectiveStats(Item item)
        {
            if (item == null) return (0, 0, 0);
            int level = LevelOf(item.id);
            float mult = 1f + LEVEL_STAT_MULT * level;
            return (Mathf.RoundToInt(item.atk * mult),
                    Mathf.RoundToInt(item.def * mult),
                    Mathf.RoundToInt(item.hp  * mult));
        }

        /// <summary>Returns the stat deltas a single upgrade would add
        /// (current → current + 1), without performing the upgrade.</summary>
        public static (int atk, int def, int hp) UpgradeDeltas(Item item)
        {
            if (item == null) return (0, 0, 0);
            int level = LevelOf(item.id);
            if (level >= MAX_LEVEL) return (0, 0, 0);
            var cur  = EffectiveStats(item);
            float mult = 1f + LEVEL_STAT_MULT * (level + 1);
            int nAtk = Mathf.RoundToInt(item.atk * mult);
            int nDef = Mathf.RoundToInt(item.def * mult);
            int nHp  = Mathf.RoundToInt(item.hp  * mult);
            return (nAtk - cur.atk, nDef - cur.def, nHp - cur.hp);
        }

        /// <summary>True if the player has enough coins to upgrade this
        /// item AND it isn't already maxed out.</summary>
        public static bool CanUpgrade(string itemId)
        {
            int lv = LevelOf(itemId);
            if (lv >= MAX_LEVEL) return false;
            int cost = UpgradeCost(lv);
            try { return (Sparq.Core.SaveService.Data?.sparqCoins ?? 0) >= cost; }
            catch { return false; }
        }

        /// <summary>Deducts coins and bumps the level by 1. Returns true on
        /// success. No-op if maxed, not owned, or insufficient coins.</summary>
        public static bool Upgrade(string itemId)
        {
            if (ById(itemId) == null) return false;
            int lv = LevelOf(itemId);
            if (lv >= MAX_LEVEL) return false;
            int cost = UpgradeCost(lv);
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return false;
            if (data.sparqCoins < cost) return false;

            data.sparqCoins -= cost;
            var d = LoadLevels();
            d[itemId] = lv + 1;
            SaveLevels();
            try { Sparq.Core.SaveService.Save(); } catch {}
            OnUpgraded?.Invoke(itemId, lv + 1);
            return true;
        }

        // ───────── Stat comparison vs currently equipped ─────────

        /// <summary>Delta (candidate − currently equipped in the same
        /// slot) using effective (leveled) stats. If nothing is equipped
        /// in that slot, returns the candidate's full effective stats
        /// (everything is a gain).</summary>
        public static (int atk, int def, int hp) StatDeltaVsEquipped(Item candidate)
        {
            if (candidate == null) return (0, 0, 0);
            var cand = EffectiveStats(candidate);
            var eq = EquippedIn(candidate.slot);
            if (eq == null) return cand;
            var cur = EffectiveStats(eq);
            return (cand.atk - cur.atk, cand.def - cur.def, cand.hp - cur.hp);
        }

        /// <summary>+1 / 0 / -1 — overall direction of a slot swap using
        /// a simple weighted score (atk*2 + def + hp/5).</summary>
        public static int CompareToEquipped(Item candidate)
        {
            var d = StatDeltaVsEquipped(candidate);
            float score = d.atk * 2f + d.def + d.hp / 5f;
            if (score > 0.001f) return  1;
            if (score < -0.001f) return -1;
            return 0;
        }

        // ───────── Stat rollup ─────────
        public static (int atk, int def, int hp) TotalStats()
        {
            int atk = 0, def = 0, hp = 0;
            foreach (var kv in Equipped())
            {
                var item = ById(kv.Value);
                if (item == null) continue;
                var s = EffectiveStats(item);
                atk += s.atk; def += s.def; hp += s.hp;
            }
            return (atk, def, hp);
        }

        // ───────── Loot drops ─────────
        public static Item RollLoot(int playerLevel)
        {
            // Higher levels skew toward better rarity
            float roll = Random.value;
            Rarity r;
            if (roll < 0.55f) r = Rarity.Common;
            else if (roll < 0.85f) r = Rarity.Rare;
            else if (roll < 0.97f) r = Rarity.Epic;
            else r = Rarity.Legendary;

            var pool = new List<Item>();
            foreach (var c in CATALOG) if (c.rarity == r) pool.Add(c);
            if (pool.Count == 0) pool.AddRange(CATALOG);
            return pool[Random.Range(0, pool.Count)];
        }

        public static Color RarityColor(Rarity r) => r switch
        {
            Rarity.Common    => new Color(0.7f, 0.7f, 0.75f),
            Rarity.Rare      => new Color(0.45f, 0.75f, 1f),
            Rarity.Epic      => new Color(0.75f, 0.45f, 1f),
            Rarity.Legendary => new Color(1f, 0.78f, 0.22f),
            _ => Color.white,
        };
    }
}
