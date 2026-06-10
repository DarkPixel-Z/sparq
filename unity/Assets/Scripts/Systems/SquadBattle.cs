using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Spine.Unity;   // BattleOfHeroes Spine characters — fluid skeletal animation replacing PNG sequences

namespace Sparq.Systems
{
    /// <summary>
    /// Squad-vs-squad chibi auto-battler — Hero Wars / Idle Heroes flavor.
    ///
    ///   • 3 hero squad (player class + pet + ally) on left
    ///   • Up to 5 enemies per wave on right (3 waves: easy → medium → boss)
    ///   • Real-time auto-attack ticks per fighter
    ///   • Ult bar fills as fighters act / take damage; player taps the
    ///     glowing portrait to fire that hero's ult
    ///   • Animated chibi sprites (idle bob + attack frame swap)
    ///   • Layer Lab GUI Pro buttons + FantasyMaps painted backgrounds
    ///   • Leohpaz SFX + Action RPG BGM
    ///
    /// ENTRY:   SquadBattle.Start(stageTitle, stageBiome="forest")
    /// </summary>
    public static class SquadBattle
    {
        // ───────── Palette ─────────
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color HP_GREEN   = new Color(0.45f, 0.85f, 0.45f);
        private static readonly Color HP_AMBER   = new Color(1.00f, 0.78f, 0.30f);
        private static readonly Color HP_RED     = new Color(0.85f, 0.35f, 0.40f);
        private static readonly Color ULT_PURPLE = new Color(0.85f, 0.55f, 1.00f);
        private static readonly Color CARD_BG    = new Color(0.08f, 0.06f, 0.16f, 0.94f);
        private static readonly Color BG_DARK    = new Color(0.04f, 0.03f, 0.10f, 0.88f);

        // Class tints for player party HP bars
        private static readonly Dictionary<string, Color> CLASS_TINT = new Dictionary<string, Color>
        {
            ["knight"]   = new Color(0.55f, 0.65f, 0.95f),
            ["paladin"]  = new Color(1.00f, 0.78f, 0.30f),
            ["archer"]   = new Color(0.45f, 0.85f, 0.55f),
            ["mage"]     = new Color(0.65f, 0.55f, 0.95f),
            ["assassin"] = new Color(0.85f, 0.40f, 0.50f),
        };

        // ───────── Real UI sprites (Layer Lab GUI Pro) ─────────
        private const string LL_BTN_BLUE   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Blue.png";
        private const string LL_BTN_BROWN  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Brown.png";
        private const string LL_BTN_GOLD   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Yellow.png";
        private const string LL_BTN_DARK   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Dark.png";
        private const string LL_BTN_RED    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Red.png";
        private const string LL_BTN_PURPLE = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_Purple.png";
        private const string LL_PORTRAIT_BG = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png";

        // ───────── SFX (Leohpaz) ─────────
        private const string LEO = "Assets/Leohpaz/RPG_Essentials_Free/";
        private const string SFX_SLASH    = LEO + "10_Battle_SFX/22_Slash_04.wav";
        private const string SFX_FLESH    = LEO + "10_Battle_SFX/15_Impact_flesh_02.wav";
        private const string SFX_FLESH2   = LEO + "10_Battle_SFX/77_flesh_02.wav";
        private const string SFX_ENCOUNTER= LEO + "10_Battle_SFX/55_Encounter_02.wav";
        private const string SFX_DEATH    = LEO + "10_Battle_SFX/69_Enemy_death_01.wav";
        private const string SFX_FIRE     = LEO + "8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav";
        private const string SFX_THUNDER  = LEO + "8_Atk_Magic_SFX/18_Thunder_02.wav";
        private const string SFX_WIND     = LEO + "8_Atk_Magic_SFX/25_Wind_01.wav";
        private const string SFX_EARTH    = LEO + "8_Atk_Magic_SFX/30_Earth_02.wav";
        private const string SFX_CHARGE   = LEO + "8_Atk_Magic_SFX/45_Charge_05.wav";
        private const string SFX_HEAL     = LEO + "8_Buffs_Heals_SFX/02_Heal_02.wav";
        private const string SFX_CONFIRM  = LEO + "10_UI_Menu_SFX/013_Confirm_03.wav";

        // BGM
        private const string BGM_DIR = "Assets/Action RPG Music 1.6/";
        private static readonly string[] BGM_BATTLE = {
            BGM_DIR + "BGM07battle1.wav", BGM_DIR + "BGM07battle2.wav",
            BGM_DIR + "BGM07battle3.wav", BGM_DIR + "BGM07battle4.wav",
        };
        private static readonly string[] BGM_BOSS = {
            BGM_DIR + "BGM08boss1.wav", BGM_DIR + "BGM08boss2.wav",
        };
        private const string MS_VICTORY = BGM_DIR + "MS01triumph1NL.wav";
        private const string MS_DEFEAT  = BGM_DIR + "MS02gameover1NL.wav";

        // ═══════════════════════════════════════════════════════════════════
        // FIGHTER MODEL
        // ═══════════════════════════════════════════════════════════════════
        private class Fighter
        {
            public string  name;
            public bool    isPlayer;
            public int     hp, maxHp;
            public float   hpDisplayed;
            public int     atk, def;
            public float   attackInterval;   // seconds between auto-attacks
            public float   attackTimer;      // counts down to next attack
            public float   ultCharge;        // 0..100
            public float   critChance;       // 0..1
            public bool    alive = true;

            // Sprites: idle frames cycled, attack frames played briefly on swing
            public Sprite[] idleFrames;
            public Sprite[] attackFrames;
            public int      frameIdx;
            public float    frameTimer;
            public bool     swinging;
            public float    swingTimer;
            public bool     inMotion;     // suppresses idle bob during lunge
            public bool     ultArmed;     // player tapped ult — fires this player phase instead of basic
            public Fighter  phaseTarget;  // who this fighter is attacking this phase

            // Class id (for player slots) — drives ult flavor + tint
            public string classId;

            // Ult definition
            public string ultName;
            public string ultVfx;            // "fire" | "thunder" | "earth" | "wind" | "slash" | "heal"
            public Color  ultColor;
            public float  ultDmgMult;
            public bool   ultIsAOE;
            public bool   ultIsHeal;
            public string ultSfx;

            // UI hooks
            public RectTransform slotRT;
            public Image    portrait;
            public Image    portraitBg;
            // Spine skeletal animation (BattleOfHeroes pack). When set, supersedes the
            // PNG-frame portrait Image — animations are driven via spine.AnimationState.
            public SkeletonGraphic spine;
            public Slider   hpBar;
            public Image    hpFill;
            public Slider   ultBar;
            public Image    ultFill;
            public Image    ultGlow;
            public Button   ultButton;
            public Vector2  homePos;
            public Transform fxLayer;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ENEMY ROSTER (chibi pack-based, animated)
        // ═══════════════════════════════════════════════════════════════════
        private struct EnemyDef
        {
            public string name;
            public string idlePathBase;   // e.g. "Assets/.../Idle/0_Bandit_Idle_"
            public string atkPathBase;    // attack frame base
            public int    idleCount;      // number of idle frames available (we'll grab first 6)
            public int    atkCount;
            public int    hp, atk;
            public float  attackInterval;
            public string ultVfx;         // basic enemies have no ult, but we use this for hit color flavor
            public Color  tint;
        }

