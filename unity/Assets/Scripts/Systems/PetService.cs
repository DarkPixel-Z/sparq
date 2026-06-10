using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Pet companion system. One active pet at a time (Finch-style), but
    /// player owns a roster they can swap, sell, or feed.
    /// Each pet has 3 gear slots (Hat / Body / Trinket) that grant stat bonuses.
    /// Persisted via PlayerPrefs.
    /// </summary>
    public static class PetService
    {
        public enum Slot { Hat, Body, Trinket }

        // ───────── Catalog: species + items ─────────
        public class Species
        {
            public string id, name, blurb;
            public Color  tint;
            public int    cost;        // gold to adopt
            public int    sellValue;   // gold returned when sold
            public int    baseAtk, baseDef, baseHp;
            public string spritePath;  // path to monster pack sprite
            public string rarity;      // Common, Rare, Epic, Legendary
            public string element;     // Air, Fire, Water, Earth, Storm, Mystic
        }

        public class Item
        {
            public string id, name;
            public Slot   slot;
            public int    cost;
            public int    sellValue;
            public int    atk, def, hp;
            public Color  tint;
        }

        // Catalog of 26 species across 5 rarity tiers.
        // Mythic pets are deliberately expensive — true status symbols.
        private const string MP  = "Assets/2D Fantasy Monster Sprite Pack/Monsters/";
        private const string AZW = "Assets/Azure Will-o’-Wisp/Azure Will-o’-Wisp.png";   // curly apostrophe
        private const string BAT = "Assets/ArtApex Studio/BATTY BUDDIES/Bat_";
        public static readonly Species[] CATALOG = new[]
        {
            // ── COMMON (100g, free starter) ────────────────────────────────
            // Drip — the starter pet (renamed from Wisp so the name matches the droplet sprite)
            new Species { id="wisp",     name="Drip",     blurb="Bouncy droplet companion", tint=new Color(0.95f, 0.45f, 0.55f), cost=0,     sellValue=0,    baseAtk=4,  baseDef=3,  baseHp=18, spritePath=MP+"Droplet/Sweet-Droplet.png",       rarity="Common",    element="Mystic"},
            new Species { id="pebble",   name="Pebble",   blurb="Tiny earth bug",           tint=new Color(0.65f, 0.55f, 0.35f), cost=100,   sellValue=30,   baseAtk=3,  baseDef=4,  baseHp=14, spritePath=MP+"Beetle/Bush-Beetle.png",         rarity="Common",    element="Earth" },
            new Species { id="drip",     name="Drip",     blurb="Sweet water droplet",      tint=new Color(0.55f, 0.80f, 1.0f),  cost=100,   sellValue=30,   baseAtk=3,  baseDef=3,  baseHp=12, spritePath=MP+"Droplet/Sweet-Droplet.png",      rarity="Common",    element="Water" },
            new Species { id="fluff",    name="Fluff",    blurb="Fluffy chick friend",      tint=new Color(1.0f, 0.92f, 0.65f),  cost=100,   sellValue=30,   baseAtk=4,  baseDef=2,  baseHp=11, spritePath=MP+"Chick/Fluffy-Chick.png",         rarity="Common",    element="Air"   },
            // Batty upgraded to Bat_01 from BATTY BUDDIES (animated buddy)
            new Species { id="batty",    name="Batty",    blurb="Plucky little flier",      tint=new Color(0.85f, 0.75f, 0.55f), cost=100,   sellValue=30,   baseAtk=4,  baseDef=2,  baseHp=12, spritePath=BAT+"01/PNG Sequence/Idle/00_Idle.png", rarity="Common",  element="Air"   },

            // ── RARE (300g) ────────────────────────────────────────────────
            new Species { id="ember",    name="Ember",    blurb="Tiny fire chick",          tint=new Color(1.0f, 0.55f, 0.30f),  cost=300,   sellValue=100,  baseAtk=7,  baseDef=3,  baseHp=15, spritePath=MP+"Chick/Orange-Chick.png",         rarity="Rare",      element="Fire"  },
            new Species { id="frost",    name="Frost",    blurb="Frozen polar pup",         tint=new Color(0.55f, 0.85f, 1.0f),  cost=300,   sellValue=100,  baseAtk=4,  baseDef=7,  baseHp=20, spritePath=MP+"Bear/Polar-Bear.png",            rarity="Rare",      element="Ice"   },
            new Species { id="sprout",   name="Sprout",   blurb="Mossy emerald seedling",   tint=new Color(0.40f, 0.85f, 0.45f), cost=300,   sellValue=100,  baseAtk=5,  baseDef=6,  baseHp=22, spritePath=MP+"Beetle/Emerald-Beetle.png",      rarity="Rare",      element="Earth" },
            new Species { id="alley",    name="Alley",    blurb="Street-smart feline",      tint=new Color(0.55f, 0.50f, 0.45f), cost=300,   sellValue=100,  baseAtk=7,  baseDef=4,  baseHp=15, spritePath=MP+"Feline/Street-Feline.png",       rarity="Rare",      element="Air"   },
            new Species { id="miner",    name="Miner",    blurb="Stout digging gnome",      tint=new Color(0.75f, 0.60f, 0.40f), cost=300,   sellValue=100,  baseAtk=5,  baseDef=8,  baseHp=20, spritePath=MP+"Gnome/Miner-Gnome.png",          rarity="Rare",      element="Earth" },
            new Species { id="brawler",  name="Brawler",  blurb="Pint-sized fighter",       tint=new Color(0.85f, 0.55f, 0.35f), cost=300,   sellValue=100,  baseAtk=8,  baseDef=4,  baseHp=16, spritePath=MP+"Brawler/Brigading-Brawler.png",  rarity="Rare",      element="Fighter"},
            new Species { id="hen",      name="Hen",      blurb="Furious henling",          tint=new Color(0.95f, 0.70f, 0.55f), cost=300,   sellValue=100,  baseAtk=6,  baseDef=5,  baseHp=18, spritePath=MP+"Hen/Furious-Hen.png",            rarity="Rare",      element="Fighter"},
            // BATTY BUDDIES Bat_02 — Rare shadow bat
            new Species { id="bat_shadow",name="Shade",   blurb="Wisp of midnight",         tint=new Color(0.45f, 0.35f, 0.65f), cost=300,   sellValue=100,  baseAtk=8,  baseDef=4,  baseHp=15, spritePath=BAT+"02/PNG Sequence/Idle/00_Idle.png", rarity="Rare",  element="Dark"  },

            // ── EPIC (1,000g) ──────────────────────────────────────────────
            new Species { id="zappy",    name="Zappy",    blurb="Storm-cloud cub",          tint=new Color(1.0f, 0.92f, 0.30f),  cost=1000,  sellValue=350,  baseAtk=10, baseDef=4,  baseHp=20, spritePath=MP+"Cloud/Electric-Cloud.png",       rarity="Epic",      element="Storm" },
            new Species { id="burner",   name="Burner",   blurb="Pyrokinetic brute",        tint=new Color(0.95f, 0.35f, 0.25f), cost=1000,  sellValue=350,  baseAtk=12, baseDef=5,  baseHp=22, spritePath=MP+"Brute/Pyro-Brute.png",            rarity="Epic",      element="Fire"  },
            new Species { id="tide",     name="Tide",     blurb="Watery undersea ghoul",    tint=new Color(0.30f, 0.75f, 0.85f), cost=1000,  sellValue=350,  baseAtk=9,  baseDef=9,  baseHp=24, spritePath=MP+"Ghoul/Aquatic-Ghoul.png",        rarity="Epic",      element="Water" },
            new Species { id="hound",    name="Hound",    blurb="Pyro hellhound",           tint=new Color(0.85f, 0.40f, 0.30f), cost=1000,  sellValue=350,  baseAtk=11, baseDef=6,  baseHp=22, spritePath=MP+"Hellhound/Pyro-Hellhound.png",   rarity="Epic",      element="Fire"  },
            new Species { id="frosty",   name="Frosty",   blurb="Frosty gnome shaman",      tint=new Color(0.55f, 0.85f, 0.95f), cost=1000,  sellValue=350,  baseAtk=8,  baseDef=10, baseHp=24, spritePath=MP+"Gnome/Frosty-Gnome.png",         rarity="Epic",      element="Ice"   },
            new Species { id="haunt",    name="Haunt",    blurb="Grinning spectre",         tint=new Color(0.65f, 0.50f, 0.85f), cost=1000,  sellValue=350,  baseAtk=11, baseDef=7,  baseHp=20, spritePath=MP+"Haunt/Grining-Haunt.png",        rarity="Epic",      element="Mystic"},
            new Species { id="jelly",    name="Jelly",    blurb="Crystal jellyfish",        tint=new Color(0.55f, 0.90f, 0.85f), cost=1000,  sellValue=350,  baseAtk=9,  baseDef=10, baseHp=22, spritePath=MP+"Jellyfish/Crystal-Jellyfish.png",rarity="Epic",      element="Water" },
            // BATTY BUDDIES Bat_03 — Epic storm bat
            new Species { id="bat_storm",name="Volt",     blurb="Carries lightning aloft",  tint=new Color(0.95f, 0.85f, 0.30f), cost=1000,  sellValue=350,  baseAtk=12, baseDef=5,  baseHp=22, spritePath=BAT+"03/PNG Sequence/Idle/00_Idle.png", rarity="Epic",      element="Storm" },

            // ── LEGENDARY (3,500g) ─────────────────────────────────────────
            new Species { id="moon",     name="Moon",     blurb="Gliding mystic demon",     tint=new Color(0.62f, 0.55f, 0.95f), cost=3500,  sellValue=1200, baseAtk=13, baseDef=11, baseHp=30, spritePath=MP+"Demon/Gliding-Demon.png",        rarity="Legendary", element="Mystic"},
            new Species { id="griffin",  name="Griffin",  blurb="Pyro griffin hunter",      tint=new Color(0.95f, 0.65f, 0.30f), cost=3500,  sellValue=1200, baseAtk=15, baseDef=9,  baseHp=28, spritePath=MP+"Griffin/Pyro-Griffin.png",       rarity="Legendary", element="Fire"  },
            new Species { id="bones",    name="Bones",    blurb="Skeletal warrior",         tint=new Color(0.85f, 0.85f, 0.85f), cost=3500,  sellValue=1200, baseAtk=14, baseDef=12, baseHp=30, spritePath=MP+"Bones/Skeleton-Bones.png",       rarity="Legendary", element="Dark"  },
            new Species { id="angler",   name="Angler",   blurb="Hungry deep-sea angler",   tint=new Color(0.30f, 0.50f, 0.75f), cost=3500,  sellValue=1200, baseAtk=16, baseDef=8,  baseHp=28, spritePath=MP+"Angler/Hungry-Angler.png",       rarity="Legendary", element="Water" },
            new Species { id="eclipse",  name="Eclipse",  blurb="Eclipse dragonling",       tint=new Color(0.45f, 0.30f, 0.75f), cost=3500,  sellValue=1200, baseAtk=18, baseDef=10, baseHp=30, spritePath=MP+"Dragon/Eclipse-Dragon.png",      rarity="Legendary", element="Mystic"},
            new Species { id="shadowbrute", name="Umbra", blurb="Shadow brute",             tint=new Color(0.30f, 0.20f, 0.40f), cost=3500,  sellValue=1200, baseAtk=14, baseDef=14, baseHp=32, spritePath=MP+"Brute/Shadow-Brute.png",          rarity="Legendary", element="Dark"  },
            // BATTY BUDDIES Bat_04 — Legendary void bat
            new Species { id="bat_void", name="Eclipse",  blurb="Wing tips fade to night",  tint=new Color(0.30f, 0.18f, 0.45f), cost=3500,  sellValue=1200, baseAtk=16, baseDef=11, baseHp=30, spritePath=BAT+"04/PNG Sequence/Idle/00_Idle.png", rarity="Legendary", element="Mystic"},

            // ── MYTHIC (10,000g) — true elite ──────────────────────────────
            new Species { id="dragonflare", name="Dragonflare", blurb="Heart of the volcano", tint=new Color(1.0f, 0.50f, 0.10f),  cost=10000, sellValue=4000, baseAtk=25, baseDef=15, baseHp=45, spritePath=MP+"Dragonflare/Dragonflare.png",   rarity="Mythic",    element="Inferno"},
            new Species { id="reddragon",   name="Pyrion",      blurb="Crimson sky tyrant",   tint=new Color(0.95f, 0.20f, 0.20f), cost=10000, sellValue=4000, baseAtk=24, baseDef=14, baseHp=42, spritePath=MP+"Dragon/Red-Dragon.png",         rarity="Mythic",    element="Fire"   },
            new Species { id="greendragon", name="Verdane",     blurb="Verdant ancient",      tint=new Color(0.30f, 0.75f, 0.40f), cost=10000, sellValue=4000, baseAtk=22, baseDef=16, baseHp=44, spritePath=MP+"Dragon/Green-Dragon.png",       rarity="Mythic",    element="Nature" },
            new Species { id="purpledragon",name="Twilight",    blurb="Twilight wyrm",        tint=new Color(0.65f, 0.35f, 0.85f), cost=10000, sellValue=4000, baseAtk=23, baseDef=15, baseHp=45, spritePath=MP+"Dragon/Purple-Dragon.png",      rarity="Mythic",    element="Mystic" },
            // BATTY BUDDIES Bat_05 — Mythic celestial bat (apex bat companion)
            new Species { id="bat_celestial",name="Aurora",   blurb="Sings the dawn into being", tint=new Color(0.95f, 0.75f, 1.0f), cost=10000, sellValue=4000, baseAtk=22, baseDef=16, baseHp=44, spritePath=BAT+"05/PNG Sequence/Idle/00_Idle.png", rarity="Mythic",  element="Celestial"},
        };

        public static readonly Item[] ITEMS = new[]
        {
            new Item { id="hat_crown",   name="Tiny Crown",   slot=Slot.Hat,     cost=120, sellValue=48,  atk=0, def=2, hp=4,  tint=new Color(1f, 0.82f, 0.30f) },
            new Item { id="hat_witch",   name="Witch Hat",    slot=Slot.Hat,     cost=180, sellValue=72,  atk=2, def=0, hp=2,  tint=new Color(0.62f, 0.40f, 0.92f) },
            new Item { id="hat_helm",    name="Steel Helm",   slot=Slot.Hat,     cost=260, sellValue=104, atk=0, def=4, hp=6,  tint=new Color(0.65f, 0.70f, 0.80f) },
            new Item { id="body_cape",   name="Hero Cape",    slot=Slot.Body,    cost=200, sellValue=80,  atk=2, def=2, hp=4,  tint=new Color(0.92f, 0.20f, 0.50f) },
            new Item { id="body_robe",   name="Mage Robe",    slot=Slot.Body,    cost=300, sellValue=120, atk=4, def=1, hp=4,  tint=new Color(0.30f, 0.55f, 0.85f) },
            new Item { id="body_armor",  name="Plate Armor",  slot=Slot.Body,    cost=400, sellValue=160, atk=0, def=8, hp=8,  tint=new Color(0.55f, 0.60f, 0.70f) },
            new Item { id="trin_ribbon", name="Lucky Ribbon", slot=Slot.Trinket, cost=80,  sellValue=32,  atk=1, def=1, hp=2,  tint=new Color(1f, 0.55f, 0.75f) },
            new Item { id="trin_amulet", name="Star Amulet",  slot=Slot.Trinket, cost=240, sellValue=96,  atk=2, def=2, hp=4,  tint=new Color(1f, 0.92f, 0.55f) },
            new Item { id="trin_orb",    name="Spirit Orb",   slot=Slot.Trinket, cost=360, sellValue=144, atk=4, def=4, hp=6,  tint=new Color(0.55f, 0.95f, 0.85f) },
        };

        public static Species FindSpecies(string id)
        {
            foreach (var s in CATALOG) if (s.id == id) return s;
            return CATALOG[0];
        }
        public static Item FindItem(string id)
        {
            foreach (var i in ITEMS) if (i.id == id) return i;
            return null;
        }

        // ───────── Food catalog ─────────
        public class Food
        {
            public string id, name;
            public int    cost;
            public int    hungerRestore;
            public Color  tint;
            public string spritePath;
        }

        public static readonly Food[] FOODS = new[]
        {
            new Food { id="berry",  name="Berry",  cost=5,   hungerRestore=10, tint=new Color(0.95f, 0.30f, 0.45f), spritePath="Assets/FantasyIconPack/256/HeartFull.png" },
            new Food { id="apple",  name="Apple",  cost=15,  hungerRestore=22, tint=new Color(0.95f, 0.45f, 0.30f), spritePath="Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_IconMisc/Icon_Logo_Apple.png" },
            new Food { id="bread",  name="Bread",  cost=30,  hungerRestore=40, tint=new Color(0.95f, 0.78f, 0.50f), spritePath="" },
            new Food { id="meat",   name="Meat",   cost=60,  hungerRestore=70, tint=new Color(0.85f, 0.40f, 0.35f), spritePath="Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Meat.png" },
            new Food { id="treat",  name="Royal Treat", cost=120, hungerRestore=100, tint=new Color(1f, 0.82f, 0.30f), spritePath="Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Crown_1.png" },
        };

        public static Food FindFood(string id)
        {
            foreach (var f in FOODS) if (f.id == id) return f;
            return null;
        }

        // Per-food owned count
        private const string KEY_FOOD = "sparq.pets.food.v1";
        private static Dictionary<string, int> _foodCounts;
        public static Dictionary<string, int> FoodCounts()
        {
            if (_foodCounts != null) return _foodCounts;
            _foodCounts = new Dictionary<string, int>();
            string raw = PlayerPrefs.GetString(KEY_FOOD, "");
            if (!string.IsNullOrEmpty(raw))
            {
                foreach (var seg in raw.Split(','))
                {
                    var p = seg.Split(':');
                    if (p.Length == 2 && int.TryParse(p[1], out int n)) _foodCounts[p[0]] = n;
                }
            }
            // First launch — seed with 3 berries so player can try Feed immediately
            if (_foodCounts.Count == 0) { _foodCounts["berry"] = 3; SaveFood(); }
            return _foodCounts;
        }
        public static int FoodCount(string id) { FoodCounts().TryGetValue(id, out int n); return n; }
        public static int TotalFoodCount()
        {
            int sum = 0;
            foreach (var kv in FoodCounts()) sum += kv.Value;
            return sum;
        }

        public static bool BuyFood(string foodId, int quantity = 1)
        {
            var f = FindFood(foodId);
            if (f == null) return false;
            int total = f.cost * quantity;
            if (Coins() < total) return false;
            SpendCoins(total);
            var counts = FoodCounts();
            counts[foodId] = (counts.TryGetValue(foodId, out int n) ? n : 0) + quantity;
            SaveFood(); OnChanged?.Invoke();
            return true;
        }

        public static bool ConsumeFood(string foodId)
        {
            var counts = FoodCounts();
            if (!counts.TryGetValue(foodId, out int n) || n <= 0) return false;
            var f = FindFood(foodId); if (f == null) return false;
            var p = Active(); if (p == null) return false;

            counts[foodId] = n - 1;
            if (counts[foodId] <= 0) counts.Remove(foodId);
            p.hunger = Mathf.Min(100, p.hunger + f.hungerRestore);
            // If well-fed, level up chance
            if (p.hunger >= 100 && p.level < 25 && UnityEngine.Random.value < 0.45f) p.level++;
            SaveFood(); SaveRoster(); OnChanged?.Invoke();
            return true;
        }

        // Random food drop — call from quest completion or battle victory
        public static string RollFoodDrop()
        {
            // Common foods drop more often
            float r = UnityEngine.Random.value;
            string id = r < 0.55f ? "berry"
                      : r < 0.85f ? "apple"
                      : r < 0.95f ? "bread"
                      : r < 0.99f ? "meat"
                      : "treat";
            var counts = FoodCounts();
            counts[id] = (counts.TryGetValue(id, out int n) ? n : 0) + 1;
            SaveFood(); OnChanged?.Invoke();
            return id;
        }

        private static void SaveFood()
        {
            var sb = new System.Text.StringBuilder();
            int i = 0;
            foreach (var kv in _foodCounts)
            {
                if (kv.Value <= 0) continue;
                if (i > 0) sb.Append(',');
                sb.Append(kv.Key).Append(':').Append(kv.Value);
                i++;
            }
            PlayerPrefs.SetString(KEY_FOOD, sb.ToString());
            PlayerPrefs.Save();
        }

        // ───────── Per-pet save struct ─────────
        [Serializable]
        public class Pet
        {
            public string instanceId;
            public string speciesId;
            public string nickname;
            public int    level   = 1;
            public int    hunger  = 80;     // 0-100 (100 = full)
            public string hatId   = "";
            public string bodyId  = "";
            public string trinkId = "";
        }

        public static event Action OnChanged;

        // ─────────────────────────────────────────────────────────────────
        // TAMAGOTCHI CARE — four needs (Food / Hygiene / Dental / Social)
        // decay over real time; if ANY meter sits at 0 for GRACE_SECONDS
        // (default 24h), the pet dies and must be revived or replaced.
        // ─────────────────────────────────────────────────────────────────

        public enum Need { Food, Hygiene, Dental, Social }

        /// <summary>How many points each need loses per real-world hour.</summary>
        public static readonly Dictionary<Need, float> DECAY_PER_HOUR = new Dictionary<Need, float>
        {
            { Need.Food,    4.0f },   // ~25h to empty
            { Need.Hygiene, 2.5f },   // ~40h
            { Need.Dental,  1.5f },   // ~66h
            { Need.Social,  3.0f },   // ~33h
        };

        /// <summary>How long a meter can stay at 0 before the pet dies.</summary>
        public const float GRACE_SECONDS = 24f * 3600f;

        public static int NeedLevel(Need n)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return 0;
            switch (n)
            {
                case Need.Food:    return d.hunger;
                case Need.Hygiene: return d.petHygiene;
                case Need.Dental:  return d.petDental;
                case Need.Social:  return d.petSocial;
            }
            return 0;
        }

        private static void SetNeed(Sparq.Core.PlayerData d, Need n, int value)
        {
            value = Mathf.Clamp(value, 0, 100);
            switch (n)
            {
                case Need.Food:    d.hunger     = value; break;
                case Need.Hygiene: d.petHygiene = value; break;
                case Need.Dental:  d.petDental  = value; break;
                case Need.Social:  d.petSocial  = value; break;
            }
        }

        /// <summary>Decay all four needs by the real time elapsed since the
        /// last tick. Called periodically by PetCareRunner and on each
        /// PetPanel.Show() so users returning to the app see the right state.</summary>
        public static void TickNeeds()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            if (!d.petAlive) return;   // dead pets don't decay further

            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (d.petLastNeedTickUnix <= 0) { d.petLastNeedTickUnix = nowUnix; return; }

            long elapsed = nowUnix - d.petLastNeedTickUnix;
            if (elapsed <= 0) return;
            d.petLastNeedTickUnix = nowUnix;
            float hours = elapsed / 3600f;

            foreach (var kv in DECAY_PER_HOUR)
            {
                int cur = NeedLevel(kv.Key);
                int next = Mathf.Max(0, Mathf.RoundToInt(cur - kv.Value * hours));
                SetNeed(d, kv.Key, next);
            }

            // Death-timer bookkeeping — track when ANY need first hit 0.
            bool anyZero = NeedLevel(Need.Food) == 0 || NeedLevel(Need.Hygiene) == 0
                        || NeedLevel(Need.Dental) == 0 || NeedLevel(Need.Social) == 0;
            if (anyZero)
            {
                if (d.petZeroNeedSinceUnix <= 0) d.petZeroNeedSinceUnix = nowUnix;
                else if (nowUnix - d.petZeroNeedSinceUnix >= GRACE_SECONDS)
                {
                    d.petAlive = false;
                    Debug.Log($"[PetService] {d.petName} has passed on — needs were neglected for {GRACE_SECONDS / 3600f}h.");
                }
            }
            else
            {
                d.petZeroNeedSinceUnix = 0;   // reset timer if all needs back above 0
            }

            try { Sparq.Core.SaveService.Save(); } catch {}
            OnChanged?.Invoke();
        }

        // ── Care actions ────────────────────────────────────────────────

        /// <summary>Feed a specific food item. Returns true on success.</summary>
        public static bool Feed(string foodId)
        {
            if (!ConsumeFood(foodId)) return false;
            return true;
        }

        /// <summary>Free care actions that don't consume an inventory item.</summary>
        public static bool Bathe()       => DoCareAction(Need.Hygiene, +35, 8);
        public static bool Brush()       => DoCareAction(Need.Dental,  +40, 6);
        public static bool Socialize()   => DoCareAction(Need.Social,  +30, 10);

        private static bool DoCareAction(Need n, int delta, int xpAward)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null || !d.petAlive) return false;
            int cur = NeedLevel(n);
            if (cur >= 100) return false;       // already full
            SetNeed(d, n, cur + delta);
            d.petXP += xpAward;
            CheckLevelUp(d);
            try { Sparq.Core.SaveService.Save(); } catch {}
            OnChanged?.Invoke();
            return true;
        }

        private static void CheckLevelUp(Sparq.Core.PlayerData d)
        {
            // Simple curve: level N requires N * 50 XP.
            while (d.petLevel < 30 && d.petXP >= d.petLevel * 50)
            {
                d.petXP -= d.petLevel * 50;
                d.petLevel++;
            }
        }

        /// <summary>Revive a dead pet — costs coins. Caller checks cost.</summary>
        public static bool Revive(int costCoins)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return false;
            if (d.sparqCoins < costCoins) return false;
            d.sparqCoins -= costCoins;
            d.petAlive = true;
            d.hunger = d.petHygiene = d.petDental = d.petSocial = 60;   // back to "ok" state
            d.petZeroNeedSinceUnix = 0;
            try { Sparq.Core.SaveService.Save(); } catch {}
            OnChanged?.Invoke();
            return true;
        }

        public static bool IsAlive() => Sparq.Core.SaveService.Data?.petAlive ?? false;

        // ─────────────────────────────────────────────────────────────────
        // EGG SYSTEM — Sims-style care-streak rewards. Keep pets alive +
        // happy long enough and you earn eggs that hatch into new pets.
        // ─────────────────────────────────────────────────────────────────

        public const int ROSTER_CAP = 3;
        public const string EGG_COMMON    = "common";
        public const string EGG_RARE      = "rare";
        public const string EGG_EPIC      = "epic";
        public const string EGG_LEGENDARY = "legendary";
        public const string EGG_MYTHIC    = "mythic";

        // Care-streak milestones — day count → egg rarity awarded.
        // Triggered by the midnight check in PetCareRunner.
        public static readonly Dictionary<int, string> STREAK_MILESTONES = new Dictionary<int, string>
        {
            {   7, EGG_COMMON },
            {  14, EGG_RARE },
            {  30, EGG_EPIC },
            {  90, EGG_LEGENDARY },
            { 180, EGG_MYTHIC },
        };

        /// <summary>"Happy" threshold for the daily care check — all four
        /// meters must be ≥ this value at midnight for the streak to tick.</summary>
        public const int HAPPY_THRESHOLD = 60;

        public static event Action<string> OnEggGranted;   // (rarity)
        public static event Action<Pet>    OnEggHatched;   // (new pet)

        public static void GrantEgg(string rarity)
        {
            if (string.IsNullOrEmpty(rarity)) return;
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            if (d.eggInventory == null) d.eggInventory = new List<string>();
            d.eggInventory.Add(rarity);
            try { Sparq.Core.SaveService.Save(); } catch {}
            OnEggGranted?.Invoke(rarity);
            OnChanged?.Invoke();
            Debug.Log($"[PetService] Granted a {rarity} egg. Inventory: {d.eggInventory.Count}");
        }

        public static List<string> EggInventory()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d?.eggInventory == null) return new List<string>();
            return new List<string>(d.eggInventory);
        }

        /// <summary>Hatch the egg at inventoryIndex. Rolls a species from
        /// the catalog weighted by rarity tier, adds it to the roster.
        /// Returns the new pet, or null if hatch failed (roster full,
        /// bad index, etc.).</summary>
        public static Pet HatchEgg(int inventoryIndex)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d?.eggInventory == null || inventoryIndex < 0 || inventoryIndex >= d.eggInventory.Count) return null;

            // Roster cap — must sell/evict before hatching another.
            if (Roster().Count >= ROSTER_CAP)
            {
                Debug.LogWarning($"[PetService] Roster full ({ROSTER_CAP}/{ROSTER_CAP}) — sell a pet before hatching.");
                return null;
            }

            string rarity = d.eggInventory[inventoryIndex];
            d.eggInventory.RemoveAt(inventoryIndex);

            // Pool of species at this rarity (case-insensitive match).
            var pool = new List<Species>();
            foreach (var s in CATALOG)
                if (s != null && string.Equals(s.rarity, RarityFromEgg(rarity), StringComparison.OrdinalIgnoreCase))
                    pool.Add(s);
            if (pool.Count == 0) pool.AddRange(CATALOG);   // safety — shouldn't trigger

            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];

            // Add to roster (mirror BuyPet's bookkeeping but skip cost).
            var pet = new Pet
            {
                instanceId = Guid.NewGuid().ToString("N").Substring(0, 8),
                speciesId  = pick.id,
                nickname   = pick.name,
                level      = 1,
                hunger     = 80,
            };
            var roster = Roster();
            roster.Add(pet);
            SaveRoster();

            try { Sparq.Core.SaveService.Save(); } catch {}
            OnEggHatched?.Invoke(pet);
            OnChanged?.Invoke();
            Debug.Log($"[PetService] Hatched {rarity} egg → {pick.name} ({pick.id}).");
            return pet;
        }

        private static string RarityFromEgg(string eggRarity)
        {
            // Catalog uses "Common"/"Rare"/"Epic"/"Legendary"/"Mythic"
            switch ((eggRarity ?? "").ToLowerInvariant())
            {
                case EGG_COMMON:    return "Common";
                case EGG_RARE:      return "Rare";
                case EGG_EPIC:      return "Epic";
                case EGG_LEGENDARY: return "Legendary";
                case EGG_MYTHIC:    return "Mythic";
                default:            return "Common";
            }
        }

        // ── Care-streak midnight check ──────────────────────────────────

        /// <summary>Run once per app-active-day. Compares today's date to
        /// the last-checked date; if a new day rolled over, evaluates
        /// whether the pet stayed happy and advances/resets the streak.
        /// Grants eggs when a milestone is hit.</summary>
        public static void CheckDailyCareStreak()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (d.petLastCareCheckDate == today) return;   // already checked today

            // Skip the streak evaluation on the very first check ever —
            // no "yesterday" to grade.
            bool firstCheck = string.IsNullOrEmpty(d.petLastCareCheckDate);
            d.petLastCareCheckDate = today;

            if (firstCheck) { try { Sparq.Core.SaveService.Save(); } catch {} return; }

            // Pet died → reset streak hard
            if (!d.petAlive)
            {
                if (d.petCareStreakDays > 0)
                    Debug.Log($"[PetService] Streak reset (pet died). Was {d.petCareStreakDays} days.");
                d.petCareStreakDays = 0;
                try { Sparq.Core.SaveService.Save(); } catch {}
                return;
            }

            // Are all four meters above the happy threshold?
            bool happy = d.hunger     >= HAPPY_THRESHOLD
                      && d.petHygiene >= HAPPY_THRESHOLD
                      && d.petDental  >= HAPPY_THRESHOLD
                      && d.petSocial  >= HAPPY_THRESHOLD;

            if (!happy)
            {
                if (d.petCareStreakDays > 0)
                    Debug.Log($"[PetService] Streak reset (pet unhappy at check). Was {d.petCareStreakDays} days.");
                d.petCareStreakDays = 0;
            }
            else
            {
                d.petCareStreakDays++;
                Debug.Log($"[PetService] Care streak → {d.petCareStreakDays} days.");

                // Milestone egg?
                if (STREAK_MILESTONES.TryGetValue(d.petCareStreakDays, out var eggRarity))
                {
                    GrantEgg(eggRarity);
                }
            }
            try { Sparq.Core.SaveService.Save(); } catch {}
            OnChanged?.Invoke();
        }

        // ── Hidden mythic eggs in the stage map ─────────────────────────

        // Three fixed stage-IDs that hide a Mythic egg. Picked by feel:
        // an early-mid surprise, a mid-game reward, and a late-game gift.
        public static readonly string[] MYTHIC_EGG_STAGES = { "stage_07", "stage_18", "stage_30" };

        /// <summary>Call when the player visits or completes a stage.
        /// If it's one of the hidden mythic-egg stages and hasn't been
        /// claimed yet, grant the egg and remember the stage.</summary>
        public static bool TryCollectMythicEggAtStage(string stageId)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null || string.IsNullOrEmpty(stageId)) return false;

            bool isMythic = false;
            foreach (var s in MYTHIC_EGG_STAGES)
                if (s == stageId) { isMythic = true; break; }
            if (!isMythic) return false;

            if (d.mythicEggsFound == null) d.mythicEggsFound = new List<string>();
            if (d.mythicEggsFound.Contains(stageId)) return false;   // already claimed

            d.mythicEggsFound.Add(stageId);
            GrantEgg(EGG_MYTHIC);
            Debug.Log($"[PetService] HIDDEN MYTHIC EGG found at {stageId}!");
            return true;
        }

        /// <summary>Seconds left before death given the current zero-need timer.
        /// Returns -1 if no need is currently at 0.</summary>
        public static long SecondsUntilDeath()
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null || !d.petAlive) return -1;
            if (d.petZeroNeedSinceUnix <= 0) return -1;
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = nowUnix - d.petZeroNeedSinceUnix;
            return Math.Max(0, (long)GRACE_SECONDS - elapsed);
        }

        private const string KEY_ROSTER = "sparq.pets.roster.v1";
        private const string KEY_ACTIVE = "sparq.pets.active.v1";
        private const string KEY_INV    = "sparq.pets.inv.v1";

        private static List<Pet> _roster;
        private static List<string> _ownedItems;
        private static string _activeId;

        public static List<Pet> Roster()
        {
            if (_roster != null) return _roster;
            _roster = new List<Pet>();
            string raw = PlayerPrefs.GetString(KEY_ROSTER, "");
            if (string.IsNullOrEmpty(raw)) { Seed(); return _roster; }
            foreach (var seg in raw.Split('|'))
            {
                if (string.IsNullOrEmpty(seg)) continue;
                var p = seg.Split(';');
                if (p.Length < 7) continue;
                _roster.Add(new Pet
                {
                    instanceId = p[0], speciesId = p[1], nickname = p[2],
                    level = int.TryParse(p[3], out int l) ? l : 1,
                    hunger = int.TryParse(p[4], out int h) ? h : 80,
                    hatId = p[5], bodyId = p[6],
                    trinkId = p.Length > 7 ? p[7] : "",
                });
            }
            // One-time migration: old default nickname "Wisp" → new "Drip" so the
            // pet's name matches the new Sweet-Droplet sprite.
            bool migrated = false;
            foreach (var pet in _roster)
            {
                if (pet.speciesId == "wisp" && pet.nickname == "Wisp")
                { pet.nickname = "Drip"; migrated = true; }
            }
            if (migrated) SaveRoster();
            return _roster;
        }

        public static List<string> OwnedItems()
        {
            if (_ownedItems != null) return _ownedItems;
            _ownedItems = new List<string>();
            string raw = PlayerPrefs.GetString(KEY_INV, "");
            if (string.IsNullOrEmpty(raw)) return _ownedItems;
            foreach (var s in raw.Split(',')) if (!string.IsNullOrEmpty(s)) _ownedItems.Add(s);
            return _ownedItems;
        }

        public static string ActiveId()
        {
            if (_activeId != null) return _activeId;
            _activeId = PlayerPrefs.GetString(KEY_ACTIVE, "");
            if (string.IsNullOrEmpty(_activeId) && Roster().Count > 0)
                _activeId = _roster[0].instanceId;
            return _activeId ?? "";
        }

        public static Pet Active()
        {
            string id = ActiveId();
            foreach (var p in Roster()) if (p.instanceId == id) return p;
            return Roster().Count > 0 ? Roster()[0] : null;
        }

        public static void SetActive(string instanceId)
        {
            _activeId = instanceId;
            PlayerPrefs.SetString(KEY_ACTIVE, instanceId);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }

        public static (int atk, int def, int hp) StatsOf(Pet p)
        {
            if (p == null) return (0, 0, 0);
            var sp = FindSpecies(p.speciesId);
            int atk = sp.baseAtk, def = sp.baseDef, hp = sp.baseHp;
            void Apply(string id) { var it = FindItem(id); if (it != null) { atk += it.atk; def += it.def; hp += it.hp; } }
            Apply(p.hatId); Apply(p.bodyId); Apply(p.trinkId);
            // Level scaling
            int lvBonus = (p.level - 1) * 2;
            return (atk + lvBonus, def + lvBonus, hp + lvBonus * 2);
        }

        // ───────── Pet Evolution ─────────
        // Stages from PlayerData.petLevel (1..30):
        //   0 Hatchling (1-9)  1 Juvenile (10-19)  2 Adult (20-29)  3 Elder (30)
        // Each stage grows the pet's render size and (from Adult+) buffs its
        // battle stats — the visible reward for caring for it long-term.
        public static int EvolutionStage(int petLevel)
        {
            if (petLevel >= 30) return 3;
            if (petLevel >= 20) return 2;
            if (petLevel >= 10) return 1;
            return 0;
        }

        public static string EvolutionStageName(int stage) => stage switch
        {
            1 => "Juvenile",
            2 => "Adult",
            3 => "Elder",
            _ => "Hatchling",
        };

        /// <summary>Render-size multiplier: bigger pet at higher stages.</summary>
        public static float EvolutionScale(int stage) => stage switch
        {
            1 => 1.00f,
            2 => 1.12f,
            3 => 1.25f,
            _ => 0.85f,
        };

        /// <summary>Battle stat multiplier applied on top of pet care/level scaling.</summary>
        public static float EvolutionStatMult(int stage) => stage switch
        {
            2 => 1.10f,
            3 => 1.25f,
            _ => 1.00f,
        };

        /// <summary>
        /// True if the active pet has crossed into a new evolution stage we
        /// haven't celebrated yet. Updates PlayerData.petLastShownStage in-place
        /// so callers don't have to. Use to fire the "PET EVOLVED!" popup.
        /// </summary>
        public static bool TryConsumeEvolutionAdvance(out int newStage)
        {
            newStage = 0;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d == null) return false;
                int cur = EvolutionStage(d.petLevel);
                if (cur > d.petLastShownStage)
                {
                    newStage = cur;
                    d.petLastShownStage = cur;
                    Sparq.Core.SaveService.ScheduleSave();
                    return true;
                }
            }
            catch {}
            return false;
        }

        // ───────── Actions ─────────
        public static int Coins()
        {
            var d = Sparq.Core.SaveService.Data;
            return d?.sparqCoins ?? 0;
        }
        private static void SpendCoins(int amount)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d != null) { d.sparqCoins = Mathf.Max(0, d.sparqCoins - amount); Sparq.Core.SaveService.Save(); }
        }
        private static void GainCoins(int amount)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d != null) { d.sparqCoins += amount; Sparq.Core.SaveService.Save(); }
        }

        public static bool BuyPet(string speciesId)
        {
            var sp = FindSpecies(speciesId);
            if (sp == null || Coins() < sp.cost) return false;
            SpendCoins(sp.cost);
            var pet = new Pet
            {
                instanceId = Guid.NewGuid().ToString("N").Substring(0, 8),
                speciesId = speciesId,
                nickname = sp.name,
                level = 1, hunger = 80,
            };
            Roster().Add(pet);
            SaveRoster(); OnChanged?.Invoke();
            return true;
        }

        public static bool SellPet(string instanceId)
        {
            var list = Roster();
            if (list.Count <= 1) return false;       // never let them sell their only pet
            for (int i = 0; i < list.Count; i++)
                if (list[i].instanceId == instanceId)
                {
                    var sp = FindSpecies(list[i].speciesId);
                    GainCoins(sp.sellValue);
                    list.RemoveAt(i);
                    if (_activeId == instanceId) SetActive(list[0].instanceId);
                    SaveRoster(); OnChanged?.Invoke();
                    return true;
                }
            return false;
        }

        public static bool BuyItem(string itemId)
        {
            var it = FindItem(itemId);
            if (it == null || Coins() < it.cost) return false;
            SpendCoins(it.cost);
            OwnedItems().Add(itemId);
            SaveItems(); OnChanged?.Invoke();
            return true;
        }

        public static bool SellItem(string itemId)
        {
            var it = FindItem(itemId);
            if (it == null) return false;
            var owned = OwnedItems();
            if (!owned.Remove(itemId)) return false;
            GainCoins(it.sellValue);
            // If equipped on active, unequip
            var p = Active();
            if (p != null)
            {
                if (p.hatId == itemId) p.hatId = "";
                if (p.bodyId == itemId) p.bodyId = "";
                if (p.trinkId == itemId) p.trinkId = "";
                SaveRoster();
            }
            SaveItems(); OnChanged?.Invoke();
            return true;
        }

        public static void Equip(string petId, string itemId)
        {
            var it = FindItem(itemId); if (it == null) return;
            foreach (var p in Roster())
            {
                if (p.instanceId != petId) continue;
                switch (it.slot)
                {
                    case Slot.Hat:     p.hatId   = itemId; break;
                    case Slot.Body:    p.bodyId  = itemId; break;
                    case Slot.Trinket: p.trinkId = itemId; break;
                }
                SaveRoster(); OnChanged?.Invoke();
                return;
            }
        }

        public static void Unequip(string petId, Slot slot)
        {
            foreach (var p in Roster())
            {
                if (p.instanceId != petId) continue;
                switch (slot)
                {
                    case Slot.Hat:     p.hatId   = ""; break;
                    case Slot.Body:    p.bodyId  = ""; break;
                    case Slot.Trinket: p.trinkId = ""; break;
                }
                SaveRoster(); OnChanged?.Invoke();
                return;
            }
        }

        // Quick-feed: consume the cheapest food the player owns. Returns the food
        // id consumed, or null if pantry is empty.
        public static string QuickFeed()
        {
            foreach (var f in FOODS) // FOODS already ordered cheap→expensive
            {
                if (FoodCount(f.id) > 0) { ConsumeFood(f.id); return f.id; }
            }
            return null;
        }

        // ───────── Persistence ─────────
        private static void SaveRoster()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _roster.Count; i++)
            {
                if (i > 0) sb.Append('|');
                var p = _roster[i];
                sb.Append(p.instanceId).Append(';')
                  .Append(p.speciesId).Append(';')
                  .Append(p.nickname).Append(';')
                  .Append(p.level).Append(';')
                  .Append(p.hunger).Append(';')
                  .Append(p.hatId).Append(';')
                  .Append(p.bodyId).Append(';')
                  .Append(p.trinkId);
            }
            PlayerPrefs.SetString(KEY_ROSTER, sb.ToString());
            PlayerPrefs.Save();
        }
        private static void SaveItems()
        {
            PlayerPrefs.SetString(KEY_INV, string.Join(",", _ownedItems));
            PlayerPrefs.Save();
        }
        private static void Seed()
        {
            // Start every player with a free Drip (droplet companion)
            _roster.Add(new Pet
            {
                instanceId = Guid.NewGuid().ToString("N").Substring(0, 8),
                speciesId = "wisp",
                nickname = "Drip",
                level = 1, hunger = 80,
            });
            _activeId = _roster[0].instanceId;
            // Free starter ribbon
            OwnedItems().Add("trin_ribbon");
            SaveRoster(); SaveItems();
            PlayerPrefs.SetString(KEY_ACTIVE, _activeId);
            PlayerPrefs.Save();
        }
    }
}