        // Wave compositions per stage. Wave count is implicit by array length.
        // The last wave entry is treated as the boss.
        private static EnemyDef[][] WavesFor(string biome)
        {
            // Bandits — common easy enemy
            var bandit = new EnemyDef {
                name = "Bandit",
                idlePathBase = "Assets/BanditChibi/Assassin/PNG/PNG Sequences/Back - Idle/Back - Idle_",
                atkPathBase  = "Assets/BanditChibi/Assassin/PNG/PNG Sequences/Back - Attacking/Back - Attacking_",
                idleCount = 8, atkCount = 6,
                hp = 35, atk = 6, attackInterval = 2.0f,
                ultVfx = "slash", tint = new Color(0.95f, 0.55f, 0.45f) };

            var pirate = new EnemyDef {
                name = "Pirate",
                idlePathBase = "Assets/PirateChibi/PNG/1/1_entity_000_IDLE_",
                atkPathBase  = "Assets/PirateChibi/PNG/1/1_entity_000_ATTACK_",
                idleCount = 8, atkCount = 6,
                hp = 45, atk = 7, attackInterval = 2.1f,
                ultVfx = "slash", tint = new Color(0.85f, 0.65f, 0.45f) };

            var mercenary = new EnemyDef {
                name = "Mercenary",
                idlePathBase = "Assets/MercenariesChibi/Mercenaries_1/PNG/PNG Sequences/Idle/0_Mercenaries_Idle_",
                atkPathBase  = "Assets/MercenariesChibi/Mercenaries_1/PNG/PNG Sequences/Run Slashing/0_Mercenaries_Run Slashing_",
                idleCount = 8, atkCount = 6,
                hp = 55, atk = 9, attackInterval = 2.3f,
                ultVfx = "earth", tint = new Color(0.75f, 0.55f, 0.45f) };

            var medusa = new EnemyDef {
                name = "Medusa",
                idlePathBase = "Assets/MedusaChibi/Medusa_1/PNG/PNG Sequences/Idle/0_Medusa_Idle_",
                atkPathBase  = "Assets/MedusaChibi/Medusa_1/PNG/PNG Sequences/Run Slashing/0_Medusa_Run Slashing_",
                idleCount = 8, atkCount = 6,
                hp = 70, atk = 11, attackInterval = 2.4f,
                ultVfx = "thunder", tint = new Color(0.65f, 0.85f, 0.45f) };

            var samurai = new EnemyDef {
                name = "Ronin",
                idlePathBase = "Assets/SamuraiChibi/Samurai_1/PNG/PNG Sequences/Idle/0_Samurai_Idle_",
                atkPathBase  = "Assets/SamuraiChibi/Samurai_1/PNG/PNG Sequences/Run Slashing/0_Samurai_Run Slashing_",
                idleCount = 8, atkCount = 6,
                hp = 65, atk = 10, attackInterval = 2.2f,
                ultVfx = "slash", tint = new Color(0.85f, 0.45f, 0.55f) };

            var persian = new EnemyDef {
                name = "Sand Raider",
                idlePathBase = "Assets/PersianWarriorChibi/Persian_and_Arab_Warriors_1/PNG/PNG Sequences/Idle/0_Persian_and_Arab_Warriors_Idle_",
                atkPathBase  = "Assets/PersianWarriorChibi/Persian_and_Arab_Warriors_1/PNG/PNG Sequences/Run Slashing/0_Persian_and_Arab_Warriors_Run Slashing_",
                idleCount = 8, atkCount = 6,
                hp = 75, atk = 11, attackInterval = 2.3f,
                ultVfx = "earth", tint = new Color(0.95f, 0.78f, 0.45f) };

            var mimic = new EnemyDef {
                name = "Mimic Lord",
                idlePathBase = "Assets/MimicChibi/Mimic_1/PNG/PNG Sequences/Idle/0_Mimic_Idle_",
                atkPathBase  = "Assets/MimicChibi/Mimic_1/PNG/PNG Sequences/Run Slashing/0_Mimic_Run Slashing_",
                idleCount = 10, atkCount = 6,
                hp = 200, atk = 16, attackInterval = 2.6f,
                ultVfx = "fire", tint = new Color(1.00f, 0.55f, 0.30f) };

            // Wave structure: 3 fights — easy / medium / boss.
            // Bigger waves so each fight feels like a brawl, not a duel.
            return new[]
            {
                new[] { bandit, bandit, bandit, bandit },                  // wave 1: 4 bandits
                new[] { pirate, mercenary, bandit, pirate, bandit },        // wave 2: 5 mixed mid-tier
                new[] { mimic, samurai, persian, medusa, samurai },         // wave 3 (boss): boss + 4 escorts
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // STATE
        // ═══════════════════════════════════════════════════════════════════
        private static GameObject _root;
        private static MonoBehaviour _runner;
        private static AudioSource _audio, _music;
        private static List<Fighter> _party = new List<Fighter>();
        private static List<Fighter> _enemies = new List<Fighter>();
        private static int _waveIndex;
        private static EnemyDef[][] _waves;
        private static int _xpReward, _goldReward;
        private static TMP_Text _statusBanner, _waveText;
        private static Image _fullScreenFlash;
        private static RectTransform _skyLayer, _decoLayer;
        private static Vector2 _skyHome, _decoHome;
        private static string _stageTitle;
        private static bool _battleActive;
        private static bool _bossEncounter;   // true = full 3-wave set piece; false = single skirmish
        private static float _difficulty = 1f; // zone difficulty scalar applied to enemy HP/ATK
        private static int _vitalityPercent;    // today's wellness buff % applied to the squad

        // Outcome of the most recent battle, readable by the map after the battle
        // root is destroyed. Defaults to false (a loss) so callers never advance
        // unless the squad actually won.
        public static bool LastBattleWon { get; private set; }

        // ═══════════════════════════════════════════════════════════════════
        // ENTRY
        // ═══════════════════════════════════════════════════════════════════
        public static void Start(string stageTitle, string biome = "forest", bool bossEncounter = false, float difficulty = 1f)
        {
            if (_root != null) Hide();

            _stageTitle = stageTitle;
            _waveIndex = 0;
            _xpReward = 0;
            _goldReward = 0;
            _bossEncounter = bossEncounter;
            _difficulty = Mathf.Max(0.5f, difficulty);
            LastBattleWon = false;   // assume a loss until Victory() flips it
            _waves = WavesFor(biome);
            if (!bossEncounter)
            {
                // Regular roaming encounter — ONE quick skirmish instead of the
                // full easy→medium→boss escalation. Take the first (easy) wave and
                // trim it to a small group so fights stay snappy.
                var first = _waves[0];
                int take = Mathf.Min(3, first.Length);
                var skirmish = new EnemyDef[take];
                System.Array.Copy(first, skirmish, take);
                _waves = new[] { skirmish };
            }
            _party.Clear();
            _enemies.Clear();

            BuildParty();
            _currentBiome = biome;
            BuildUI(biome);
            BuildEnemyWave(_waves[_waveIndex]);

            if (_runner == null)
            {
                var go = new GameObject("SquadBattle.Runner");
                _runner = go.AddComponent<RunnerMB>();
                _audio = go.AddComponent<AudioSource>();
                _audio.playOnAwake = false; _audio.spatialBlend = 0f;
                _music = go.AddComponent<AudioSource>();
                _music.playOnAwake = false; _music.spatialBlend = 0f;
                _music.loop = true; _music.volume = 0.70f;
            }

            // BGM
            #if UNITY_EDITOR
            string bgmPath = bossEncounter
                ? BGM_BOSS[Random.Range(0, BGM_BOSS.Length)]
                : BGM_BATTLE[Random.Range(0, BGM_BATTLE.Length)];
            // Streaming + Vorbis prevents choppy playback on big WAVs
            Sparq.UI.HomeBgm.ConfigureForStreaming(bgmPath);
            var bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(bgmPath);
            if (bgm != null) { _music.clip = bgm; _music.Play(); }
            #endif

            // Pause home BGM so it doesn't bleed into battle
            try { Sparq.UI.HomeBgm.Pause(); } catch {}

            // Hide home-scene visuals so they don't show through the battle canvas
            // (battle canvas is ScreenSpaceCamera now, doesn't auto-cover overlay UI / scene sprites)
            HideHomeSceneVisuals();

            _battleActive = true;
            _runner.StartCoroutine(BattleLoop());
            _runner.StartCoroutine(IdleAnimateAll());
            _runner.StartCoroutine(LerpHpBars());
            _runner.StartCoroutine(ParallaxDrift());
            _runner.StartCoroutine(SpawnAmbientParticles(BiomeFromString(_currentBiome)));
        }

        public static void Hide()
        {
            _battleActive = false;
            if (_root != null) { Object.Destroy(_root); _root = null; }
            if (_music != null) _music.Stop();
            if (_runner != null) { Object.Destroy(_runner.gameObject); _runner = null; _audio = null; _music = null; }
            _party.Clear(); _enemies.Clear();
            // Restore home-scene visuals we hid during battle
            RestoreHomeSceneVisuals();
            // Resume home BGM now that battle is over
            try { Sparq.UI.HomeBgm.Resume(); } catch {}
        }

        // Hide home-scene UI canvases + sprite GameObjects so they don't render through
        // the battle's ScreenSpaceCamera canvas. We track everything we hide so we can
        // restore exactly the same set on Hide().
        private static void HideHomeSceneVisuals()
        {
            _hiddenHomeObjects.Clear();

            // 1) Hide other Canvases at the home level. Skip the battle's own + the help icon.
            foreach (var c in Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (c == null) continue;
                if (!c.gameObject.activeSelf) continue;
                string n = c.name;
                if (n.StartsWith("SquadBattleRoot")) continue;
                if (n.StartsWith("Sparq_HelpIcon")) continue;
                if (n.StartsWith("StageMapPanel")) continue;
                // Only hide ROOT canvases (have no parent canvas) so we don't double-touch
                if (c.transform.parent != null && c.transform.parent.GetComponentInParent<Canvas>() != null) continue;
                c.gameObject.SetActive(false);
                _hiddenHomeObjects.Add(c.gameObject);
            }

            // 2) Hide home scene SpriteRenderer GameObjects by name
            string[] homeSpriteNames = {
                "Karu", "Mochi", "Una", "[Cinematic]", "WorldMap", "SocialPanel",
                "[ScatteredLoot]", "Loot_Rune", "Loot_Chest", "Loot_GoldenLeaf",
                "Loot_Butterfly", "Loot_EasterEgg", "[Forest]", "SparqEnv"
            };
            foreach (var name in homeSpriteNames)
            {
                var go = GameObject.Find(name);
                if (go != null && go.activeSelf)
                {
                    go.SetActive(false);
                    _hiddenHomeObjects.Add(go);
                }
            }

            // 3) Hide all wandering NPCs (HomeNpc_0, HomeNpc_1, ...)
            for (int i = 0; i < 8; i++)
            {
                var npc = GameObject.Find($"HomeNpc_{i}");
                if (npc != null && npc.activeSelf)
                {
                    npc.SetActive(false);
                    _hiddenHomeObjects.Add(npc);
                }
            }
        }

        private static void RestoreHomeSceneVisuals()
        {
            foreach (var go in _hiddenHomeObjects)
            {
                if (go != null) go.SetActive(true);
            }
            _hiddenHomeObjects.Clear();
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: PARTY (player hero + pet + ally)
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildParty()
        {
            int level = Sparq.Core.SaveService.Data?.level ?? 1;
            int gearAtk = 0, gearDef = 0, gearHp = 0;
            try { (gearAtk, gearDef, gearHp) = EquipmentService.TotalStats(); } catch {}

            // ── Slot 1: Player hero (class-driven) ──
            // Use IsNullOrEmpty — `??` doesn't catch empty strings, only null
            string cls = Sparq.Core.SaveService.Data?.heroClass;
            if (string.IsNullOrEmpty(cls)) cls = "knight";
            var hero = MakeHeroFighter(cls, level, gearAtk, gearDef, gearHp);
            _party.Add(hero);

            // ── Slot 2: Pet (from PetService) ──
            var pet = MakePetFighter(level);
            if (pet != null) _party.Add(pet);

            // ── Slot 3: Ally (random chibi for now — Citizen-Women makes a flavorful 'cleric') ──
            var ally = MakeAllyFighter(level);
            if (ally != null) _party.Add(ally);

            // ── Wellness → power: today's Vitality buffs the whole squad ──
            _vitalityPercent = 0;
            try
            {
                _vitalityPercent = Sparq.Systems.VitalityService.BuffPercent();
                float mult = Sparq.Systems.VitalityService.SquadStatMultiplier();
                if (mult > 1.001f)
                {
                    foreach (var f in _party)
                    {
                        if (f == null) continue;
                        f.maxHp = Mathf.RoundToInt(f.maxHp * mult);
                        f.hp    = f.maxHp;
                        f.atk   = Mathf.RoundToInt(f.atk * mult);
                    }
                }
            }
            catch {}
        }

        private static Fighter MakeHeroFighter(string cls, int level, int gearAtk, int gearDef, int gearHp)
        {
            // Base stats per class
            var (idleBase, atkBase, ult, ultVfx, ultColor) = cls switch
            {
                "knight"   => ("Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__IDLE_",
                               "Assets/FantasyKnight/_PNG/1_KNIGHT/Knight_01__ATTACK_",
                               "Cleave",      "slash",  new Color(1.0f, 0.55f, 0.30f)),
                "archer"   => ("Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Idle/0_Archer_Idle_",
                               "Assets/ElfArcherChibi/Archer_1/PNG/PNG Sequences/Shoot/0_Archer_Shoot_",
                               "Volley",      "wind",   new Color(0.55f, 0.95f, 0.55f)),
                "mage"     => ("Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Idle/0_Time_Keeper_Idle_",
                               "Assets/TimeKeeperChibi/Time_Keeper_1/PNG/PNG Sequences/Attack/0_Time_Keeper_Attack_",
                               "Meteor",      "fire",   new Color(1.0f, 0.45f, 0.20f)),
                "assassin" => ("Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Idle/Idle_",
                               "Assets/NinjaAssassinChibi/Assassin Guy/PNG/PNG Sequences/Attack/Attack_",
                               "Backstab",    "slash",  new Color(0.55f, 0.30f, 0.65f)),
                _          => ("Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Idle/0_Paladin_Idle_",
                               "Assets/PaladinChibi/Paladin_1/PNG/PNG Sequences/Attack/0_Paladin_Attack_",
                               "Holy Smite",  "thunder",new Color(1.0f, 0.85f, 0.30f)),
            };

            return new Fighter
            {
                name = char.ToUpper(cls[0]) + cls.Substring(1),
                isPlayer = true,
                classId = cls,
                maxHp = 110 + level * 12 + gearHp,
                hp   = 110 + level * 12 + gearHp,
                atk  = 12 + level * 2 + gearAtk,
                def  = 4 + level + gearDef,
                attackInterval = 1.8f,
                attackTimer = Random.Range(0.4f, 1.2f),
                critChance = 0.12f,
                idleFrames = LoadFrames(idleBase, 8),
                attackFrames = LoadFrames(atkBase, 6),
                ultName = ult, ultVfx = ultVfx, ultColor = ultColor,
                ultDmgMult = 2.6f, ultIsAOE = true,
                ultSfx = ultVfx switch { "fire"=>SFX_FIRE, "thunder"=>SFX_THUNDER, "wind"=>SFX_WIND, _=>SFX_SLASH },
            };
        }

        private static Fighter MakePetFighter(int level)
        {
            var pet = PetService.Active();
            if (pet == null) return null;
            var sp = PetService.FindSpecies(pet.speciesId);
            // Pet uses a single sprite — for animation we just bob/pulse it.
            // Was previously editor-only — that's why Drip rendered as a white
            // circle in APK even though Sweet-Droplet.png is in Resources.
            var single = !string.IsNullOrEmpty(sp.spritePath)
                ? Sparq.Core.SpriteLoader.Load(sp.spritePath) : null;
            var frames = single != null ? new[] { single } : new Sprite[0];

            // Pet care + level feed its battle stats (wellness → power). A
            // pampered, leveled-up pet is a real asset; a neglected one is sluggish.
            // On TOP of that, evolution stages (Adult/Elder) give the pet a
            // permanent stat bump — the visible reward for long-term care.
            var d = Sparq.Core.SaveService.Data;
            int petLvl = Mathf.Max(1, d?.petLevel ?? 1);
            float care = 0.75f;
            if (d != null)
                care = Mathf.Clamp01((d.hunger + d.petHygiene + d.petDental + d.petSocial) / 400f);
            float careMult = 0.60f + 0.55f * care;   // 60% (neglected) … 115% (pampered)
            float evoMult  = PetService.EvolutionStatMult(PetService.EvolutionStage(petLvl));
            float comboMult = careMult * evoMult;

            int hp  = Mathf.RoundToInt((sp.baseHp * 4 + level * 6 + petLvl * 10) * comboMult);
            int atk = Mathf.RoundToInt((sp.baseAtk + level / 2 + petLvl) * comboMult);
            int def = Mathf.RoundToInt((sp.baseDef + level / 3 + petLvl / 2) * comboMult);
            return new Fighter
            {
                name = pet.nickname ?? sp.name,
                isPlayer = true,
                classId = "pet",
                maxHp = hp, hp = hp,
                atk = atk,
                def = def,
                attackInterval = 2.4f,
                attackTimer = Random.Range(0.6f, 1.4f),
                critChance = 0.08f,
                idleFrames = frames,
                attackFrames = frames,
                ultName = "Pet Heal", ultVfx = "heal", ultColor = new Color(0.55f, 1f, 0.55f),
                ultDmgMult = 0f, ultIsHeal = true,
                ultSfx = SFX_HEAL,
            };
        }

        private static Fighter MakeAllyFighter(int level)
        {
            // Pulls the player's currently-active ally from AllyRoster (slot 3
            // is now a recruitable roster, not a fixed Paladin). Default starter
            // is "paladin", which preserves the old hardcoded behaviour for new
            // players. SetActive happens via AllyRosterPanel.
            var a = AllyRoster.Active();
            if (a == null) return null;
            return new Fighter
            {
                name = a.name,
                isPlayer = true,
                classId = a.classId,
                maxHp = a.baseHp + level * a.hpPerLevel,
                hp   = a.baseHp + level * a.hpPerLevel,
                atk  = a.baseAtk + level * a.atkPerLevel,
                def  = a.baseDef + level * a.defPerLevel,
                attackInterval = a.attackInterval,
                attackTimer = Random.Range(0.8f, 1.6f),
                critChance = a.critChance,
                idleFrames   = LoadFrames(a.idleBase, Mathf.Min(a.idleCount, 8)),
                attackFrames = LoadFrames(a.atkBase,  Mathf.Min(a.atkCount, 6)),
                ultName = a.ultName, ultVfx = a.ultVfx,
                ultColor = a.ultColor,
                ultDmgMult = a.ultDmgMult, ultIsAOE = a.ultIsAOE, ultIsHeal = a.ultIsHeal,
                ultSfx = a.ultSfx,
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: ENEMY WAVE
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildEnemyWave(EnemyDef[] defs)
        {
            _enemies.Clear();
            int level = Sparq.Core.SaveService.Data?.level ?? 1;
            // Only a real boss encounter's FINAL wave gets the boss treatment.
            bool isBoss = _bossEncounter && (_waveIndex == _waves.Length - 1);

            for (int i = 0; i < defs.Length; i++)
            {
                var d = defs[i];
                bool slotIsBoss = isBoss && i == 0;   // first slot of final wave is the boss
                int hpScale = isBoss ? (slotIsBoss ? 3 : 1) : 1;
                // Zone difficulty scales enemy HP + ATK so deeper zones bite harder.
                int eHp  = Mathf.RoundToInt((d.hp + level * 3) * hpScale * _difficulty);
                int eAtk = Mathf.RoundToInt((d.atk + level / 2) * _difficulty);
                var f = new Fighter
                {
                    name = slotIsBoss ? $"<b>{d.name}</b>" : d.name,
                    isPlayer = false,
                    maxHp = eHp,
                    hp   = eHp,
                    atk  = eAtk,
                    def  = level / 3,
                    attackInterval = d.attackInterval,
                    attackTimer = Random.Range(0.4f, 1.4f),
                    critChance = 0.06f,
                    idleFrames = LoadFrames(d.idlePathBase, Mathf.Min(d.idleCount, 8)),
                    attackFrames = LoadFrames(d.atkPathBase, Mathf.Min(d.atkCount, 6)),
                    ultName = "—", ultVfx = d.ultVfx, ultColor = d.tint,
                    ultDmgMult = 0f,    // enemies have no ult tap, just basic attacks
                };
                _enemies.Add(f);
                if (slotIsBoss) _bossFighter = f;
            }
            BuildEnemySlots();
            // Build the dramatic top-of-screen boss HP bar if this is the boss wave
            if (isBoss && _bossFighter != null)
            {
                BuildBossHpBar();
                _bossBasicAttackCounter = 0;
            }
            else
            {
                // Tear down any existing boss bar from previous wave
                if (_bossHpRoot != null) { Object.Destroy(_bossHpRoot); _bossHpRoot = null; }
                _bossFighter = null;
            }
        }

        // Big boss HP bar across the top — distinct from per-fighter slot bars.
        // Includes name banner and color-tinted fill so the boss feels like a Real Threat.
        private static void BuildBossHpBar()
        {
            if (_bossHpRoot != null) Object.Destroy(_bossHpRoot);
            _bossHpRoot = new GameObject("BossHpBar", typeof(RectTransform), typeof(Image));
            _bossHpRoot.transform.SetParent(_root.transform, false);
            var rt = _bossHpRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.86f); rt.anchorMax = new Vector2(0.5f, 0.86f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(880, 70);
            _bossHpRoot.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.08f, 0.95f);
            _bossHpRoot.GetComponent<Image>().raycastTarget = false;

            // Crimson border / accent
            var accent = MakeImage(_bossHpRoot.transform, "Accent", new Color(0.85f, 0.20f, 0.20f, 0.95f));
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(-3, -3); art.offsetMax = new Vector2(3, 3);
            accent.transform.SetAsFirstSibling();
            accent.raycastTarget = false;

            // Boss name (top-left of bar)
            _bossNameTxt = MakeText(_bossHpRoot.transform, "Name",
                $"<b>{_bossFighter.name}</b>",
                new Vector2(0, 0.5f), new Vector2(280, 32), 22, FontStyles.Bold, new Color(1f, 0.85f, 0.45f));
            var nameRT = _bossNameTxt.rectTransform;
            nameRT.pivot = new Vector2(0, 0.5f);
            nameRT.anchoredPosition = new Vector2(20, 14);
            _bossNameTxt.alignment = TextAlignmentOptions.Left;
            _bossNameTxt.outlineWidth = 0.30f; _bossNameTxt.outlineColor = Color.black;
            _bossNameTxt.richText = true;
            _bossNameTxt.raycastTarget = false;

            // Slider for HP fill
            var slGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            slGO.transform.SetParent(_bossHpRoot.transform, false);
            var slRT = slGO.GetComponent<RectTransform>();
            slRT.anchorMin = new Vector2(0, 0); slRT.anchorMax = new Vector2(1, 0);
            slRT.pivot = new Vector2(0.5f, 0); slRT.anchoredPosition = new Vector2(0, 4);
            slRT.sizeDelta = new Vector2(-30, 24);
            _bossHpBar = slGO.GetComponent<Slider>();
            _bossHpBar.minValue = 0; _bossHpBar.maxValue = 1; _bossHpBar.value = 1;
            _bossHpBar.transition = Selectable.Transition.None; _bossHpBar.interactable = false;

            var bgGO = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(slGO.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgGO.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.10f, 0.95f);

            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(slGO.transform, false);
            var faRT = fa.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = new Vector2(2, 2); faRT.offsetMax = new Vector2(-2, -2);
            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fa.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            _bossHpFill = fillGO.GetComponent<Image>();
            _bossHpFill.color = new Color(0.85f, 0.20f, 0.25f);
            _bossHpFill.raycastTarget = false;
            _bossHpBar.fillRect = fillRT;

            // HP text in the middle
            _bossHpTxt = MakeText(_bossHpRoot.transform, "HpTxt",
                $"{_bossFighter.hp} / {_bossFighter.maxHp}",
                new Vector2(0.5f, 0.2f), new Vector2(880, 26), 18, FontStyles.Bold, CREAM);
            _bossHpTxt.alignment = TextAlignmentOptions.Center;
            _bossHpTxt.outlineWidth = 0.22f; _bossHpTxt.outlineColor = Color.black;
            _bossHpTxt.raycastTarget = false;
        }

        private static void RefreshBossHpBar()
        {
            if (_bossFighter == null || _bossHpBar == null) return;
            float ratio = _bossFighter.maxHp > 0 ? _bossFighter.hpDisplayed / _bossFighter.maxHp : 0f;
            _bossHpBar.value = ratio;
            if (_bossHpFill != null)
            {
                _bossHpFill.color = ratio < 0.30f ? new Color(0.95f, 0.18f, 0.18f)
                                  : ratio < 0.55f ? new Color(0.95f, 0.50f, 0.20f)
                                                  : new Color(0.85f, 0.20f, 0.25f);
            }
            if (_bossHpTxt != null)
            {
                _bossHpTxt.text = $"{Mathf.CeilToInt(_bossFighter.hpDisplayed)} / {_bossFighter.maxHp}";
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: UI
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildUI(string biome)
        {
            _root = new GameObject("SquadBattleRoot",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            // Use ScreenSpaceOverlay so the canvas always renders on top of everything.
            // CartoonFX 3D particles can't draw above overlay — instead we use beefy
            // in-canvas procedural effects (slash arcs, big impact starbursts, screen tints)
            // that are guaranteed to render.
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            _useCfxParticles = false;
            c.sortingOrder = 16500;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Painted parallax background
            BuildParallaxBackground(biome);

            // Vignette
            var vig = MakeImage(_root.transform, "Vignette", new Color(0,0,0,0.30f));
            var vrt = vig.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            vig.raycastTarget = false;

            // Foreground GROUND BAND — dark band across the screen at the fighters' feet.
            // This anchors them visually, kills the "floating in the sky" feeling.
            var ground = MakeImage(_root.transform, "GroundBand", new Color(0.05f, 0.04f, 0.10f, 0.78f));
            var grRT = ground.GetComponent<RectTransform>();
            grRT.anchorMin = new Vector2(0, 0); grRT.anchorMax = new Vector2(1, GROUND_Y);
            grRT.offsetMin = Vector2.zero; grRT.offsetMax = Vector2.zero;
            ground.raycastTarget = false;
            // Top edge highlight — gold rule line where the ground meets the painted bg
            var groundRule = MakeImage(_root.transform, "GroundRule", new Color(1f, 0.78f, 0.30f, 0.55f));
            var grlRT = groundRule.GetComponent<RectTransform>();
            grlRT.anchorMin = new Vector2(0, GROUND_Y); grlRT.anchorMax = new Vector2(1, GROUND_Y);
            grlRT.pivot = new Vector2(0.5f, 0.5f);
            grlRT.sizeDelta = new Vector2(0, 4);
            groundRule.raycastTarget = false;

            // Stage banner top
            _statusBanner = MakeText(_root.transform, "Banner", $"<color=#FFD466>{_stageTitle}</color>",
                new Vector2(0.5f, 0.95f), new Vector2(900, 60), 36, FontStyles.Bold, CREAM);
            _statusBanner.alignment = TextAlignmentOptions.Center;
            _statusBanner.outlineWidth = 0.22f; _statusBanner.outlineColor = Color.black;
            _statusBanner.richText = true;

            _waveText = MakeText(_root.transform, "Wave", $"WAVE 1 / {_waves.Length}",
                new Vector2(0.5f, 0.91f), new Vector2(400, 40), 22, FontStyles.Bold, GOLD);
            _waveText.alignment = TextAlignmentOptions.Center;
            _waveText.outlineWidth = 0.20f; _waveText.outlineColor = Color.black;

            // Vitality chip — shows the day's self-care buff so the wellness→power
            // link is felt right where it pays off.
            if (_vitalityPercent > 0)
            {
                var vit = MakeText(_root.transform, "VitalityChip",
                    $"<color=#7CFC8A>VITALITY  +{_vitalityPercent}% squad power</color>",
                    new Vector2(0.5f, 0.87f), new Vector2(720, 38), 24, FontStyles.Bold, CREAM);
                vit.alignment = TextAlignmentOptions.Center;
                vit.outlineWidth = 0.22f; vit.outlineColor = Color.black;
                vit.richText = true; vit.raycastTarget = false;
            }

            // Build party slots (left side)
            BuildPartySlots();

            // Bottom stats bar — Lv / XP / ATK / DEF / HP. High-contrast on dark plate so colors don't clash with painted bg.
            // Bottom HP/MP bar removed per user request — battle stays clean,
            // per-fighter HP bars are visible under each chibi.
            // BuildStatsBar();

            // Full-screen flash overlay (alpha animated)
            _fullScreenFlash = MakeImage(_root.transform, "FSFlash", new Color(1,1,1,0));
            var fsr = _fullScreenFlash.GetComponent<RectTransform>();
            fsr.anchorMin = Vector2.zero; fsr.anchorMax = Vector2.one;
            fsr.offsetMin = Vector2.zero; fsr.offsetMax = Vector2.zero;
            _fullScreenFlash.raycastTarget = false;
        }

        // Bottom stats strip: dark plate so colors stay readable over any background.
        // Shows: Lv N • XP bar • ATK • DEF • HP totals from the player hero.
        // Bottom HUD — big HP and MP bars for the player hero.
        // (Replaces the old Lv/ATK/DEF/HP chips per user request — "replace with health and mp")
        private static Slider _hudHpBar, _hudMpBar;
        private static TMP_Text _hudHpTxt, _hudMpTxt;

        private static void BuildStatsBar()
        {

            // Dark plate
            var plate = MakeImage(_root.transform, "StatsPlate", new Color(0.04f, 0.03f, 0.10f, 0.94f));
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 0);
            prt.pivot = new Vector2(0.5f, 0);
            prt.sizeDelta = new Vector2(0, 110);
            prt.offsetMin = new Vector2(0, 0); prt.offsetMax = new Vector2(0, 110);
            plate.raycastTarget = false;

            // Gold top-rule
            var rule = MakeImage(plate.transform, "Rule", new Color(1f, 0.78f, 0.30f, 0.95f));
            var rrt = rule.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1);
            rrt.sizeDelta = new Vector2(0, 3);
            rule.raycastTarget = false;

            // Helper that builds a labeled bar (HP/MP)
            (Slider, TMP_Text) BuildBar(string label, Color barColor, float yAnchor, string title)
            {
                // Label on the left
                var lbl = MakeText(plate.transform, $"{label}_Lbl",
                    $"<color=#FFD466><b>{title}</b></color>",
                    new Vector2(0, yAnchor), new Vector2(70, 28), 20, FontStyles.Bold, CREAM);
                lbl.alignment = TextAlignmentOptions.Left;
                lbl.richText = true;
                var lblRT = lbl.rectTransform;
                lblRT.pivot = new Vector2(0, 0.5f);
                lblRT.anchoredPosition = new Vector2(20, 0);
                lbl.outlineWidth = 0.20f; lbl.outlineColor = Color.black;
                lbl.raycastTarget = false;

                // Bar background
                var bgGO = new GameObject($"{label}_Bg", typeof(RectTransform), typeof(Image));
                bgGO.transform.SetParent(plate.transform, false);
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(0, yAnchor); bgRT.anchorMax = new Vector2(1, yAnchor);
                bgRT.pivot = new Vector2(0.5f, 0.5f);
                bgRT.offsetMin = new Vector2(100, -16); bgRT.offsetMax = new Vector2(-200, 16);
                bgGO.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.20f, 0.95f);

                // Slider
                var slGO = new GameObject($"{label}_Slider", typeof(RectTransform), typeof(Slider));
                slGO.transform.SetParent(bgGO.transform, false);
                var slRT = slGO.GetComponent<RectTransform>();
                slRT.anchorMin = Vector2.zero; slRT.anchorMax = Vector2.one;
                slRT.offsetMin = Vector2.zero; slRT.offsetMax = Vector2.zero;
                var slider = slGO.GetComponent<Slider>();
                slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
                slider.transition = Selectable.Transition.None; slider.interactable = false;

                var fa = new GameObject("FillArea", typeof(RectTransform));
                fa.transform.SetParent(slGO.transform, false);
                var faRT = fa.GetComponent<RectTransform>();
                faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
                faRT.offsetMin = new Vector2(2, 2); faRT.offsetMax = new Vector2(-2, -2);
                var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGO.transform.SetParent(fa.transform, false);
                var fillRT = fillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
                fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
                var fillImg = fillGO.GetComponent<Image>();
                fillImg.color = barColor;
                fillImg.raycastTarget = false;
                slider.fillRect = fillRT;

                // Numeric text on the right
                var txt = MakeText(plate.transform, $"{label}_Txt", "0/0",
                    new Vector2(1, yAnchor), new Vector2(180, 28), 22, FontStyles.Bold, CREAM);
                txt.alignment = TextAlignmentOptions.Center;
                var txtRT = txt.rectTransform;
                txtRT.pivot = new Vector2(1, 0.5f);
                txtRT.anchoredPosition = new Vector2(-20, 0);
                txt.outlineWidth = 0.22f; txt.outlineColor = Color.black;
                txt.raycastTarget = false;

                return (slider, txt);
            }

            // HP bar (top of plate)
            var (hpBar, hpTxt) = BuildBar("HP", new Color(0.85f, 0.30f, 0.30f), 0.72f, "HP");
            _hudHpBar = hpBar; _hudHpTxt = hpTxt;
            // MP bar (bottom of plate)
            var (mpBar, mpTxt) = BuildBar("MP", new Color(0.45f, 0.55f, 1.00f), 0.30f, "MP");
            _hudMpBar = mpBar; _hudMpTxt = mpTxt;
        }

        // Update HUD HP/MP bars from the player hero (slot 0).
        // Called every frame from LerpHpBars so values stay live.
        private static void RefreshHudBars()
        {
            if (_party == null || _party.Count == 0) return;
            var hero = _party[0];
            if (hero == null) return;
            if (_hudHpBar != null)
            {
                _hudHpBar.value = hero.maxHp > 0 ? hero.hpDisplayed / hero.maxHp : 0f;
            }
            if (_hudHpTxt != null)
            {
                _hudHpTxt.text = $"{Mathf.CeilToInt(hero.hpDisplayed)} / {hero.maxHp}";
            }
            if (_hudMpBar != null)
            {
                _hudMpBar.value = hero.ultCharge / 100f;
            }
            if (_hudMpTxt != null)
            {
                _hudMpTxt.text = $"{Mathf.RoundToInt(hero.ultCharge)} / 100";
            }
        }

        // ── LAYOUT: ground line + uniform sizes ──
        // All fighters stand on the same horizontal "ground line" in a single row.
        // Party occupies the LEFT half, enemies the RIGHT, all at the same Y.
        private const float GROUND_Y = 0.42f;          // bottom-edge y for all fighters
        private const float FIGHTER_TARGET_HEIGHT = 240f;   // every fighter's portrait renders at this pixel height — uniform look

        // ── Cartoon FX Remaster particle prefab paths (per VFX kind) ──
        private const string CFX = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/";
        private static readonly Dictionary<string, string> CFX_BY_VFX = new Dictionary<string, string>
        {
            ["slash"]  = CFX + "Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab",
            // The actual SLASH ARC — sweeping sword trail visual (separate from impact)
            ["slash_trail"]      = CFX + "Sword Trails/Fire/CFXR4 Sword Trail FIRE (360 Spiral).prefab",
            ["slash_trail_thin"] = CFX + "Sword Trails/Fire/CFXR4 Sword Trail FIRE (360 Thin Spiral).prefab",
            ["smash"]  = CFX + "Impacts/CFXR2 Ground Hit.prefab",
            ["arrow"]  = CFX + "Misc/CFXR3 Hit Misc A.prefab",
            ["bolt"]   = CFX + "Electric/CFXR3 Hit Electric C (Air).prefab",
            ["fire"]   = CFX + "Fire/CFXR3 Fire Explosion B.prefab",
            ["shadow"] = CFX + "Misc/CFXR3 Hit Misc F Smoke.prefab",
            ["shield"] = CFX + "Nature/CFXR3 Shield Leaves A (Lit).prefab",
            ["smoke"]  = CFX + "Misc/CFXR Magic Poof.prefab",
            ["thunder"]= CFX + "Electric/CFXR3 Hit Electric C (Air).prefab",
            ["wind"]   = CFX + "Nature/CFXR3 Hit Leaves A (Lit).prefab",
            // Ult-tier — bigger explosions
            ["ult_fire"]   = CFX + "Explosions/CFXR Explosion 1.prefab",
            ["ult_burst"]  = CFX + "Explosions/CFXR2 WW Explosion.prefab",
            ["ult_firework"]=CFX + "Explosions/CFXR4 Firework HDR Shoot Single (Random Color).prefab",
        };

        private static bool _useCfxParticles;        // true if canvas is ScreenSpaceCamera and Main Camera exists
        private static float _cfxLayerZ;             // world Z to spawn particles (in front of canvas plane)
        // Cache loaded prefabs so AssetDatabase.LoadAssetAtPath only runs once per kind
        private static readonly Dictionary<string, GameObject> _cfxCache = new Dictionary<string, GameObject>();

        // ── BattleOfHeroes Spine character mapping ──
        // 20 Spine-animated characters (Char01-Char20) with full anim sets:
        //   Idle / Attack / Walk / Get Hit / Death / Get Freez / Get poisoned
        // Mapping is approximate (we'd need to preview each in Spine Editor for perfect fit).
        // Leaving as a constant so user can swap numbers easily.
        // Feature flag: gate Spine integration until skeleton data assets verifiably parse.
        // When false, every fighter takes the PNG-fallback path — which is what we
        // ship reliably. Flip to true ONLY after running Sparq → Fix Spine Imports
        // and confirming all 20 SkeletonData assets generated cleanly.
        private const bool USE_SPINE = false;

        private const string SPINE_CHAR_DIR = "Assets/BattleOfHeroes/UI/Spine/Characters/Character ";
        private static readonly Dictionary<string, int> SPINE_CHAR_BY_CLASS = new Dictionary<string, int>
        {
            // Player heroes
            ["knight"]   = 1,
            ["paladin"]  = 2,
            ["archer"]   = 3,
            ["mage"]     = 4,
            ["assassin"] = 5,
            ["pet"]      = 6,
            // Enemies — we'll cycle 11-20 for variety
        };
        // Enemies pick from this pool by index modulo length
        private static readonly int[] SPINE_ENEMY_POOL = { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        private static int _spineEnemyIdx = 0;
        // Cache SkeletonDataAsset by character number so we don't re-load
        private static readonly Dictionary<int, SkeletonDataAsset> _spineCache = new Dictionary<int, SkeletonDataAsset>();

        // Try to load a BattleOfHeroes Spine SkeletonDataAsset for the given character number (1..20)
        private static SkeletonDataAsset LoadSpineFor(int charNum)
        {
            if (_spineCache.TryGetValue(charNum, out var cached) && cached != null) return cached;
            #if UNITY_EDITOR
            string num = charNum.ToString().PadLeft(2, '0');
            string path = $"{SPINE_CHAR_DIR}{num}/skeleton_SkeletonData.asset";
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(path);
            if (asset != null)
            {
                _spineCache[charNum] = asset;
                return asset;
            }
            #endif
            return null;
        }

        private static SkeletonDataAsset LoadSpineForClass(string classId)
        {
            if (string.IsNullOrEmpty(classId)) return null;
            return SPINE_CHAR_BY_CLASS.TryGetValue(classId, out int n) ? LoadSpineFor(n) : null;
        }

        private static SkeletonDataAsset LoadSpineForEnemy(int slotIdx)
        {
            int charNum = SPINE_ENEMY_POOL[slotIdx % SPINE_ENEMY_POOL.Length];
            return LoadSpineFor(charNum);
        }

        // Trigger a Spine animation on a fighter, returning to Idle when done.
        // Names match the BattleOfHeroes character animations (case-sensitive):
        //   "Attack" / "Get Hit" / "Death" / "Walk" / "Get Freez" / "Get poisoned" / "Idle"
        private static void PlaySpineAnim(Fighter f, string anim, bool returnToIdle = true)
        {
            if (f == null || f.spine == null || f.spine.AnimationState == null) return;
            try
            {
                var entry = f.spine.AnimationState.SetAnimation(0, anim, false);
                if (returnToIdle && entry != null)
                {
                    f.spine.AnimationState.AddAnimation(0, "Idle", true, 0);
                }
            }
            catch
            {
                // Animation name mismatch — fall back to Idle so we don't soft-lock the rig
                try { f.spine.AnimationState.SetAnimation(0, "Idle", true); } catch {}
            }
        }

        // Tracks home-scene GameObjects we hide during battle (so they don't bleed through
        // ScreenSpaceCamera canvases). Restored on SquadBattle.Hide().
        private static readonly List<GameObject> _hiddenHomeObjects = new List<GameObject>();

        // Currently-active biome (from Start). Used by ambient particle spawn.
        private static string _currentBiome = "forest";

        // ── BOSS state ──
        // Tracks the boss enemy + the big HP bar at the top of the screen.
        private static Fighter _bossFighter;        // null until boss wave
        private static Slider  _bossHpBar;
        private static Image   _bossHpFill;
        private static TMP_Text _bossHpTxt, _bossNameTxt;
        private static GameObject _bossHpRoot;
        private static int _bossBasicAttackCounter; // every Nth attack triggers mega-attack
        private const int  BOSS_MEGA_EVERY = 5;     // every 5th basic attack fires the AOE — feels tense but not punishing

        // ── Ambient particle config per biome ──
        private struct AmbientCfg
        {
            public string kind;         // "fireflies" | "embers" | "mist" | "leaves"
            public Color  color;
            public float  intervalSec;  // seconds between spawns
        }
        private static AmbientCfg BiomeFromString(string biome)
        {
            switch (biome)
            {
                case "moonlit": return new AmbientCfg { kind="fireflies", color=new Color(0.65f, 0.75f, 1f, 0.85f), intervalSec=0.18f };
                case "haunted": return new AmbientCfg { kind="mist",      color=new Color(0.85f, 0.55f, 1f, 0.55f), intervalSec=0.22f };
                case "rocky":   return new AmbientCfg { kind="embers",    color=new Color(1f,    0.55f, 0.20f, 0.85f), intervalSec=0.10f };
                default:        return new AmbientCfg { kind="fireflies", color=new Color(1f,    0.92f, 0.55f, 0.85f), intervalSec=0.18f };
            }
        }

        private static void BuildPartySlots()
        {
            // 3 party fighters in a CHEVRON formation — front-line tank pushes ahead,
            // mid + back are slightly offset so they don't all clip on a flat line.
            // Order: slot 0 = front (highest atk), 1 = mid-back, 2 = far-back
            float[] xs3 = { 0.18f, 0.10f, 0.26f };
            float[] ys3 = { 0.40f, 0.46f, 0.46f };   // back-line slightly higher = "deeper" feel
            float[] xs2 = { 0.18f, 0.28f };
            float[] ys2 = { 0.42f, 0.46f };
            float[] xs1 = { 0.22f };
            float[] ys1 = { 0.42f };
            float[] xs = _party.Count switch { 3 => xs3, 2 => xs2, _ => xs1 };
            float[] ys = _party.Count switch { 3 => ys3, 2 => ys2, _ => ys1 };
            for (int i = 0; i < _party.Count; i++)
            {
                BuildFighterSlot(_party[i], new Vector2(xs[i], ys[i]), true);
            }
        }

        private static void BuildEnemySlots()
        {
            // Clear any old enemy slots from last wave
            var existing = _root.transform.Find("EnemySlots");
            if (existing != null) Object.Destroy(existing.gameObject);

            var holder = new GameObject("EnemySlots", typeof(RectTransform));
            holder.transform.SetParent(_root.transform, false);
            var hrt = holder.GetComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;

            // Enemies on the RIGHT in a mirrored chevron formation — front-line
            // closer to the squad, others stepped back. Real battle vibe instead
            // of a perfectly straight line.
            float[] xs5 = { 0.78f, 0.66f, 0.90f, 0.58f, 0.96f };
            float[] ys5 = { 0.40f, 0.46f, 0.46f, 0.50f, 0.50f };
            float[] xs4 = { 0.78f, 0.66f, 0.90f, 0.58f };
            float[] ys4 = { 0.40f, 0.46f, 0.46f, 0.50f };
            float[] xs3 = { 0.78f, 0.66f, 0.90f };
            float[] ys3 = { 0.40f, 0.46f, 0.46f };
            float[] xs2 = { 0.72f, 0.86f };
            float[] ys2 = { 0.42f, 0.46f };
            float[] xs1 = { 0.78f };
            float[] ys1 = { 0.42f };
            float[] xs = _enemies.Count switch { 5 => xs5, 4 => xs4, 3 => xs3, 2 => xs2, _ => xs1 };
            float[] ys = _enemies.Count switch { 5 => ys5, 4 => ys4, 3 => ys3, 2 => ys2, _ => ys1 };

            for (int i = 0; i < _enemies.Count; i++)
            {
                BuildFighterSlot(_enemies[i], new Vector2(xs[i], ys[i]), false, holder.transform);
            }
        }

        private static void BuildFighterSlot(Fighter f, Vector2 anchor, bool isPlayerSide, Transform parent = null)
        {
            parent = parent ?? _root.transform;
            var go = new GameObject($"Slot_{f.name}", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            // Pivot at bottom-center so all fighters' FEET sit on the ground line (uniform horizon)
            rt.pivot = new Vector2(0.5f, 0f);
            // Uniform size for every party + enemy slot
            rt.sizeDelta = new Vector2(280, 280);
            f.slotRT = rt;
            f.homePos = rt.anchoredPosition;

            // Platform shadow under the fighter — much bigger + darker so they look
            // grounded instead of floating in the sky.
            var plat = new GameObject("Platform", typeof(RectTransform), typeof(Image));
            plat.transform.SetParent(go.transform, false);
            var pRT = plat.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 0); pRT.anchorMax = new Vector2(0.5f, 0);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(0, 14);    // sits at feet
            pRT.sizeDelta = new Vector2(280, 56);         // was 170x28 → almost double
            var pimg = plat.GetComponent<Image>();
            pimg.color = new Color(0, 0, 0, 0.65f);       // darker (was 0.45)
            pimg.sprite = MakeEllipseSprite();
            pimg.raycastTarget = false;

            // ── Try Spine first; fall back to PNG-frame Image if not available ──
            // Spine path uses BattleOfHeroes SkeletonGraphic with skeletal anims (Idle/Attack/Get Hit/Death).
            // Gated behind USE_SPINE flag — if SkeletonData assets aren't parsing
            // we end up with empty pink rectangles, which is worse than PNG.
            SkeletonDataAsset spineAsset = null;
            if (USE_SPINE)
            {
                if (isPlayerSide)
                    spineAsset = LoadSpineForClass(f.classId);
                else
                    spineAsset = LoadSpineForEnemy(_spineEnemyIdx++);
            }

            if (spineAsset != null)
            {
                // Spine path
                var spineGO = new GameObject("SpineChar", typeof(RectTransform));
                spineGO.transform.SetParent(go.transform, false);
                var sgRT = spineGO.GetComponent<RectTransform>();
                sgRT.anchorMin = new Vector2(0.5f, 0);
                sgRT.anchorMax = new Vector2(0.5f, 0);
                sgRT.pivot     = new Vector2(0.5f, 0);
                sgRT.anchoredPosition = new Vector2(0, 30);
                sgRT.sizeDelta = new Vector2(280, 280);

                var sg = spineGO.AddComponent<SkeletonGraphic>();
                sg.skeletonDataAsset = spineAsset;
                sg.startingAnimation = "Idle";
                sg.startingLoop = true;
                sg.material = sg.material;   // ensures default material is set
                sg.Initialize(false);
                sg.raycastTarget = false;
                f.spine = sg;

                // Mirror enemies horizontally
                if (!isPlayerSide) sgRT.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                // PNG-fallback path (original code)
                var portGO = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
                portGO.transform.SetParent(go.transform, false);
                var portRT = portGO.GetComponent<RectTransform>();
                portRT.anchorMin = new Vector2(0.5f, 0);
                portRT.anchorMax = new Vector2(0.5f, 0);
                portRT.pivot     = new Vector2(0.5f, 0);
                portRT.anchoredPosition = new Vector2(0, 30);
                portRT.sizeDelta = new Vector2(240, 240);
                f.portrait = portGO.GetComponent<Image>();
                f.portrait.preserveAspect = false;
                f.portrait.raycastTarget = false;
                if (f.idleFrames != null && f.idleFrames.Length > 0 && f.idleFrames[0] != null)
                {
                    f.portrait.sprite = f.idleFrames[0];
                    f.portrait.color = Color.white;
                    NormalizeFighterSize(f.portrait, FIGHTER_TARGET_HEIGHT, f.classId);
                }
                else
                {
                    // No character art bundled — fall back to a colored circle.
                    // Shrink to roughly real-character size and use deeper colors
                    // so the placeholder reads as "a unit", not a glowing orb.
                    var circleSp = Sparq.Core.SpriteLoader.Load(
                        "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png");
                    if (circleSp != null) f.portrait.sprite = circleSp;
                    f.portrait.color = isPlayerSide
                        ? new Color(0.75f, 0.55f, 0.15f, 1f)     // deeper gold
                        : new Color(0.65f, 0.20f, 0.20f, 1f);    // deeper red
                    // Shrink to ~50% of fighter target — circles read as a unit
                    // marker, not a giant glow. ~120px in a 240px portrait box.
                    portRT.sizeDelta = new Vector2(FIGHTER_TARGET_HEIGHT * 0.5f,
                                                   FIGHTER_TARGET_HEIGHT * 0.5f);
                }
                if (!isPlayerSide) portRT.localScale = new Vector3(-1, 1, 1);
            }

            // FX layer above sprite (floating numbers, AoE rings, etc.)
            var fxGO = new GameObject("FX", typeof(RectTransform));
            fxGO.transform.SetParent(go.transform, false);
            var frt = fxGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            f.fxLayer = fxGO.transform;

            // HP bar below the slot
            var hpGO = new GameObject("HP", typeof(RectTransform));
            hpGO.transform.SetParent(go.transform, false);
            var hprt = hpGO.GetComponent<RectTransform>();
            hprt.anchorMin = new Vector2(0.5f, 0); hprt.anchorMax = new Vector2(0.5f, 0);
            hprt.pivot = new Vector2(0.5f, 1);
            hprt.anchoredPosition = new Vector2(0, 0);
            hprt.sizeDelta = new Vector2(200, 18);

            var hpBg = MakeImage(hpGO.transform, "Bg", new Color(0.04f, 0.03f, 0.10f, 0.95f));
            var bgrt = hpBg.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero; bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero; bgrt.offsetMax = Vector2.zero;
            hpBg.raycastTarget = false;

            var slGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            slGO.transform.SetParent(hpGO.transform, false);
            var slrt = slGO.GetComponent<RectTransform>();
            slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one;
            slrt.offsetMin = Vector2.zero; slrt.offsetMax = Vector2.zero;
            f.hpBar = slGO.GetComponent<Slider>();
            f.hpBar.minValue = 0; f.hpBar.maxValue = f.maxHp; f.hpBar.value = f.maxHp;
            f.hpBar.transition = Selectable.Transition.None;
            f.hpBar.interactable = false;
            f.hpDisplayed = f.maxHp;

            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(slGO.transform, false);
            var fart = fa.GetComponent<RectTransform>();
            fart.anchorMin = new Vector2(0, 0.1f); fart.anchorMax = new Vector2(1, 0.9f);
            fart.offsetMin = new Vector2(2, 0); fart.offsetMax = new Vector2(-2, 0);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(fa.transform, false);
            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = Vector2.zero; fillRT.offsetMax = Vector2.zero;
            f.hpFill = fillGO.GetComponent<Image>();
            Color hpFillColor = isPlayerSide
                ? (CLASS_TINT.TryGetValue(f.classId ?? "", out var t) ? t : HP_GREEN)
                : f.ultColor;
            f.hpFill.color = hpFillColor;
            f.hpFill.raycastTarget = false;
            f.hpBar.fillRect = fillRT;

            // Name label above hp bar
            var nameTxt = MakeText(go.transform, "Name", f.name,
                new Vector2(0.5f, 0), new Vector2(220, 26),
                17, FontStyles.Bold, CREAM);
            nameTxt.alignment = TextAlignmentOptions.Center;
            nameTxt.outlineWidth = 0.22f; nameTxt.outlineColor = Color.black;
            var nameRT = nameTxt.rectTransform;
            nameRT.pivot = new Vector2(0.5f, 1f);
            nameRT.anchoredPosition = new Vector2(0, 24);
            nameTxt.raycastTarget = false;
            nameTxt.richText = true;

            // ULT bar (player only) — sits below HP, tappable when full
            if (isPlayerSide && f.ultDmgMult > 0 || f.ultIsHeal)
            {
                var ultGO = new GameObject("Ult", typeof(RectTransform), typeof(Image), typeof(Button));
                ultGO.transform.SetParent(go.transform, false);
                var urt = ultGO.GetComponent<RectTransform>();
                urt.anchorMin = new Vector2(0.5f, 0); urt.anchorMax = new Vector2(0.5f, 0);
                urt.pivot = new Vector2(0.5f, 1);
                urt.anchoredPosition = new Vector2(0, -22);
                urt.sizeDelta = new Vector2(200, 16);
                ultGO.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.10f, 0.95f);
                f.ultButton = ultGO.GetComponent<Button>();
                f.ultButton.transition = Selectable.Transition.None;
                f.ultButton.interactable = false;
                Fighter capF = f;
                f.ultButton.onClick.AddListener(() => OnUltTapped(capF));

                // Glow ring (visible when full)
                var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image));
                glowGO.transform.SetParent(ultGO.transform, false);
                var glowRT = glowGO.GetComponent<RectTransform>();
                glowRT.anchorMin = Vector2.zero; glowRT.anchorMax = Vector2.one;
                glowRT.offsetMin = new Vector2(-6, -6); glowRT.offsetMax = new Vector2(6, 6);
                f.ultGlow = glowGO.GetComponent<Image>();
                f.ultGlow.color = new Color(ULT_PURPLE.r, ULT_PURPLE.g, ULT_PURPLE.b, 0f);
                f.ultGlow.raycastTarget = false;

                var uslGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
                uslGO.transform.SetParent(ultGO.transform, false);
                var uslRT = uslGO.GetComponent<RectTransform>();
                uslRT.anchorMin = Vector2.zero; uslRT.anchorMax = Vector2.one;
                uslRT.offsetMin = Vector2.zero; uslRT.offsetMax = Vector2.zero;
                f.ultBar = uslGO.GetComponent<Slider>();
                f.ultBar.minValue = 0; f.ultBar.maxValue = 100; f.ultBar.value = 0;
                f.ultBar.transition = Selectable.Transition.None;
                f.ultBar.interactable = false;

                var ufa = new GameObject("FillArea", typeof(RectTransform));
                ufa.transform.SetParent(uslGO.transform, false);
                var ufart = ufa.GetComponent<RectTransform>();
                ufart.anchorMin = Vector2.zero; ufart.anchorMax = Vector2.one;
                ufart.offsetMin = Vector2.zero; ufart.offsetMax = Vector2.zero;
                var ufillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                ufillGO.transform.SetParent(ufa.transform, false);
                var ufillRT = ufillGO.GetComponent<RectTransform>();
                ufillRT.anchorMin = Vector2.zero; ufillRT.anchorMax = Vector2.one;
                ufillRT.offsetMin = Vector2.zero; ufillRT.offsetMax = Vector2.zero;
                f.ultFill = ufillGO.GetComponent<Image>();
                f.ultFill.color = ULT_PURPLE;
                f.ultFill.raycastTarget = false;
                f.ultBar.fillRect = ufillRT;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BACKGROUND
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildParallaxBackground(string biome)
        {
            // Solid dark backplate so nothing leaks through if BG load fails
            var bp = MakeImage(_root.transform, "BG_Plate", new Color(0.06f, 0.04f, 0.12f, 1f));
            var bprt = bp.GetComponent<RectTransform>();
            bprt.anchorMin = Vector2.zero; bprt.anchorMax = Vector2.one;
            bprt.offsetMin = Vector2.zero; bprt.offsetMax = Vector2.zero;
            bp.raycastTarget = false;

            // Layer Lab Demo painted scenes — vivid, more dramatic than FantasyMaps' soft pastel art.
            // Pick by biome + apply tint so each fight feels distinct.
            string demoBg; Color bgTint;
            switch (biome)
            {
                case "moonlit": demoBg = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_05.png"; bgTint = new Color(0.55f, 0.55f, 0.85f); break;
                case "haunted": demoBg = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_04.png"; bgTint = new Color(0.85f, 0.65f, 0.95f); break;
                case "rocky":   demoBg = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_02.png"; bgTint = new Color(1.00f, 0.80f, 0.55f); break;
                default:        demoBg = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_03.png"; bgTint = Color.white; break;
            }
            var bg = MakeBgLayer(_root.transform, "BG_Painted", demoBg, bgTint);
            _skyLayer = bg.GetComponent<RectTransform>();
            _skyHome = _skyLayer.anchoredPosition;

            // Decoration overlay from FantasyMaps — adds parallax depth
            string mapNum = biome == "haunted" || biome == "rocky" ? "02" : "01";
            string decoPath = $"Assets/FantasyMaps/_PNG/{mapNum}/layers/l3-decoartions.png";
            var deco = MakeBgLayer(_root.transform, "BG_Deco", decoPath, new Color(1, 1, 1, 0.55f));
            _decoLayer = deco.GetComponent<RectTransform>();
            _decoHome = _decoLayer.anchoredPosition;

            // Heavy battle vignette — darkens edges, focuses center
            var dark = MakeImage(_root.transform, "BG_Darken", new Color(0, 0, 0, 0.45f));
            var drt = dark.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dark.raycastTarget = false;
        }

        private static Image MakeBgLayer(Transform parent, string name, string spritePath, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-100, -100); rt.offsetMax = new Vector2(100, 100);
            var img = go.GetComponent<Image>();
            img.color = tint;
            img.preserveAspect = false;
            img.raycastTarget = false;
            var sp = Sparq.Core.SpriteLoader.Load(spritePath);
            if (sp != null) img.sprite = sp;
            return img;
        }

        private static IEnumerator ParallaxDrift()
        {
            if (Accessibility.ReducedMotion) yield break;   // calm mode: static background
            float t = 0;
            while (_root != null)
            {
                t += Time.deltaTime;
                if (_skyLayer != null)
                    _skyLayer.anchoredPosition = _skyHome + new Vector2(Mathf.Sin(t * 0.15f) * 30f, 0);
                if (_decoLayer != null)
                    _decoLayer.anchoredPosition = _decoHome + new Vector2(Mathf.Sin(t * 0.45f) * 8f, Mathf.Cos(t * 0.30f) * 4f);
                yield return null;
            }
        }

        // ── AMBIENT ENVIRONMENT PARTICLES ──
        // Continuously spawns fireflies / embers / mist / leaves across the screen
        // depending on the active biome. Particles drift, fade, and self-destroy.
        private static IEnumerator SpawnAmbientParticles(AmbientCfg cfg)
        {
            if (Accessibility.ReducedMotion) yield break;   // calm mode: no ambient particle drift
            // Dedicated layer behind the combatants but in front of background
            var layer = new GameObject("AmbientParticles", typeof(RectTransform));
            layer.transform.SetParent(_root.transform, false);
            var lrt = layer.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            // Sit between background and fighters in render order
            layer.transform.SetSiblingIndex(3);

            while (_root != null)
            {
                yield return new WaitForSeconds(cfg.intervalSec);
                if (_root == null) yield break;
                SpawnOneParticle(layer.transform, cfg);
            }
        }

        private static void SpawnOneParticle(Transform parent, AmbientCfg cfg)
        {
            var go = new GameObject("p", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            // Spawn position depends on kind (mist drifts up from bottom, leaves fall from top, etc.)
            float x = Random.Range(0.0f, 1.0f);
            float y = cfg.kind == "leaves" ? 1.05f
                    : cfg.kind == "mist"   ? 0.05f
                                           : Random.Range(0.05f, 0.55f);   // fireflies/embers spawn low
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float size = cfg.kind == "mist" ? 30f : (cfg.kind == "embers" ? 8f : 12f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.color = cfg.color;
            img.sprite = MakeFilledCircleSprite();
            img.raycastTarget = false;
            _runner.StartCoroutine(AmbientParticleAnim(rt, img, cfg));
        }

        private static IEnumerator AmbientParticleAnim(RectTransform rt, Image img, AmbientCfg cfg)
        {
            float life = cfg.kind == "mist" ? 5.5f : (cfg.kind == "embers" ? 2.5f : 3.5f);
            float t = 0;
            // Drift direction depends on kind
            float driftX = Random.Range(-0.3f, 0.3f);
            float driftY = cfg.kind == "leaves" ? -0.6f
                         : cfg.kind == "embers" ? 1.0f         // embers rise
                         : cfg.kind == "mist"   ? 0.4f         // mist rises slowly
                         : Random.Range(0.3f, 0.8f);            // fireflies bob upward
            Color baseC = img.color;
            Vector2 startAnchor = rt.anchorMin;
            while (t < life && rt != null)
            {
                float p = t / life;
                Vector2 anchor = startAnchor + new Vector2(driftX, driftY) * p * 0.15f;
                // Add a gentle horizontal sine wobble for fireflies and embers
                if (cfg.kind == "fireflies" || cfg.kind == "embers")
                    anchor.x += Mathf.Sin(t * 3f) * 0.02f;
                rt.anchorMin = rt.anchorMax = anchor;
                // Fade out near the end of life
                float alpha = baseC.a * (1f - Mathf.Pow(p, 2));
                img.color = new Color(baseC.r, baseC.g, baseC.b, alpha);
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BATTLE TICK LOOP
        // ═══════════════════════════════════════════════════════════════════
        private static IEnumerator BattleLoop()
        {
            yield return WaveIntro();

            while (_root != null && _battleActive)
            {
                if (AllDead(_party)) { yield return Defeat(); yield break; }
                if (AllDead(_enemies))
                {
                    yield return WaveCleared();
                    if (_waveIndex >= _waves.Length) { yield return Victory(); yield break; }
                    BuildEnemyWave(_waves[_waveIndex]);
                    yield return WaveIntro();
                }

                // ── REAL-TIME TICK (Top Heroes style) ──
                // Each fighter has their own attackInterval — they fire when their
                // timer hits 0, independent of everyone else. Result: overlapping
                // attacks from both sides, chaotic-feeling combat instead of
                // back-and-forth phases.
                float dt = Time.deltaTime;
                TickFighters(_party, _enemies, dt, isPlayerSide: true);
                TickFighters(_enemies, _party, dt, isPlayerSide: false);
                yield return null;
            }
        }

        // ── REAL-TIME TICK ──
        // Each living, non-busy fighter counts down their attackTimer; when it hits 0,
        // they fire a non-blocking StationaryAttack coroutine. This produces the
        // overlapping, simultaneous combat feel that mobile RPGs use.
        private static void TickFighters(List<Fighter> attackers, List<Fighter> defenders, float dt, bool isPlayerSide)
        {
            foreach (var a in attackers)
            {
                if (a == null || !a.alive) continue;
                if (a.inMotion) continue;          // mid-attack already
                // Ult takes priority — if player tapped the bar at full charge, fire it
                if (a.ultArmed)
                {
                    a.ultArmed = false;
                    a.attackTimer = a.attackInterval;   // reset basic timer too
                    _runner.StartCoroutine(DoUlt(a));
                    continue;
                }
                a.attackTimer -= dt;
                if (a.attackTimer <= 0f)
                {
                    a.attackTimer = a.attackInterval;
                    var target = PickAliveTarget(defenders);
                    if (target == null) continue;

                    // ── BOSS MEGA-ATTACK ──
                    // The boss punctuates basic attacks with a telegraphed AOE every Nth swing.
                    // Breaks the back-and-forth rhythm with a real "uh oh" moment.
                    if (a == _bossFighter)
                    {
                        _bossBasicAttackCounter++;
                        if (_bossBasicAttackCounter % BOSS_MEGA_EVERY == 0)
                        {
                            _runner.StartCoroutine(BossMegaAttack(a));
                            continue;
                        }
                    }

                    _runner.StartCoroutine(StationaryAttack(a, target, isPlayerSide));
                }
            }
        }

        // Pulse the banner text for `duration` seconds — used to amp up tension
        // when the boss is charging a mega-attack.
        private static IEnumerator PulseBanner(TMP_Text txt, float duration)
        {
            if (txt == null) yield break;
            float t = 0;
            var rt = txt.rectTransform;
            Vector3 baseScale = rt.localScale;
            while (t < duration && txt != null)
            {
                float pulse = 1f + Mathf.Sin(t * 12f) * 0.10f;
                rt.localScale = baseScale * pulse;
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) rt.localScale = baseScale;
        }

        // ── BOSS MEGA-ATTACK ──
        // Big telegraphed move: warning banner → wind-up animation → screen flash + AOE
        // damage to ALL alive heroes. Punctuates the rhythm of basic attacks.
        private static IEnumerator BossMegaAttack(Fighter boss)
        {
            if (boss == null || !boss.alive || boss.slotRT == null) yield break;
            boss.inMotion = true;

            // 1) WARNING BANNER + boss glow — pulse animation drives tension
            string warning = $"<color=#FF4455>⚠  {boss.name} CHARGES MASSIVE ATTACK!  ⚠</color>";
            _statusBanner.text = warning;
            _runner.StartCoroutine(PulseBanner(_statusBanner, 1.4f));   // pulse for the duration of the charge
            // Pulsing red aura under the boss
            var aura = SpawnActiveSpotlight(boss);
            // Boss "lifts up" slightly during charge
            Vector3 home = boss.slotRT.position;
            yield return LerpWorldPos(boss.slotRT, home, home + new Vector3(0, 28, 0), 0.30f);

            // Heavy charge SFX
            PlayAudio(SFX_CHARGE, 1.0f);
            ShakeOnce(_root.GetComponent<RectTransform>(), 4f, 1.4f);   // sustained low rumble for full charge

            // Hold the charge so player feels the threat (extended for proper telegraph)
            yield return new WaitForSeconds(1.4f);

            // 2) UNLEASH — full screen red flash, big screen shake, AOE damage
            FullScreenFlash(new Color(1f, 0.20f, 0.15f, 0.65f), 0.50f);
            ShakeOnce(_root.GetComponent<RectTransform>(), 28f, 0.45f);
            PlayAudio(SFX_FIRE, 1.0f);

            // AOE damage — 1.6× boss atk to every alive hero, cannot be reduced below 1
            int dmgPerHero = Mathf.Max(1, Mathf.RoundToInt(boss.atk * 1.6f));
            foreach (var p in _party)
            {
                if (!p.alive) continue;
                int dmg = Mathf.Max(1, dmgPerHero - p.def);
                p.hp = Mathf.Max(0, p.hp - dmg);
                FlashPortrait(p.portrait, new Color(1f, 0.30f, 0.30f));
                ShakeOnce(p.slotRT, 16f, 0.30f);
                FloatNumber(p, $"<b>{dmg}</b>", new Color(1f, 0.40f, 0.40f), 1.7f, true);
                // Red impact burst on every hero
                SpawnImpactBurst(p.fxLayer, new Color(1f, 0.30f, 0.20f), false);
            }

            // 3) Snap back, recover
            yield return new WaitForSeconds(0.30f);
            if (aura != null) Object.Destroy(aura);
            if (boss.alive) yield return LerpWorldPos(boss.slotRT, boss.slotRT.position, home, 0.20f);

            // Process any deaths
            foreach (var p in _party)
                if (p.hp <= 0 && p.alive) _runner.StartCoroutine(KillFighter(p));

            _statusBanner.text = $"<color=#FFD466>{_stageTitle}</color>";
            boss.inMotion = false;
        }

        // Stationary attack — fighter LEANS forward (~30px), spits out a slash/lightning
        // visual at the target, deals damage, snaps back. They DO NOT run across
        // the screen. The visual effects are what travels — the body stays in lane.
        private static IEnumerator StationaryAttack(Fighter a, Fighter target, bool isPlayerAttacker)
        {
            if (a == null || !a.alive || a.slotRT == null || target == null || !target.alive) yield break;
            a.inMotion = true;

            Vector3 home = a.slotRT.position;
            // Small lean toward target — 35px max so they barely leave their slot
            Vector3 dir = (target.slotRT.position - home);
            float dist = dir.magnitude;
            if (dist > 0.001f) dir /= dist;
            else dir = Vector3.right;
            Vector3 lean = home + dir * 35f;

            // Quick lean forward
            yield return LerpWorldPos(a.slotRT, home, lean, 0.10f);

            // Swing animation + fire visual at target
            a.swinging = true; a.swingTimer = 0;
            // Spine: play attack swing → return to idle automatically
            PlaySpineAnim(a, "Attack");
            SpawnSlashTrail(a, target, dir);   // class-aware: lightning for mage/paladin, slash arc otherwise

            // Brief lead-in so the visual reads before damage
            yield return new WaitForSeconds(0.12f);

            // Damage applies (with knockback + impact burst inside ApplyHit)
            ApplyHit(a, target, isPlayerAttacker);

            // Hit-stop pause for weight
            yield return new WaitForSeconds(0.18f);
            a.swinging = false;

            // Snap back to home
            if (a.alive) yield return LerpWorldPos(a.slotRT, lean, home, 0.14f);

            // Process death of target if any
            if (target != null && target.hp <= 0 && target.alive)
                _runner.StartCoroutine(KillFighter(target));

            a.inMotion = false;
        }

        // Resolve one squad's coordinated attack: wind-up → lunge → hit (sync) → return.
        // All alive attackers move in unison; ults replace basic attacks for armed heroes.
        private static IEnumerator ResolvePhase(List<Fighter> attackers, List<Fighter> defenders, bool isPlayerPhase)
        {
            // 1) FOCUS FIRE — entire squad targets one defender at a time.
            // Pick the lowest-HP alive defender (finish them off first) so the
            // squad coordinates instead of spreading damage.
            Fighter sharedTarget = null;
            foreach (var d in defenders)
            {
                if (!d.alive) continue;
                if (sharedTarget == null || d.hp < sharedTarget.hp) sharedTarget = d;
            }
            var actions = new List<Fighter>();
            foreach (var a in attackers)
            {
                if (!a.alive) continue;
                a.phaseTarget = sharedTarget;
                if (a.phaseTarget == null && !a.ultArmed) continue;
                actions.Add(a);
            }
            if (actions.Count == 0) yield break;

            // ── Heroes with armed ults branch off and fire those FIRST (cinematic) ──
            // Run ults sequentially because each is a big visual moment.
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                var a = actions[i];
                if (a.ultArmed)
                {
                    a.ultArmed = false;
                    yield return DoUlt(a);
                    actions.RemoveAt(i);
                }
            }
            if (actions.Count == 0) yield break;
            // Refresh defender alive list — ults may have killed targets
            for (int i = actions.Count - 1; i >= 0; i--)
            {
                if (actions[i].phaseTarget == null || !actions[i].phaseTarget.alive)
                    actions[i].phaseTarget = PickAliveTarget(defenders);
                if (actions[i].phaseTarget == null) actions.RemoveAt(i);
            }
            if (actions.Count == 0) yield break;

            // 2) Banner
            _statusBanner.text = isPlayerPhase
                ? "<color=#FFD466>YOUR PARTY ATTACKS!</color>"
                : "<color=#FF6677>ENEMIES ATTACK!</color>";

            // 3) SEQUENTIAL — each attacker takes their turn one at a time.
            // Order: highest-ATK first (signature opener), then descending.
            actions.Sort((x, y) => y.atk.CompareTo(x.atk));

            foreach (var a in actions)
            {
                if (!a.alive) continue;
                // Re-pick target if current one died from a previous hit this phase
                if (a.phaseTarget == null || !a.phaseTarget.alive)
                {
                    a.phaseTarget = PickAliveTarget(defenders);
                    if (a.phaseTarget == null) break;
                }
                yield return SingleAttackerSequence(a, a.phaseTarget, isPlayerPhase);
                yield return new WaitForSeconds(0.35f);   // slower beat between hits — feels more deliberate
            }

            // 4) Process deaths from this phase
            foreach (var d in defenders)
                if (d.hp <= 0 && d.alive) yield return KillFighter(d);

            // 5) Charge ult bars (player only) for surviving attackers
            if (isPlayerPhase)
            {
                foreach (var a in actions)
                {
                    if (!a.alive) continue;
                    if (a.ultDmgMult > 0 || a.ultIsHeal)
                    {
                        a.ultCharge = Mathf.Min(100f, a.ultCharge + 18f);
                        RefreshUltUI(a);
                    }
                }
            }

            _statusBanner.text = $"<color=#FFD466>{_stageTitle}</color>";
        }

        // Per-class compensation for sprites with extra transparent padding.
        // FantasyKnight + the pet droplet visually appear smaller than the chibi
        // packs which fill their frames more tightly — bump them so all
        // fighters APPEAR the same size on screen.
        private static readonly Dictionary<string, float> CLASS_SIZE_BOOST = new Dictionary<string, float>
        {
            ["knight"] = 1.55f,   // FantasyKnight has lots of frame padding
            ["pet"]    = 1.70f,   // Sweet-Droplet is a small icon
        };

        // Resize a fighter's portrait Image so its rendered HEIGHT is fixed,
        // letting WIDTH derive from the sprite's native aspect. Per-class boost
        // compensates for sprites that have heavy transparent padding.
        private static void NormalizeFighterSize(Image img, float targetHeight, string classId = null)
        {
            if (img == null || img.sprite == null) return;
            float boost = !string.IsNullOrEmpty(classId)
                && CLASS_SIZE_BOOST.TryGetValue(classId, out var b) ? b : 1.0f;
            float h = targetHeight * boost;
            float aspect = img.sprite.rect.width / Mathf.Max(1f, img.sprite.rect.height);
            img.rectTransform.sizeDelta = new Vector2(h * aspect, h);
        }

        // One fighter's complete attack sequence: wind-up → lunge → strike → recoil → return
        private static IEnumerator SingleAttackerSequence(Fighter a, Fighter target, bool isPlayerPhase)
        {
            if (a.slotRT == null || target.slotRT == null) yield break;

            Vector3 home   = a.slotRT.position;
            Vector3 tgt    = target.slotRT.position;
            // Simple horizontal lunge — no vertical arcs (those were sending fighters off-screen).
            // The slash trail particle handles the "wow" visual; the body just steps in to deliver it.
            Vector3 windUp = Vector3.Lerp(home, tgt, -0.08f);     // small step back
            Vector3 meet   = Vector3.Lerp(home, tgt, 0.78f);      // close to target but not on top
            Vector3 recoil = Vector3.Lerp(home, tgt, 0.55f);      // bounce back
            // Direction from attacker to target — used for spawning the slash particle in the right rotation
            Vector3 attackDir = (tgt - home);
            if (attackDir.sqrMagnitude < 0.001f) attackDir = Vector3.right;

            // Spotlight aura — shows the player whose turn it is (pulses under the active fighter)
            var spotlight = SpawnActiveSpotlight(a);

            a.inMotion = true;

            // 1) Wind-up — small step back (anticipation)
            yield return LerpWorldPos(a.slotRT, home, windUp, 0.16f);
            yield return new WaitForSeconds(0.08f);   // brief pause to "load"

            // Dust puff at the attacker's feet right before lunging — sells the kick-off
            SpawnDustPuff(a);

            // 2) Lunge — flat, snappy
            yield return LerpWorldPos(a.slotRT, windUp, meet, 0.18f);

            // 3) STRIKE — spawn slash trail particle just before damage applies
            SpawnSlashTrail(a, target, attackDir);
            a.swinging = true;
            a.swingTimer = 0;
            yield return new WaitForSeconds(0.06f);   // small lead-in for the slash to register

            ApplyHit(a, target, isPlayerPhase);

            // Defender knockback (subtle — just a quick reactive nudge, not flying away)
            if (target.alive && target.slotRT != null)
            {
                _runner.StartCoroutine(KnockbackTarget(target, tgt, home));
            }

            // 4) Hit-stop pause for weight
            yield return new WaitForSeconds(0.28f);
            a.swinging = false;

            // 5) Recoil — small bounce back
            if (a.alive) yield return LerpWorldPos(a.slotRT, meet, recoil, 0.12f);

            // 6) Return home — flat, no hops
            if (a.alive) yield return LerpWorldPos(a.slotRT, recoil, home, 0.20f);

            a.inMotion = false;
            // Tear down the spotlight now that this fighter's turn is done
            if (spotlight != null) Object.Destroy(spotlight);
        }

        // Pulsing colored aura under the active fighter. Shows which fighter is about to act
        // (and who's mid-action). Color = class tint. Destroyed when their sequence ends.
        private static GameObject SpawnActiveSpotlight(Fighter f)
        {
            if (f?.slotRT == null) return null;
            var go = new GameObject("ActiveSpotlight", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(f.slotRT, false);
            go.transform.SetAsFirstSibling();   // render BEHIND the fighter sprite
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 30);     // sits at fighter's feet
            rt.sizeDelta = new Vector2(280, 280);
            var img = go.GetComponent<Image>();
            Color tint = (f.isPlayer && CLASS_TINT.TryGetValue(f.classId ?? "", out var c))
                ? c : new Color(1f, 0.85f, 0.30f);
            img.color = new Color(tint.r, tint.g, tint.b, 0.55f);
            img.sprite = MakeFilledCircleSprite();
            img.raycastTarget = false;
            _runner.StartCoroutine(SpotlightPulse(go.GetComponent<RectTransform>(), img, tint));
            return go;
        }

        private static IEnumerator SpotlightPulse(RectTransform rt, Image img, Color tint)
        {
            float t = 0;
            while (rt != null && img != null)
            {
                t += Time.deltaTime;
                float pulse = 0.85f + Mathf.Sin(t * 4f) * 0.15f;
                rt.localScale = new Vector3(pulse, pulse, 1f);
                float alpha = 0.45f + Mathf.Sin(t * 4f) * 0.20f;
                img.color = new Color(tint.r, tint.g, tint.b, alpha);
                yield return null;
            }
        }

        // Dust puff at a fighter's feet — frame-animated chemical smoke (CartoonSmokeFX pack).
        // Sells the impact of leaving the ground when lunging.
        private static void SpawnDustPuff(Fighter f)
        {
            if (f?.fxLayer == null) return;
            var smoke = LoadFrameSequence(
                "Assets/CartoonSmokeFX/Chemical Smoke/PNG/Chemical Smoke_Frame_{N}.png", 1, 2);
            if (smoke == null || smoke.Length == 0) return;
            // Sized to read at slot scale, positioned at the fighter's feet
            SpawnFrameAnim(f.fxLayer, smoke,
                new Vector2(0.5f, 0.05f),         // bottom-center of the slot
                new Vector2(220, 220),
                0.05f,                              // 20fps — quick puff
                destroyOnEnd: true,
                tint: new Color(0.85f, 0.78f, 0.65f, 0.85f));   // warm dust color
        }

        // Defender knockback — pushed away from attacker briefly, then snaps back.
        // Adds reactive movement so the target doesn't just stand there absorbing hits.
        private static IEnumerator KnockbackTarget(Fighter target, Vector3 attackerPos, Vector3 attackerHome)
        {
            if (target.slotRT == null) yield break;
            target.inMotion = true;
            Vector3 home = target.slotRT.position;
            // Knockback direction = away from attacker
            Vector3 dir = (home - attackerHome).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;
            // Smaller knockback so defenders stay in their slot area
            Vector3 back = home + dir * 25f;
            yield return LerpWorldPos(target.slotRT, home, back, 0.10f);
            yield return new WaitForSeconds(0.08f);
            if (target.alive) yield return LerpWorldPos(target.slotRT, back, home, 0.18f);
            target.inMotion = false;
        }

        // Damage application, no animation — animation handled by phase
        private static void ApplyHit(Fighter attacker, Fighter target, bool isPlayerAttacker)
        {
            if (target == null || !target.alive) return;
            bool crit = Random.value < attacker.critChance;
            int raw = attacker.atk;
            int dmg = Mathf.Max(1, raw - target.def);
            if (crit) dmg = Mathf.RoundToInt(dmg * 1.6f);

            target.hp = Mathf.Max(0, target.hp - dmg);
            FlashPortrait(target.portrait, isPlayerAttacker ? new Color(1f, 0.85f, 0.45f) : new Color(1f, 0.45f, 0.45f));
            // Spine: play hit reaction → return to idle
            PlaySpineAnim(target, "Get Hit");
            ShakeOnce(target.slotRT, crit ? 14f : 8f, crit ? 0.26f : 0.16f);
            // Small whole-screen shake on every hit (bigger on crit handled below).
            // Adds physicality + breaks the repetitive feel of back-and-forth combat.
            if (!crit) ShakeOnce(_root.GetComponent<RectTransform>(), 4f, 0.10f);

            // ── Impact effect: prefer real CartoonFX particle prefabs when available ──
            // Map attacker class → vfx kind, then pick the matching CFX prefab.
            string vfxKind = (attacker.classId ?? "") switch {
                "archer"   => "arrow",
                "mage"     => "bolt",
                "assassin" => "shadow",
                "pet"      => "smoke",
                "knight"   => "slash",
                "paladin"  => "thunder",
                _          => "slash",
            };
            bool spawned = TrySpawnCfx(target, vfxKind, crit);
            // Fall back to the procedural ring + spike rays if CFX wasn't available
            if (!spawned)
            {
                Color burstColor = isPlayerAttacker ? new Color(1f, 0.85f, 0.45f) : new Color(1f, 0.45f, 0.30f);
                SpawnImpactBurst(target.fxLayer, burstColor, crit);
            }
            FloatNumber(target, crit ? $"<b>{dmg}!</b>" : $"{dmg}",
                crit ? GOLD : new Color(1f, 0.55f, 0.55f),
                crit ? 1.6f : 1.1f, crit);

            // ── HIT STOP / SCREEN FLASH on crits — "that hurt" feedback ──
            // Uses the existing FullScreenFlash machinery + brief scale punch
            // on the target slot ("freeze frame" effect selling impact).
            if (crit)
            {
                Color flashColor = isPlayerAttacker
                    ? new Color(1f, 0.95f, 0.55f, 0.45f)    // golden flash on player crit
                    : new Color(1f, 0.30f, 0.30f, 0.45f);   // red flash on enemy crit
                FullScreenFlash(flashColor, 0.18f);
                // Extra screen shake — punchier than the per-slot one
                ShakeOnce(_root.GetComponent<RectTransform>(), 10f, 0.25f);
                // Target scale punch — 1.15× momentary then snap back
                _runner.StartCoroutine(HitPosePunch(target.slotRT, 0.10f));
            }

            // Audio
            if (isPlayerAttacker)
            {
                PlayAudio(attacker.classId == "archer" ? SFX_WIND
                        : attacker.classId == "mage" ? SFX_THUNDER
                        : SFX_SLASH, crit ? 1.0f : 0.85f);
                _runner.StartCoroutine(DelayedAudio(Random.value < 0.5f ? SFX_FLESH : SFX_FLESH2, 0.06f, crit ? 1.0f : 0.80f));
            }
            else
            {
                PlayAudio(SFX_SLASH, crit ? 0.95f : 0.75f);
                _runner.StartCoroutine(DelayedAudio(SFX_FLESH, 0.06f, 0.80f));
            }

            // Build target's ult charge (player only)
            if (target.isPlayer && (target.ultDmgMult > 0 || target.ultIsHeal))
            {
                target.ultCharge = Mathf.Min(100f, target.ultCharge + 18f);
                RefreshUltUI(target);
            }
        }

        private static IEnumerator WaveIntro()
        {
            bool boss = _bossEncounter && _waveIndex == _waves.Length - 1;
            _waveText.text = $"WAVE {_waveIndex + 1} / {_waves.Length}" + (boss ? "  <color=#FF6677>BOSS</color>" : "");
            _statusBanner.text = boss
                ? $"<color=#FF6677>BOSS WAVE!</color>"
                : $"<color=#FFD466>{_stageTitle}</color>";

            PlayAudio(SFX_ENCOUNTER, 0.85f);
            FullScreenFlash(boss ? new Color(1f, 0.30f, 0.30f, 0.55f) : new Color(1f, 0.85f, 0.30f, 0.40f), 0.45f);
            ShakeOnce(_root.GetComponent<RectTransform>(), boss ? 12f : 6f, 0.32f);

            // Swap to boss music for final wave
            #if UNITY_EDITOR
            if (boss && _music != null)
            {
                string bossPath = BGM_BOSS[Random.Range(0, BGM_BOSS.Length)];
                Sparq.UI.HomeBgm.ConfigureForStreaming(bossPath);
                var bgm = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(bossPath);
                if (bgm != null) { _music.clip = bgm; _music.Play(); }
            }
            #endif

            // ── Cinematic banner sweep ──
            // Big text swooshes in from off-screen left, holds, then exits right.
            yield return SwooshWaveBanner(boss);
        }

        // Cinematic wave-intro banner: huge text slides in from off-screen, holds with a
        // pulse, slides out. Adds the "polish" that mobile RPGs use to make wave starts
        // feel like real moments rather than just a banner text change.
        private static IEnumerator SwooshWaveBanner(bool boss)
        {
            var go = new GameObject("WaveBannerSwoosh", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, 140);

            // Dark stripe behind the text so it pops on any background
            var bgImg = MakeImage(go.transform, "Bg",
                boss ? new Color(0.40f, 0.05f, 0.10f, 0.78f)
                     : new Color(0.10f, 0.05f, 0.20f, 0.78f));
            var bgRT = bgImg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bgImg.raycastTarget = false;

            // Gold accent line top + bottom
            void MakeAccent(float y, Color col)
            {
                var line = MakeImage(go.transform, "Accent", col);
                var lrt = line.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, y); lrt.anchorMax = new Vector2(1, y);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.sizeDelta = new Vector2(0, 4);
                line.raycastTarget = false;
            }
            Color accentColor = boss ? new Color(1f, 0.40f, 0.30f, 1f) : new Color(1f, 0.78f, 0.30f, 1f);
            MakeAccent(0f, accentColor);
            MakeAccent(1f, accentColor);

            // Big swooshing text
            string headline = boss
                ? "<color=#FF6677>BOSS WAVE</color>"
                : $"<color=#FFD466>WAVE {_waveIndex + 1}</color>  <size=70%><color=#FFFFFFAA>OF {_waves.Length}</color></size>";
            var txt = MakeText(go.transform, "Txt", headline,
                new Vector2(0.5f, 0.5f), new Vector2(900, 100), 78, FontStyles.Bold, CREAM);
            txt.alignment = TextAlignmentOptions.Center;
            txt.outlineWidth = 0.30f; txt.outlineColor = Color.black;
            txt.richText = true;
            txt.raycastTarget = false;
            var txtRT = txt.rectTransform;
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

            // Calm mode: show the banner static (no slide / pulse), hold, remove.
            if (Accessibility.ReducedMotion)
            {
                rt.anchoredPosition = Vector2.zero;
                yield return new WaitForSeconds(1.1f);
                if (go != null) Object.Destroy(go);
                yield break;
            }

            // Swoosh in from left → hold with pulse → swoosh out to right
            float screenW = 1200f;
            // Phase 1: enter (0.30s) — slide from -screenW to center
            float t = 0; float dur = 0.30f;
            while (t < dur)
            {
                float p = t / dur;
                float eased = 1f - Mathf.Pow(1f - p, 3);   // cubic-out
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-screenW, 0, eased), 0);
                t += Time.deltaTime; yield return null;
            }
            rt.anchoredPosition = Vector2.zero;

            // Phase 2: hold + pulse (0.65s)
            float hold = 0.65f; float ht = 0;
            while (ht < hold)
            {
                float pulse = 1f + Mathf.Sin(ht * 12f) * 0.04f;
                txtRT.localScale = new Vector3(pulse, pulse, 1f);
                ht += Time.deltaTime; yield return null;
            }
            txtRT.localScale = Vector3.one;

            // Phase 3: exit (0.25s) — slide center → +screenW
            t = 0; dur = 0.25f;
            while (t < dur)
            {
                float p = t / dur;
                float eased = p * p;   // quadratic-in (accelerates out)
                rt.anchoredPosition = new Vector2(Mathf.Lerp(0, screenW, eased), 0);
                t += Time.deltaTime; yield return null;
            }

            Object.Destroy(go);
        }

        private static Fighter PickAliveTarget(List<Fighter> list)
        {
            var alive = list.FindAll(x => x.alive);
            if (alive.Count == 0) return null;
            return alive[Random.Range(0, alive.Count)];
        }

        private static bool AllDead(List<Fighter> list)
        {
            foreach (var f in list) if (f.alive) return false;
            return true;
        }

        // ═══════════════════════════════════════════════════════════════════
        // COMBAT
        // ═══════════════════════════════════════════════════════════════════
        // World-space position lerp for the slot rect
        private static IEnumerator LerpWorldPos(RectTransform rt, Vector3 a, Vector3 b, float dur)
        {
            if (rt == null) yield break;
            float t = 0;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                // Ease — quadratic-out for snappy "clunky" feel
                float eased = 1f - (1f - p) * (1f - p);
                rt.position = Vector3.Lerp(a, b, eased);
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) rt.position = b;
        }

        private static void OnUltTapped(Fighter f)
        {
            if (!f.isPlayer || f.ultCharge < 100f || !f.alive) return;
            // Queue ult for the next player phase (cinematic — fires synced with the squad charge)
            f.ultArmed = true;
            f.ultCharge = 0;
            RefreshUltUI(f);
            PlayAudio(SFX_CONFIRM, 0.7f);
            _statusBanner.text = $"<color=#FFD466>{f.name} READY: {f.ultName}</color>";
        }

        private static IEnumerator DoUlt(Fighter f)
        {
            f.ultCharge = 0; RefreshUltUI(f);
            PlayAudio(SFX_CONFIRM, 0.7f);
            PlayAudio(SFX_CHARGE, 0.85f);
            FullScreenFlash(f.ultColor, 0.40f);
            ShakeOnce(_root.GetComponent<RectTransform>(), 12f, 0.32f);

            _statusBanner.text = $"<color=#FFD466>{f.name}</color>  →  <color=#FFD466>{f.ultName}</color>!";

            // AoE ring at all enemies' positions for AoE ults
            if (f.ultIsAOE)
            {
                foreach (var enemy in _enemies)
                {
                    if (!enemy.alive) continue;
                    // Big CFX explosion on every enemy; falls back to procedural ring if CFX not available
                    if (!TrySpawnCfx(enemy, f.ultVfx ?? "fire", true))
                        SpawnAoERing(enemy.fxLayer, f.ultColor);
                }
                yield return new WaitForSeconds(0.32f);
                int dmgPerTarget = Mathf.RoundToInt(f.atk * f.ultDmgMult);
                foreach (var enemy in _enemies)
                {
                    if (!enemy.alive) continue;
                    int dmg = Mathf.Max(1, dmgPerTarget - enemy.def);
                    enemy.hp = Mathf.Max(0, enemy.hp - dmg);
                    FlashPortrait(enemy.portrait, f.ultColor);
                    ShakeOnce(enemy.slotRT, 14f, 0.28f);
                    FloatNumber(enemy, $"<b>{dmg}</b>", f.ultColor, 1.7f, true);
                    if (enemy.hp <= 0) _runner.StartCoroutine(KillFighter(enemy));
                }
                PlayAudio(f.ultSfx, 0.95f);
            }
            else if (f.ultIsHeal)
            {
                int healAmount = Mathf.RoundToInt(f.atk * 3f) + 25;
                foreach (var ally in _party)
                {
                    if (!ally.alive) continue;
                    int actually = Mathf.Min(healAmount, ally.maxHp - ally.hp);
                    if (actually <= 0) continue;
                    ally.hp += actually;
                    FloatNumber(ally, $"+{actually}", new Color(0.55f, 1f, 0.55f), 1.5f, false);
                    if (!TrySpawnCfx(ally, "shield", false))
                        SpawnAoERing(ally.fxLayer, new Color(0.55f, 1f, 0.55f));
                }
                PlayAudio(SFX_HEAL, 0.95f);
            }
            else
            {
                // Single-target ult
                var target = PickAliveTarget(_enemies);
                if (target == null) yield break;
                yield return new WaitForSeconds(0.18f);
                int dmg = Mathf.Max(1, Mathf.RoundToInt(f.atk * f.ultDmgMult) - target.def);
                target.hp = Mathf.Max(0, target.hp - dmg);
                FlashPortrait(target.portrait, f.ultColor);
                ShakeOnce(target.slotRT, 16f, 0.32f);
                FloatNumber(target, $"<b>{dmg}!</b>", GOLD, 1.9f, true);
                PlayAudio(f.ultSfx, 0.95f);
                if (target.hp <= 0) yield return KillFighter(target);
            }

            yield return new WaitForSeconds(0.4f);
            _statusBanner.text = $"<color=#FFD466>{_stageTitle}</color>";
        }

        private static IEnumerator KillFighter(Fighter f)
        {
            if (!f.alive) yield break;
            f.alive = false;
            PlayAudio(SFX_DEATH, 0.85f);
            // Spine: play death animation (no return-to-idle, they stay collapsed)
            PlaySpineAnim(f, "Death", returnToIdle: false);

            // 1) Death burst — radial impact + smoke puff at the death spot (sells the moment)
            if (f.fxLayer != null)
            {
                SpawnImpactBurst(f.fxLayer,
                    f.isPlayer ? new Color(0.95f, 0.55f, 0.45f) : new Color(0.85f, 0.30f, 0.30f),
                    crit: true);
                // Frame-animated smoke poof
                var smoke = LoadFrameSequence(
                    "Assets/CartoonSmokeFX/Smoke Explosion/PNG/Smoke Explosion_Frame_{N}.png", 1, 2);
                if (smoke == null || smoke.Length == 0)
                    smoke = LoadFrameSequence(
                        "Assets/CartoonSmokeFX/Chemical Smoke/PNG/Chemical Smoke_Frame_{N}.png", 1, 2);
                if (smoke != null && smoke.Length > 0)
                    SpawnFrameAnim(f.fxLayer, smoke, new Vector2(0.5f, 0.5f),
                        new Vector2(320, 320), 0.05f, true,
                        new Color(0.85f, 0.65f, 0.55f, 0.95f));
            }

            // 2) Body animation: brief upward kick → spin + shrink + fade
            float t = 0; float dur = 0.85f;
            var rt = f.slotRT;
            if (rt == null) yield break;          // slot already destroyed; nothing to animate
            var img = f.portrait;                  // null if Spine path was used — guard everywhere
            Vector3 baseScale = rt.localScale;
            Vector2 basePos = rt.anchoredPosition;
            Color baseColor = img != null ? img.color : Color.white;
            // First 25% — quick kick up + initial spin
            while (t < dur && rt != null)
            {
                float p = t / dur;
                // Up + drift sideways like a knockback
                float upArc = Mathf.Sin(p * Mathf.PI * 0.5f) * 60f;       // peaks early
                float side = (f.isPlayer ? -1f : 1f) * p * 30f;
                rt.anchoredPosition = basePos + new Vector2(side, upArc);
                // Faster spin into the death pose
                float spinAmount = Mathf.Lerp(0, f.isPlayer ? -120f : 120f, p);
                rt.localRotation = Quaternion.Euler(0, 0, spinAmount);
                // Shrink with squash-and-stretch overshoot for character
                float scaleK = (p < 0.30f)
                    ? Mathf.Lerp(1f, 1.15f, p / 0.30f)        // brief grow
                    : Mathf.Lerp(1.15f, 0.0f, (p - 0.30f) / 0.70f);   // then collapse to 0
                rt.localScale = baseScale * scaleK;
                if (img != null)
                    img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - Mathf.Pow(p, 1.4f));
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) rt.gameObject.SetActive(false);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ANIMATION (idle bob + frame swap on attack)
        // ═══════════════════════════════════════════════════════════════════
        private static IEnumerator IdleAnimateAll()
        {
            // Slower frame rate for chunkier, more deliberate animation
            float frameDur = 0.20f;   // ~5 fps idle (clunky)
            while (_root != null)
            {
                float dt = Time.deltaTime;
                AnimateOne(_party, dt, frameDur);
                AnimateOne(_enemies, dt, frameDur);
                yield return null;
            }
        }

        private static void AnimateOne(List<Fighter> list, float dt, float frameDur)
        {
            foreach (var f in list)
            {
                if (!f.alive || f.portrait == null) continue;
                f.frameTimer += dt;
                Sprite[] active;
                if (f.swinging && f.attackFrames != null && f.attackFrames.Length > 0)
                {
                    f.swingTimer += dt;
                    active = f.attackFrames;
                    if (f.swingTimer >= 0.18f) f.swinging = false;
                }
                else
                {
                    active = f.idleFrames;
                }
                if (active == null || active.Length == 0) continue;
                if (f.frameTimer >= frameDur)
                {
                    f.frameTimer = 0;
                    f.frameIdx = (f.frameIdx + 1) % active.Length;
                    f.portrait.sprite = active[f.frameIdx];
                    NormalizeFighterSize(f.portrait, FIGHTER_TARGET_HEIGHT, f.classId);
                }

                // ── ALIVE-LOOKING IDLE: bob + sway + breathing scale ──
                // Each fighter gets a slightly different rhythm so they don't all
                // pulse in lockstep (uses homePos.x as a per-fighter phase offset).
                if (!f.swinging && !f.inMotion && f.slotRT != null)
                {
                    float t = Time.time;
                    float phase = f.homePos.x * 0.01f;
                    // Vertical bob — bigger amplitude so it's actually visible
                    float bob = Mathf.Sin(t * 2.4f + phase) * 14f;
                    // Horizontal sway — slower, half amplitude (rocking weight back and forth)
                    float sway = Mathf.Sin(t * 1.3f + phase * 1.7f) * 6f;
                    f.slotRT.anchoredPosition = f.homePos + new Vector2(sway, bob);

                    // Breathing scale — slight Y stretch + tiny X squash (natural breath).
                    // Slot itself stays unflipped — portrait child carries the enemy mirror.
                    float breathY = 1f + Mathf.Sin(t * 1.8f + phase) * 0.04f;
                    float breathX = 1f - (breathY - 1f) * 0.5f;
                    f.slotRT.localScale = new Vector3(breathX, breathY, 1f);
                }
            }
        }

        private static IEnumerator LerpHpBars()
        {
            while (_root != null)
            {
                LerpHp(_party);
                LerpHp(_enemies);
                RefreshHudBars();   // keep bottom HP/MP HUD in sync with player hero
                RefreshBossHpBar(); // boss top bar
                yield return null;
            }
        }

        private static void LerpHp(List<Fighter> list)
        {
            foreach (var f in list)
            {
                if (f.hpBar == null) continue;
                f.hpDisplayed = Mathf.MoveTowards(f.hpDisplayed, f.hp, Mathf.Max(15, f.maxHp) * Time.deltaTime * 1.4f);
                f.hpBar.value = f.hpDisplayed;
                if (f.hpFill != null)
                {
                    float ratio = f.maxHp > 0 ? f.hp / (float)f.maxHp : 0;
                    Color baseTint = f.hpFill.color;
                    Color lo = ratio < 0.30f ? HP_RED : (ratio < 0.55f ? HP_AMBER : baseTint);
                    f.hpFill.color = Color.Lerp(f.hpFill.color, lo, Time.deltaTime * 6f);
                }
            }
        }

        private static void RefreshUltUI(Fighter f)
        {
            if (f.ultBar == null) return;
            f.ultBar.value = f.ultCharge;
            bool full = f.ultCharge >= 100f;
            if (f.ultButton != null) f.ultButton.interactable = full;
            if (f.ultGlow != null)
                f.ultGlow.color = new Color(ULT_PURPLE.r, ULT_PURPLE.g, ULT_PURPLE.b, full ? 0.55f : 0f);
            if (f.ultFill != null)
                f.ultFill.color = full ? Color.Lerp(ULT_PURPLE, GOLD, 0.5f) : ULT_PURPLE;
        }

        // ═══════════════════════════════════════════════════════════════════
        // VFX
        // ═══════════════════════════════════════════════════════════════════
        // Tracks the in-progress flash coroutine per portrait so overlapping hits
        // don't stack. WITHOUT this, two hits within 0.10s made the second flash
        // capture the already-red color as its "restore" target → the fighter got
        // stuck permanently red (the "why are the good guys red?" bug). We now
        // cancel any running flash and always restore to the true base (white).
        private static readonly Dictionary<Image, Coroutine> _flashing = new Dictionary<Image, Coroutine>();
        private static void FlashPortrait(Image img, Color tint)
        {
            if (img == null || _runner == null) return;
            if (_flashing.TryGetValue(img, out var co) && co != null) _runner.StopCoroutine(co);
            _flashing[img] = _runner.StartCoroutine(FlashRoutine(img, tint));
        }
        private static IEnumerator FlashRoutine(Image img, Color tint)
        {
            img.color = tint;
            yield return new WaitForSeconds(0.10f);
            // Restore: white if a sprite is loaded, transparent if not (so
            // characters without art don't flash a white box on hit).
            if (img != null)
                img.color = img.sprite != null ? Color.white : new Color(0, 0, 0, 0);
            _flashing.Remove(img);
        }

        private static void ShakeOnce(RectTransform rt, float magnitude, float duration)
        {
            if (rt == null) return;
            if (Accessibility.ReducedMotion) return;   // calm mode: no shakes
            _runner.StartCoroutine(ShakeRoutine(rt, magnitude, duration));
        }
        private static IEnumerator ShakeRoutine(RectTransform rt, float mag, float dur)
        {
            Vector2 home = rt.anchoredPosition;
            float t = 0;
            while (t < dur && rt != null)
            {
                rt.anchoredPosition = home + new Vector2(Random.Range(-mag, mag), Random.Range(-mag, mag));
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) rt.anchoredPosition = home;
        }

        // Brief scale punch on a fighter slot — sells the impact moment of a
        // crit. Grows to 1.15× then snaps back over `dur`.
        private static IEnumerator HitPosePunch(RectTransform rt, float dur)
        {
            if (rt == null) yield break;
            if (Accessibility.ReducedMotion) yield break;   // calm mode: no scale punch
            Vector3 baseScale = rt.localScale;
            float t = 0;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                // Quick swell to 1.15× in first half, then ease back to 1.0
                float k = (p < 0.4f)
                    ? Mathf.Lerp(1f, 1.15f, p / 0.4f)
                    : Mathf.Lerp(1.15f, 1f, (p - 0.4f) / 0.6f);
                rt.localScale = baseScale * k;
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) rt.localScale = baseScale;
        }

        private static void FullScreenFlash(Color c, float dur)
        {
            if (_fullScreenFlash == null) return;
            // Calm mode: skip full-screen flashes entirely (photosensitivity).
            if (Accessibility.ReducedMotion) { _fullScreenFlash.color = new Color(c.r, c.g, c.b, 0); return; }
            _runner.StartCoroutine(FlashFS(c, dur));
        }
        private static IEnumerator FlashFS(Color c, float dur)
        {
            float t = 0;
            while (t < dur && _fullScreenFlash != null)
            {
                float p = t / dur;
                _fullScreenFlash.color = new Color(c.r, c.g, c.b, c.a * (1f - p));
                t += Time.deltaTime; yield return null;
            }
            if (_fullScreenFlash != null) _fullScreenFlash.color = new Color(c.r, c.g, c.b, 0);
        }

        // ── Frame-animation cache ──
        // Loaded sprite sequences are cached so repeat plays don't re-load from AssetDatabase.
        private static readonly Dictionary<string, Sprite[]> _frameCache = new Dictionary<string, Sprite[]>();

        // Load a numbered frame sequence from disk, auto-importing each PNG as a Sprite if needed.
        // pathTemplate uses "{N}" placeholder, e.g. "Assets/.../Frame_{N}.png" — N gets padded.
        // Stops loading at the first missing frame (so you don't need to know exact frame count).
        private static Sprite[] LoadFrameSequence(string pathTemplate, int startIdx, int padding, int maxFrames = 100)
        {
            #if UNITY_EDITOR
            string cacheKey = pathTemplate;
            if (_frameCache.TryGetValue(cacheKey, out var cached)) return cached;

            var list = new List<Sprite>();
            for (int i = startIdx; i < startIdx + maxFrames; i++)
            {
                string num = i.ToString().PadLeft(padding, '0');
                string path = pathTemplate.Replace("{N}", num);
                // Make sure the asset is imported as Sprite (not raw Texture2D)
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                    imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp == null) break;   // first missing frame → stop
                list.Add(sp);
            }
            var arr = list.ToArray();
            _frameCache[cacheKey] = arr;
            return arr;
            #else
            return new Sprite[0];
            #endif
        }

        // Spawn a frame-animated effect inside a parent (canvas-relative).
        // Uses normalized anchor position (0..1, 0..1) within parent's RectTransform.
        private static GameObject SpawnFrameAnim(Transform parent, Sprite[] frames, Vector2 anchorPos,
            Vector2 size, float frameDur, bool destroyOnEnd = true, Color? tint = null)
        {
            if (parent == null || frames == null || frames.Length == 0) return null;
            var go = new GameObject("FrameAnim", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchorPos;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = tint ?? Color.white;
            img.raycastTarget = false;
            img.preserveAspect = true;
            _runner.StartCoroutine(FrameAnimPlay(go, img, frames, frameDur, destroyOnEnd));
            return go;
        }

        private static IEnumerator FrameAnimPlay(GameObject go, Image img, Sprite[] frames, float frameDur, bool destroyOnEnd)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (go == null || img == null) yield break;
                img.sprite = frames[i];
                yield return new WaitForSeconds(frameDur);
            }
            if (destroyOnEnd && go != null) Object.Destroy(go);
        }

        // Strike VFX — picks the best visual per class.
        //   mage / paladin / bolt-class → Lightning frame animation (WindLightningFX pack)
        //   everything else            → procedural slash arc
        private static void SpawnSlashTrail(Fighter attacker, Fighter target, Vector3 attackDir)
        {
            if (attacker == null || target == null || target.fxLayer == null) return;
            string cls = attacker.classId ?? "";

            // Lightning path for electric-themed classes
            if (cls == "mage" || cls == "paladin")
            {
                var lightning = LoadFrameSequence(
                    "Assets/WindLightningFX/Lightning Spell/PNG/Lightning Spell_Frame_{N}.png", 1, 2);
                if (lightning != null && lightning.Length > 0)
                {
                    SpawnFrameAnim(target.fxLayer, lightning,
                        new Vector2(0.5f, 0.55f), new Vector2(420, 420), 0.04f);
                    return;
                }
            }

            // Default slash arc (procedural)
            Color col = (attacker.isPlayer && CLASS_TINT.TryGetValue(cls, out var t))
                ? t : new Color(1f, 0.85f, 0.45f);
            col = new Color(
                Mathf.Min(1f, col.r * 1.3f + 0.2f),
                Mathf.Min(1f, col.g * 1.3f + 0.2f),
                Mathf.Min(1f, col.b * 1.3f + 0.2f),
                1f);

            var go = new GameObject("SlashArc", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(target.fxLayer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(60, 30);
            float angle = 45f + Random.Range(-25f, 25f);
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            var img = go.GetComponent<Image>();
            img.color = col;
            img.sprite = MakeEllipseSprite();
            img.raycastTarget = false;
            _runner.StartCoroutine(SlashArcAnim(rt, img, col));
        }

        private static IEnumerator SlashArcAnim(RectTransform rt, Image img, Color baseColor)
        {
            float dur = 0.30f, t = 0;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                // Stretch outward fast, shrink slightly, fade out
                float w = Mathf.Lerp(60f, 480f, p);
                float h = Mathf.Lerp(30f, 12f, p);
                rt.sizeDelta = new Vector2(w, h);
                float alpha = 1f - Mathf.Pow(p, 1.5f);
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        // Try to spawn a real CartoonFX particle prefab at the target's screen position.
        // Returns true if a particle was spawned, false if we should fall back to procedural.
        private static bool TrySpawnCfx(Fighter target, string vfxKind, bool ult)
        {
            if (!_useCfxParticles) return false;
            #if UNITY_EDITOR
            // Pick prefab: ult tier gets a beefier explosion
            string key = ult ? "ult_burst" : vfxKind;
            if (!CFX_BY_VFX.ContainsKey(key)) key = vfxKind;
            if (!CFX_BY_VFX.ContainsKey(key)) return false;

            // Cache lookup
            if (!_cfxCache.TryGetValue(key, out var prefab) || prefab == null)
            {
                prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CFX_BY_VFX[key]);
                _cfxCache[key] = prefab;
            }
            if (prefab == null) return false;

            // Compute world position from the target slot's screen position
            // The slot's transform.position is already in world space (canvas is at planeDistance from camera)
            Vector3 worldPos = target.slotRT.position;
            // Bump Z toward camera so particles render in front of canvas elements
            worldPos.z = _cfxLayerZ;

            var go = Object.Instantiate(prefab, worldPos, Quaternion.identity);
            // Most CFX prefabs auto-destroy via their CFXR script; failsafe destroy after 4s
            Object.Destroy(go, 4f);
            return true;
            #else
            return false;
            #endif
        }

        // Big impact burst — bright flash, multiple expanding rings, more spike rays,
        // plus a screen-wide tint pulse on crits. Designed to feel HEAVY when hits land.
        private static void SpawnImpactBurst(Transform parent, Color color, bool crit)
        {
            if (parent == null) return;
            // 1) White-hot flash circle (much bigger now)
            var flash = MakeFxImage(parent, "BurstFlash", new Color(1, 1, 1, 0.95f), MakeFilledCircleSprite(), 120);
            _runner.StartCoroutine(BurstFlashAnim(flash.GetComponent<RectTransform>(), flash, crit ? 0.36f : 0.26f, crit ? 6.0f : 4.0f));

            // 2) Two expanding rings — outer + inner — for layered shockwave
            var ring1 = MakeFxImage(parent, "BurstRing1", new Color(color.r, color.g, color.b, 0.95f), MakeRingSprite(), 100);
            _runner.StartCoroutine(BurstRingAnim(ring1.GetComponent<RectTransform>(), ring1, crit ? 0.50f : 0.36f, crit ? 7.0f : 5.0f));
            var ring2 = MakeFxImage(parent, "BurstRing2", new Color(1, 1, 1, 0.85f), MakeRingSprite(), 80);
            _runner.StartCoroutine(BurstRingAnim(ring2.GetComponent<RectTransform>(), ring2, crit ? 0.40f : 0.28f, crit ? 4.5f : 3.2f));

            // 3) Radial spike rays — more of them, longer
            int spikes = crit ? 12 : 8;
            for (int i = 0; i < spikes; i++)
            {
                float angle = (360f / spikes) * i + Random.Range(-12f, 12f);
                _runner.StartCoroutine(SpawnSpike(parent, color, angle, crit));
            }

            // 4) Camera-wide tint flash on crit — really sells the impact
            if (crit && _fullScreenFlash != null)
            {
                _runner.StartCoroutine(CritScreenFlash(color));
                ShakeOnce(_root.GetComponent<RectTransform>(), 18f, 0.30f);
            }
        }

        // Brief screen-wide colored flash on crit — quick punch, fades fast.
        private static IEnumerator CritScreenFlash(Color color)
        {
            if (_fullScreenFlash == null) yield break;
            float dur = 0.18f; float t = 0;
            Color start = new Color(color.r, color.g, color.b, 0.42f);
            while (t < dur && _fullScreenFlash != null)
            {
                float p = t / dur;
                _fullScreenFlash.color = new Color(start.r, start.g, start.b, start.a * (1f - p));
                t += Time.deltaTime; yield return null;
            }
            if (_fullScreenFlash != null) _fullScreenFlash.color = new Color(0, 0, 0, 0);
        }

        private static Image MakeFxImage(Transform parent, string name, Color color, Sprite sprite, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        private static IEnumerator BurstFlashAnim(RectTransform rt, Image img, float dur, float maxScale)
        {
            float t = 0;
            Color baseC = img.color;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(0.4f, maxScale, p);
                img.color = new Color(baseC.r, baseC.g, baseC.b, baseC.a * (1f - p));
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static IEnumerator BurstRingAnim(RectTransform rt, Image img, float dur, float maxScale)
        {
            float t = 0;
            Color baseC = img.color;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(0.5f, maxScale, p);
                img.color = new Color(baseC.r, baseC.g, baseC.b, baseC.a * (1f - p * p));
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static IEnumerator SpawnSpike(Transform parent, Color color, float angleDeg, bool crit)
        {
            var go = new GameObject("Spike", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.0f, 0.5f);   // pivot at one end so it fires outward
            rt.sizeDelta = new Vector2(crit ? 130 : 90, crit ? 14 : 10);
            rt.localRotation = Quaternion.Euler(0, 0, angleDeg);
            var img = go.GetComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 1f);
            img.sprite = MakeRectSprite();
            img.raycastTarget = false;

            float dur = crit ? 0.32f : 0.22f;
            float t = 0;
            float baseLen = rt.sizeDelta.x;
            Color baseC = img.color;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                // grow then fade
                rt.sizeDelta = new Vector2(baseLen * Mathf.Lerp(0.4f, 1.6f, p), rt.sizeDelta.y);
                img.color = new Color(baseC.r, baseC.g, baseC.b, 1f - p);
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void SpawnAoERing(Transform parent, Color color)
        {
            if (parent == null) return;
            var go = new GameObject("AoE", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(60, 60);
            var img = go.GetComponent<Image>();
            img.color = new Color(color.r, color.g, color.b, 0.65f);
            img.sprite = MakeRingSprite();
            img.raycastTarget = false;
            _runner.StartCoroutine(AoEAnim(rt, img, color));
        }

        private static IEnumerator AoEAnim(RectTransform rt, Image img, Color color)
        {
            float t = 0; float dur = 0.55f;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                float s = Mathf.Lerp(0.5f, 4.5f, p);
                rt.localScale = new Vector3(s, s, 1f);
                img.color = new Color(color.r, color.g, color.b, 0.75f * (1f - p));
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void FloatNumber(Fighter f, string text, Color color, float scale, bool crit)
        {
            if (f.fxLayer == null) return;
            var go = new GameObject("Float", typeof(RectTransform));
            go.transform.SetParent(f.fxLayer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.7f); rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280, 90);
            var tm = go.AddComponent<TextMeshProUGUI>();
            // Crits get formatted with golden glow + size bump; normals get a clean outline
            if (crit)
            {
                tm.text = $"<color=#FFD466>{text}</color>";
                tm.fontSize = Mathf.RoundToInt(72 * scale);
                tm.outlineWidth = 0.35f;
                tm.outlineColor = new Color(0.55f, 0.20f, 0.10f, 1f);   // deep red-brown halo for gold contrast
            }
            else
            {
                tm.text = text;
                tm.fontSize = Mathf.RoundToInt(46 * scale);
                tm.outlineWidth = 0.30f;
                tm.outlineColor = Color.black;
            }
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = color;
            tm.raycastTarget = false;
            tm.richText = true;
            _runner.StartCoroutine(FloatAnim(rt, tm, crit));
        }
        private static IEnumerator FloatAnim(RectTransform rt, TMP_Text tm, bool crit)
        {
            float dur = crit ? 1.5f : 0.95f;
            float t = 0;
            Vector2 home = rt.anchoredPosition;
            float driftX = Random.Range(-40f, 40f);
            Color baseColor = tm.color;
            // Crits get a "punch in" that overshoots, then settles, then floats up
            float punchIn = crit ? 0.18f : 0.0f;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                float arcY = 130f * Mathf.Sin(p * Mathf.PI * 0.5f);
                rt.anchoredPosition = home + new Vector2(driftX * p, arcY);
                // Crit scale punch: 0→1 oversized punch in, then ease to 1.0
                float scaleK;
                if (crit && t < punchIn)
                    scaleK = Mathf.Lerp(2.4f, 1.5f, t / punchIn);    // dramatic punch
                else
                    scaleK = Mathf.Lerp(crit ? 1.5f : 1.4f, 1.0f, p);
                rt.localScale = Vector3.one * scaleK;
                tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, p < 0.75f ? 1f : 1f - (p - 0.75f) / 0.25f);
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        // ═══════════════════════════════════════════════════════════════════
        // WIN/LOSE
        // ═══════════════════════════════════════════════════════════════════
        private static IEnumerator WaveCleared()
        {
            // Rewards scale with zone difficulty so deeper zones level you faster.
            int xp   = Mathf.RoundToInt((20 + _waveIndex * 15) * _difficulty);
            int gold = Mathf.RoundToInt((15 + _waveIndex * 12) * _difficulty);
            _xpReward += xp;
            _goldReward += gold;
            _waveIndex++;
            _statusBanner.text = $"<color=#88FF88>WAVE CLEARED</color>  +{xp} XP  +{gold} g";
            FullScreenFlash(new Color(0.55f, 1f, 0.55f, 0.30f), 0.4f);
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}

            // ── Wave-clear stamp banner — slides in, holds, slides out ──
            yield return WaveClearStamp(xp, gold);
        }

        // Big "WAVE CLEAR ✓" stamp that swooshes in from above, holds with bounce, then exits.
        // Adds professional polish to the moment between waves.
        private static IEnumerator WaveClearStamp(int xp, int gold)
        {
            var go = new GameObject("WaveClearStamp", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(680, 200);
            rt.anchoredPosition = new Vector2(0, 600);   // start above the screen

            // Dark plate
            var plate = MakeImage(go.transform, "Plate", new Color(0.04f, 0.10f, 0.06f, 0.92f));
            var plRT = plate.GetComponent<RectTransform>();
            plRT.anchorMin = Vector2.zero; plRT.anchorMax = Vector2.one;
            plRT.offsetMin = Vector2.zero; plRT.offsetMax = Vector2.zero;
            plate.raycastTarget = false;

            // Green accent border
            var border = MakeImage(go.transform, "Border", new Color(0.45f, 0.95f, 0.55f, 0.95f));
            var brRT = border.GetComponent<RectTransform>();
            brRT.anchorMin = Vector2.zero; brRT.anchorMax = Vector2.one;
            brRT.offsetMin = new Vector2(-4, -4); brRT.offsetMax = new Vector2(4, 4);
            border.transform.SetAsFirstSibling();
            border.raycastTarget = false;

            // Headline + reward line
            var headline = MakeText(go.transform, "Headline",
                "<color=#88FF88>✓  WAVE CLEARED</color>",
                new Vector2(0.5f, 0.65f), new Vector2(660, 70), 56, FontStyles.Bold, CREAM);
            headline.alignment = TextAlignmentOptions.Center;
            headline.outlineWidth = 0.30f; headline.outlineColor = Color.black;
            headline.richText = true; headline.raycastTarget = false;

            var rewardTxt = MakeText(go.transform, "Reward",
                $"<color=#FFD466>+{xp} XP</color>   <color=#FFB266>+{gold} GOLD</color>",
                new Vector2(0.5f, 0.30f), new Vector2(660, 50), 32, FontStyles.Bold, CREAM);
            rewardTxt.alignment = TextAlignmentOptions.Center;
            rewardTxt.outlineWidth = 0.22f; rewardTxt.outlineColor = Color.black;
            rewardTxt.richText = true; rewardTxt.raycastTarget = false;

            // Phase 1: drop from above (cubic-out)
            float t = 0; float dur = 0.30f;
            while (t < dur)
            {
                float p = t / dur; float eased = 1f - Mathf.Pow(1f - p, 3);
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(600, 0, eased));
                t += Time.deltaTime; yield return null;
            }
            rt.anchoredPosition = Vector2.zero;

            // Phase 2: bounce
            t = 0; dur = 0.20f;
            while (t < dur)
            {
                float p = t / dur;
                float bounce = -40f * Mathf.Sin(p * Mathf.PI);   // single small bounce
                rt.anchoredPosition = new Vector2(0, bounce);
                t += Time.deltaTime; yield return null;
            }
            rt.anchoredPosition = Vector2.zero;

            // Phase 3: hold
            yield return new WaitForSeconds(0.55f);

            // Phase 4: exit upward (accelerating)
            t = 0; dur = 0.22f;
            while (t < dur)
            {
                float p = t / dur; float eased = p * p;
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(0, 700, eased));
                t += Time.deltaTime; yield return null;
            }
            Object.Destroy(go);
        }

        private static IEnumerator Victory()
        {
            _battleActive = false;
            LastBattleWon = true;
            if (_music != null) _music.Stop();
            PlayAudio(MS_VICTORY, 0.85f);
            FullScreenFlash(new Color(1f, 0.85f, 0.30f, 0.55f), 0.7f);

            // Award rewards immediately (so they're saved even if user closes mid-anim)
            int levelsGained = 0;
            var d = Sparq.Core.SaveService.Data;
            if (d != null)
            {
                d.sparqCoins += _goldReward;
                // Real leveling (Top Heroes style): winning fights levels the hero,
                // which raises the whole party's stats (they scale off d.level), so
                // you grow strong enough to push into the next zone. Single curve.
                levelsGained = Progression.GrantXp(d, _xpReward);
                Sparq.Core.SaveService.Save();
            }

            // ── Loot burst — gold coins erupt from the final boss / center position ──
            // Adds a satisfying "you killed the big guy and look what fell out" moment.
            Vector3 burstPos = _bossFighter != null && _bossFighter.slotRT != null
                ? _bossFighter.slotRT.position
                : _root.GetComponent<RectTransform>().position;
            _runner.StartCoroutine(LootCoinBurst(burstPos, Mathf.Max(8, _goldReward / 8)));

            // ── Animated VICTORY banner from WinLoseAnimateAssetPack ──
            var winFrames = LoadFrameSequence(
                "Assets/WinLoseAnimateAssetPack/Png/Win/win-Win_{N}.png", 0, 2);
            if (winFrames != null && winFrames.Length > 0)
            {
                _statusBanner.text = "";
                SpawnFrameAnim(_root.transform, winFrames,
                    new Vector2(0.5f, 0.55f), new Vector2(900, 600), 0.045f);
            }
            else
            {
                _statusBanner.text = $"<color=#FFD466>VICTORY!</color>";
            }

            // Reward chip below the banner
            var rewardTxt = MakeText(_root.transform, "RewardTxt",
                $"<color=#FFD466>+{_xpReward} XP   +{_goldReward} GOLD</color>",
                new Vector2(0.5f, 0.20f), new Vector2(900, 80), 42, FontStyles.Bold, CREAM);
            rewardTxt.alignment = TextAlignmentOptions.Center;
            rewardTxt.outlineWidth = 0.30f; rewardTxt.outlineColor = Color.black;
            rewardTxt.richText = true;
            rewardTxt.raycastTarget = false;

            // LEVEL UP! flash when the hero gained one or more levels.
            if (levelsGained > 0 && d != null)
            {
                string lvlMsg = levelsGained == 1
                    ? $"LEVEL UP!  Lv {d.level}"
                    : $"LEVEL UP ×{levelsGained}!  Lv {d.level}";
                var lvlTxt = MakeText(_root.transform, "LevelUpTxt",
                    $"<color=#7CFC8A>{lvlMsg}</color>",
                    new Vector2(0.5f, 0.30f), new Vector2(900, 80), 50, FontStyles.Bold, CREAM);
                lvlTxt.alignment = TextAlignmentOptions.Center;
                lvlTxt.outlineWidth = 0.32f; lvlTxt.outlineColor = Color.black;
                lvlTxt.richText = true; lvlTxt.raycastTarget = false;
                PlayAudio(SFX_CONFIRM, 0.9f);
            }

            yield return new WaitForSeconds(3.6f);
            Hide();
        }

        // Coin loot burst — N gold-coin sprites erupt from a position with random
        // arc trajectories, settle, then fade. Plays a coin clink for each.
        private static IEnumerator LootCoinBurst(Vector3 worldPos, int count)
        {
            // Cap at 24 to avoid spam
            count = Mathf.Min(count, 24);
            // Spawn coins in a quick fan
            for (int i = 0; i < count; i++)
            {
                _runner.StartCoroutine(LootCoin(worldPos));
                if (i % 3 == 0) try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
                yield return new WaitForSeconds(0.04f);
            }
        }

        private static IEnumerator LootCoin(Vector3 worldPos)
        {
            var go = new GameObject("LootCoin", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.position = worldPos;
            rt.sizeDelta = new Vector2(56, 56);
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 0.82f, 0.30f);
            img.sprite = MakeFilledCircleSprite();
            img.raycastTarget = false;

            // Add a subtle inner highlight for "shiny" effect
            var highlight = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            highlight.transform.SetParent(go.transform, false);
            var hRT = highlight.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.25f, 0.45f); hRT.anchorMax = new Vector2(0.55f, 0.75f);
            hRT.offsetMin = Vector2.zero; hRT.offsetMax = Vector2.zero;
            highlight.GetComponent<Image>().color = new Color(1f, 0.95f, 0.65f, 0.85f);
            highlight.GetComponent<Image>().sprite = MakeFilledCircleSprite();
            highlight.GetComponent<Image>().raycastTarget = false;

            // Random arc trajectory
            Vector2 launch = new Vector2(Random.Range(-260f, 260f), Random.Range(280f, 460f));
            float gravity = -1100f;
            float life = 1.6f;
            float t = 0;
            Vector3 start = worldPos;
            float spinDir = Random.Range(0, 2) == 0 ? 1f : -1f;
            while (t < life && rt != null)
            {
                rt.position = start + new Vector3(launch.x * t, launch.y * t + 0.5f * gravity * t * t, 0);
                rt.localRotation = Quaternion.Euler(0, 0, t * 360f * spinDir);
                // Fade out the last 30% of life
                if (t > life * 0.7f)
                {
                    float fade = 1f - (t - life * 0.7f) / (life * 0.3f);
                    img.color = new Color(1f, 0.82f, 0.30f, fade);
                }
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static IEnumerator Defeat()
        {
            _battleActive = false;
            LastBattleWon = false;
            if (_music != null) _music.Stop();
            PlayAudio(MS_DEFEAT, 0.85f);
            FullScreenFlash(new Color(0.55f, 0.10f, 0.10f, 0.55f), 0.7f);

            // ── Animated DEFEAT banner from WinLoseAnimateAssetPack ──
            var loseFrames = LoadFrameSequence(
                "Assets/WinLoseAnimateAssetPack/Png/Lose/Lose-animation_{N}.png", 0, 2);
            if (loseFrames != null && loseFrames.Length > 0)
            {
                _statusBanner.text = "";
                SpawnFrameAnim(_root.transform, loseFrames,
                    new Vector2(0.5f, 0.55f), new Vector2(900, 600), 0.05f);    // ~20fps
            }
            else
            {
                _statusBanner.text = "<color=#FF6677>DEFEAT…</color>";
            }

            yield return new WaitForSeconds(2.8f);
            Hide();
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════
        private static Sprite[] LoadFrames(string base3DigitPath, int count)
        {
            // Runs in both Editor + Player builds. SpriteLoader handles the
            // Resources-vs-AssetDatabase fork. Previously editor-only — battle
            // chars rendered as white placeholder boxes in APK.
            var arr = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                string path = base3DigitPath + i.ToString("D3") + ".png";
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null) arr.Add(sp);
            }
            return arr.ToArray();
        }

        private static void PlayAudio(string assetPath, float volume)
        {
            if (_audio == null) return;
            #if UNITY_EDITOR
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null) _audio.PlayOneShot(clip, volume);
            #endif
        }

        private static IEnumerator DelayedAudio(string path, float delay, float vol)
        {
            yield return new WaitForSeconds(delay);
            PlayAudio(path, vol);
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go.GetComponent<Image>();
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            Vector2 anchor, Vector2 size, int fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (size != Vector2.zero) { rt.anchorMin = anchor; rt.anchorMax = anchor; rt.sizeDelta = size; rt.pivot = new Vector2(0.5f, 0.5f); }
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.fontStyle = style;
            tm.color = color;
            return tm;
        }

        // Procedural sprites (cached)
        private static Sprite _ellipseSp, _ringSp, _filledCircleSp, _rectSp;

        private static Sprite MakeFilledCircleSprite()
        {
            if (_filledCircleSp != null) return _filledCircleSp;
            int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = sz / 2f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - r, dy = y - r;
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
                }
            tex.Apply();
            _filledCircleSp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return _filledCircleSp;
        }

        private static Sprite MakeRectSprite()
        {
            if (_rectSp != null) return _rectSp;
            var tex = new Texture2D(4, 4, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++) tex.SetPixel(x, y, Color.white);
            tex.Apply();
            _rectSp = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return _rectSp;
        }

        private static Sprite MakeEllipseSprite()
        {
            if (_ellipseSp != null) return _ellipseSp;
            int sw = 96, sh = 24;
            var tex = new Texture2D(sw, sh, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float rx = sw / 2f, ry = sh / 2f;
            for (int y = 0; y < sh; y++)
                for (int x = 0; x < sw; x++)
                {
                    float dx = (x - rx) / rx, dy = (y - ry) / ry;
                    bool inside = dx*dx + dy*dy <= 1f;
                    tex.SetPixel(x, y, inside ? Color.white : new Color(0,0,0,0));
                }
            tex.Apply();
            _ellipseSp = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f));
            return _ellipseSp;
        }

        private static Sprite MakeRingSprite()
        {
            if (_ringSp != null) return _ringSp;
            int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float rOuter = sz / 2f;
            float rInner = rOuter * 0.78f;
            for (int y = 0; y < sz; y++)
                for (int x = 0; x < sz; x++)
                {
                    float dx = x - rOuter, dy = y - rOuter;
                    float d = Mathf.Sqrt(dx*dx + dy*dy);
                    bool inRing = d <= rOuter && d >= rInner;
                    tex.SetPixel(x, y, inRing ? Color.white : new Color(0,0,0,0));
                }
            tex.Apply();
            _ringSp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return _ringSp;
        }

        private class RunnerMB : MonoBehaviour { }
    }
}
