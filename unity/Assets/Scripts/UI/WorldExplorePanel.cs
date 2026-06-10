using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Phase 2 of the explorable RPG map.
    ///
    /// Reference: Top Heroes / Hero Wars-style world map. Player squad walks
    /// around a top-down land, finds chests + hidden things, walks into enemies
    /// to launch battles. Uses only top-quality assets:
    ///   - Layer Lab GUI Pro-SuperCasual joystick sprites
    ///   - Layer Lab 2D Icons-RewardChestPack for chests
    ///   - Layer Lab 2D Icons-CasualIconPack for top/bottom HUD icons
    ///   - FantasyMaps painted scenes for terrain
    ///   - Existing chibi packs for hero / pet / ally / enemies
    ///
    /// Show via: Sparq.UI.WorldExplorePanel.Show("forest");
    /// </summary>
    public static class WorldExplorePanel
    {
        // ── Map layout ──
        // Tall portrait world. MAP_H was bumped so a single run is a proper
        // journey (walk the winding path up to the boss) instead of ~10s.
        private const float MAP_W = 1620f;
        private const float MAP_H = 3800f;
        // Heights are the on-map render box. Sprites are alpha-cropped tight to
        // the figure (see HeroPortrait.LoadCroppedFrames) so the figure fills the
        // box and the feet reach the base — that's what grounds them. Trimmed a
        // little vs. the old padded sizes since the figure now fills the frame.
        private const float PLAYER_HEIGHT = 280f;   // was 185 — bumped per tester ("too tiny")
        private const float ALLY_HEIGHT  = 220f;    // was 145 — bumped proportionally
        private const float ENEMY_HEIGHT = 260f;    // was 175 — bumped proportionally
        private const float CHEST_SIZE = 140f;

        // ── Movement ──
        // Tightened to a deliberate explore pace. No input smoothing — when the
        // stick releases, the player stops immediately (no glide/drift).
        private const float PLAYER_MAX_SPEED = 150f;          // px/sec at full joystick deflection (was 200 — tester felt too fast)
        private const float ALLY_FOLLOW_SPEED = 260f;         // slightly faster so the pet catches up
        private const float ARRIVAL_THRESHOLD = 8f;
        private const float ENEMY_AGGRO_RADIUS = 130f;        // walk this close = battle starts
        private const float JOYSTICK_DEADZONE = 0.25f;        // larger deadzone = fewer accidental drifts
        private const float INPUT_RESPONSE_POW = 2.0f;        // higher power = harder to go fast (more deliberate)

        // Playable area is smaller than the map sprite — keeps the squad on the
        // green island and out of the painted water/sky borders.
        private const float PLAY_AREA_MARGIN_X = 280f;
        private const float PLAY_AREA_MARGIN_Y = 380f;

        // ── Reveal (fog-of-war for loot/enemies) ──
        // Objects start faded; alpha ramps up as the player approaches. No
        // discovery means no spoilers — the world feels exploratory.
        private const float CHEST_REVEAL_RADIUS = 320f;
        private const float ENEMY_REVEAL_RADIUS = 380f;
        private const float CHEST_HIDDEN_ALPHA = 0f;          // fully hidden until approached
        private const float ENEMY_HIDDEN_ALPHA = 0.10f;       // ghostly
        // Scattered loot stays fully hidden until the squad gets close — keeps
        // the map clean and makes finding it feel like discovery.
        private const float LOOT_REVEAL_RADIUS = 260f;
        private const float LOOT_HIDDEN_ALPHA  = 0f;          // completely invisible when far

        // ── Asset paths ──
        private const string JOYSTICK_BG_PATH      = "Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Demo/Demo_Play/Joystick_play_wheel_bg.png";
        private const string JOYSTICK_HANDLE_PATH  = "Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Demo/Demo_Play/Joystick_play_wheel_handle.png";
        private const string CHEST_GOLD_CLOSED  = "Assets/Layer Lab/2D Icons-RewardChestPack/Icons/Chest_Gold/box_gold_up.png";
        private const string CHEST_GOLD_OPEN    = "Assets/Layer Lab/2D Icons-RewardChestPack/Icons/Chest_Gold/gold_open.png";
        private const string CHEST_SILVER_CLOSED= "Assets/Layer Lab/2D Icons-RewardChestPack/Icons/Chest_Silver/box_silver_up.png";
        private const string CHEST_SILVER_OPEN  = "Assets/Layer Lab/2D Icons-RewardChestPack/Icons/Chest_Silver/silver_open.png";
        private const string CHEST_WOOD_CLOSED  = "Assets/Layer Lab/2D Icons-RewardChestPack/Icons/Chest_Wood/box_wood_up.png";
        private const string ICON_COIN  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Coin01_Gold.png";
        private const string ICON_HEART = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png";
        private const string ICON_BOLT  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Lightning01_Blue.png";
        // Scattered ground loot — gems / stars / gear caches litter the map.
        private const string ICON_GEM   = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Materials_Gem02_Purple.png";
        private const string ICON_STAR  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Star01_Gold.png";
        private const string ICON_GEAR  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Materials_CogWheel01.png";
        // Walk this close to a piece of ground loot and the squad scoops it up.
        private const float LOOT_PICKUP_RADIUS = 95f;

        // ── Squad ──
        private class Squadmate
        {
            public RectTransform rt;
            public Image img;
            public Sprite[] idleFrames;
            public int frameIdx;
            public float frameTimer;
            public Vector2 followOffset;     // relative to leader's position
            public string name;
        }

        // ── Map objects ──
        private class Chest
        {
            public RectTransform rt;
            public Image img;
            public Sprite closedSprite, openSprite;
            public bool opened;
            public int rewardGold;
            public string lore;
        }

        private class MapEnemy
        {
            public RectTransform rt;
            public Image img;
            public Image shadow;             // ground shadow — fades with the body
            public TMPro.TMP_Text bang;      // "!" warning — only when close enough to see
            public Sprite[] idleFrames;
            public int frameIdx;
            public float frameTimer;
            public string enemyKey;          // matches SquadBattle ENEMIES roster
            public string biome;
            public bool defeated;
            public bool isBoss;              // true = full 3-wave set piece; else a single quick skirmish
        }

        // Small ground pickups scattered across the map — coins, gems, XP stars,
        // gear caches. Walk over one and the squad scoops it up automatically
        // (works in both manual and AUTO mode). Pure "loot litter" so the world
        // feels rich and rewarding to roam.
        private class Collectible
        {
            public RectTransform rt;
            public Image img;
            public CanvasGroup cg;           // fades the whole pickup in/out (fog-of-war)
            public string kind;              // "coin" | "gem" | "star" | "gear"
            public int coins;
            public int xp;
            public bool collected;
        }

        // ── State ──
        private static GameObject _root;
        private static RectTransform _viewport;
        private static RectTransform _mapContent;

        private static Squadmate _player;
        private static List<Squadmate> _allies = new List<Squadmate>();
        private static List<Chest> _chests = new List<Chest>();
        private static List<MapEnemy> _enemies = new List<MapEnemy>();
        private static List<Collectible> _collectibles = new List<Collectible>();
        private static TMP_Text _coinLabel;   // top-HUD coin counter — bumped live on pickup
        private static TMP_Text _zoneLabel;   // top-HUD zone name
        private static TMP_Text _levelLabel;  // top-HUD "Lv N" — refreshed on level-up
        private static TMP_Text _xpLabel;     // top-HUD XP counter
        private static TMP_Text _vitalityLabel; // top-HUD wellness buff strip

        // ── World zones (sequential progression) ───────────────────────────────
        // Each zone is a full run: walk up the map clearing enemies to the boss.
        // Beating the boss advances to the next zone — a different backdrop/tint
        // with tougher enemies and richer loot. Past the last defined zone the
        // difficulty/loot keep scaling (endless). Persisted via worldZoneIndex.
        private struct WorldZone
        {
            public string id;
            public string name;             // shown in the HUD + banners
            public string bgSet;             // FantasyMaps set: "01" or "02"
            public Color  tint;              // applied to the painted backdrop for variety
            public string biomeKey;          // drives SquadBattle backdrop + ambient
            public float  difficulty;        // enemy HP/ATK multiplier
            public float  lootMult;          // coin/gem/chest value multiplier
            // EquipmentService item id of the legendary that drops the FIRST time
            // this zone's boss is defeated. Empty = no signature (endless zones
            // fall through to the random RollLoot path).
            public string signatureItemId;
        }

        public static int ZoneCount => ZONES.Length;
        private static readonly WorldZone[] ZONES =
        {
            // difficulty = enemy HP/ATK ×; lootMult = reward ×. Difficulty ramp
            // softened (top zone 5.2→3.9) so deep zones don't wall — enemies also
            // scale with player level on top of this, so the curve compounds.
            // signatureItemId pairs each zone with its guaranteed first-clear drop.
            new WorldZone { id="greenwood", name="Greenwood Vale", bgSet="01", tint=new Color(1.00f,1.00f,1.00f), biomeKey="forest",  difficulty=1.0f,  lootMult=1.0f,  signatureItemId="sig_greenwood"  },
            new WorldZone { id="mossfen",   name="Mossfen Marsh",  bgSet="01", tint=new Color(0.74f,0.88f,0.70f), biomeKey="forest",  difficulty=1.25f, lootMult=1.25f, signatureItemId="sig_mossfen"    },
            new WorldZone { id="shadowmoor",name="Shadowmoor",     bgSet="02", tint=new Color(0.72f,0.62f,0.92f), biomeKey="haunted", difficulty=1.55f, lootMult=1.55f, signatureItemId="sig_shadowmoor" },
            new WorldZone { id="emberpeak", name="Emberpeak",      bgSet="02", tint=new Color(1.00f,0.70f,0.48f), biomeKey="rocky",   difficulty=1.9f,  lootMult=1.9f,  signatureItemId="sig_emberpeak"  },
            new WorldZone { id="frostspire",name="Frostspire",     bgSet="01", tint=new Color(0.70f,0.85f,1.00f), biomeKey="moonlit", difficulty=2.3f,  lootMult=2.3f,  signatureItemId="sig_frostspire" },
            new WorldZone { id="duskwastes",name="Dusk Wastes",    bgSet="02", tint=new Color(0.86f,0.76f,0.60f), biomeKey="rocky",   difficulty=2.75f, lootMult=2.8f,  signatureItemId="sig_duskwastes" },
            new WorldZone { id="voidreach", name="Voidreach",      bgSet="02", tint=new Color(0.62f,0.55f,0.85f), biomeKey="haunted", difficulty=3.3f,  lootMult=3.4f,  signatureItemId="sig_voidreach"  },
            new WorldZone { id="dragonspire",name="Dragonspire",   bgSet="02", tint=new Color(1.00f,0.55f,0.42f), biomeKey="rocky",   difficulty=3.9f,  lootMult=4.0f,  signatureItemId="sig_dragonspire"},
        };

        private static int _zoneIndex;

        // Resolve the active zone, escalating past the defined list for endless play.
        private static WorldZone CurrentZone()
        {
            int clamped = Mathf.Clamp(_zoneIndex, 0, ZONES.Length - 1);
            var z = ZONES[clamped];
            if (_zoneIndex >= ZONES.Length)
            {
                int extra = _zoneIndex - (ZONES.Length - 1);
                z.name = $"{z.name} +{extra}";
                z.difficulty *= 1f + 0.28f * extra;   // gentler endless ramp (was 0.40)
                z.lootMult   *= 1f + 0.30f * extra;
            }
            return z;
        }

        private static RectTransform _joystickBg, _joystickHandle;
        private static float _joystickRadius = 100f;
        private static Vector2 _joystickInput;        // -1..1, -1..1 — driving force
        private static int _joystickPointerId = int.MinValue;
        private static MonoBehaviour _runner;
        private static bool _battleHandoff;            // true while waiting for SquadBattle to finish

        // ── Idle / AUTO mode ──
        // When on, the squad auto-pilots: steers to the nearest live enemy
        // (or unopened chest), triggering battles + loot pickups automatically.
        private static bool _autoMode;
        private static Image _autoBtnImg;
        private static TMP_Text _autoBtnLbl;
        private const float AUTO_CHEST_PICKUP_RADIUS = 110f;

        // Active biome key (derived from the current zone) — used for respawns.
        private static string _biome = "forest";

        // mapId is now just an optional hint — the active map is the player's
        // current zone (persisted), so re-entering always resumes their progress.
        public static void Show(string mapId = null)
        {
            if (_root != null) Hide();

            // Resume at the furthest zone reached.
            try { _zoneIndex = Mathf.Max(0, Sparq.Core.SaveService.Data?.worldZoneIndex ?? 0); }
            catch { _zoneIndex = 0; }
            _biome = CurrentZone().biomeKey;

            try { Sparq.UI.HomeBgm.Pause(); } catch {}
            try { Sparq.UI.MapBgm.Ensure(); } catch {}

            BuildCanvas();
            BuildMapContent(_biome);
            BuildVegetation(_biome);   // trees/shrubs/stones scattered for atmosphere
            BuildSquad();
            BuildChests(_biome);
            BuildEnemies(_biome);
            BuildMinionWaves(_biome);  // small weak enemies for routine combat encounters
            BuildJoystick();
            BuildTopHud();
            BuildBackButton();
            BuildAutoButton();

            if (_runner == null)
            {
                var go = new GameObject("WorldExplore.Runner");
                _runner = go.AddComponent<RunnerMB>();
            }
            // Scatter loot AFTER the runner exists so each pickup's bob coroutine starts.
            BuildCollectibles(_biome);
            _runner.StartCoroutine(MovementLoop());
            _runner.StartCoroutine(AllyFollowLoop());
            _runner.StartCoroutine(CameraFollowLoop());
            _runner.StartCoroutine(IdleAnimateLoop());
            _runner.StartCoroutine(EnemyProximityLoop());
            _runner.StartCoroutine(RevealLoop());          // fog-of-war for chests + enemies
            _runner.StartCoroutine(AutoPilotLoop());        // idle auto-battle steering
            _runner.StartCoroutine(CollectiblePickupLoop()); // scoop up scattered loot on contact
            _battleHandoff = false;
            _autoMode = false;

            // Offline AFK rewards — if the squad earned coins/XP while the
            // player was away from the idle map, greet them with a claim popup.
            try
            {
                if (Sparq.Systems.AfkRewardService.HasClaimable())
                    Sparq.UI.AfkRewardPopup.Show();
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[WorldExplore] AFK reward check failed: {ex.Message}"); }
        }

        public static void Hide()
        {
            // Reset the AFK timer so accrual measures "since you last left the
            // idle map" — time spent actively here doesn't double-count.
            try { Sparq.Systems.AfkRewardService.Touch(); } catch {}
            try { Sparq.UI.AfkRewardPopup.Hide(); } catch {}
            if (_root != null) { Object.Destroy(_root); _root = null; }
            if (_runner != null) { Object.Destroy(_runner.gameObject); _runner = null; }
            _player = null;
            _allies.Clear();
            _chests.Clear();
            _enemies.Clear();
            _collectibles.Clear();
            _coinLabel = null;
            _zoneLabel = null;
            _levelLabel = null; _xpLabel = null;
            _vitalityLabel = null;
            _bgSky = null; _bgGround = null; _bgDeco = null;
            _joystickBg = null; _joystickHandle = null;
            try { Sparq.UI.MapBgm.Stop(); } catch {}
            try { Sparq.UI.HomeBgm.Resume(); } catch {}
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: CANVAS + MAP
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildCanvas()
        {
            _root = new GameObject("WorldExplorePanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            // Render ABOVE every existing canvas (incl. the lobby HUD at sort
            // ~14010). A hardcoded 14000 left the map behind the lobby —
            // invisible. Walk all canvases and sit 20 above the highest.
            int maxSort = 14000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            c.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Solid back fill
            var bg = MakeImage(_root.transform, "BG", new Color(0.04f, 0.06f, 0.12f, 1f));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            var vGO = new GameObject("Viewport", typeof(RectTransform));
            vGO.transform.SetParent(_root.transform, false);
            _viewport = vGO.GetComponent<RectTransform>();
            _viewport.anchorMin = Vector2.zero; _viewport.anchorMax = Vector2.one;
            _viewport.offsetMin = Vector2.zero; _viewport.offsetMax = Vector2.zero;
        }

        private static Image _bgSky, _bgGround, _bgDeco;

        private static void BuildMapContent(string mapId)
        {
            var mc = new GameObject("MapContent", typeof(RectTransform));
            mc.transform.SetParent(_viewport, false);
            _mapContent = mc.GetComponent<RectTransform>();
            _mapContent.anchorMin = new Vector2(0.5f, 0.5f);
            _mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            _mapContent.pivot = new Vector2(0.5f, 0.5f);
            _mapContent.sizeDelta = new Vector2(MAP_W, MAP_H);

            _bgSky    = AddBgLayer("Sky", 0);
            _bgGround = AddBgLayer("Ground", 1);
            _bgDeco   = AddBgLayer("Deco", 2);
            RefreshZoneBackdrop();
        }

        private static Image AddBgLayer(string name, int sibling)
        {
            var go = new GameObject($"BG_{name}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            go.transform.SetSiblingIndex(sibling);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.preserveAspect = false;
            img.raycastTarget = false;
            return img;
        }

        // Swap the 3 painted layers to the current zone's set + apply its tint so
        // each zone reads as a distinct place.
        private static void RefreshZoneBackdrop()
        {
            // Runs in both Editor + Player builds — SpriteLoader handles the
            // Resources-vs-AssetDatabase fork. Earlier this whole method was
            // editor-only (#if UNITY_EDITOR) so APK world maps rendered blank
            // with just placeholder ellipses for stage nodes.
            var z = CurrentZone();
            void Apply(Image img, string layerFile)
            {
                if (img == null) return;
                string path = $"Assets/FantasyMaps/_PNG/{z.bgSet}/layers/{layerFile}";
#if UNITY_EDITOR
                EnsureSpriteImporter(path);   // editor-only — adjusts import settings on first load
#endif
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null) img.sprite = sp;
                img.color = z.tint;
            }
            Apply(_bgSky,    "l1-sky.png");
            Apply(_bgGround, "l2-ground.png");
            Apply(_bgDeco,   "l3-decoartions.png");
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: VEGETATION (procedural scatter — trees, shrubs, stones)
        // ═══════════════════════════════════════════════════════════════════
        // Decoration only — non-interactive, doesn't block movement. Adds visual
        // density so the map doesn't feel like a flat field. Deterministic seed
        // so the same map always looks the same between sessions.
        private static void BuildVegetation(string mapId)
        {
            #if UNITY_EDITOR
            // Different seeds per biome so each map feels distinct
            int seed = mapId == null ? 0 : mapId.GetHashCode();
            var rng = new System.Random(seed);

            const string FW = "Assets/Fantasy World 2D/Sprites/PNG/";
            string[] treeOptions = { "1","2","3","4","5","6","7","8","9","10","11","12","13","14" };
            string[] shrubOptions = { "1","2","3","4","5","6","7","8","9","10","11","12","13" };
            string[] stoneOptions = { "1","2","3","4","5","6","7","8","9","10","11","12" };

            // Avoid the center vertical corridor (player spawn + main walking lane)
            // and the very top (near boss area).
            float halfW = MAP_W * 0.5f - 80f;
            float halfH = MAP_H * 0.5f - 80f;

            // Trees — heaviest cover, tall sprites along the edges
            for (int i = 0; i < 32; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * halfW;
                float y = (float)(rng.NextDouble() * 2.0 - 1.0) * halfH;
                // Bias trees outward — skip the central corridor where the player walks
                if (Mathf.Abs(x) < 180f && Mathf.Abs(y) < 200f) continue;
                int v = rng.Next(treeOptions.Length);
                SpawnVegSprite($"Tree_{i}",
                    $"{FW}trees/cartoon_world_tree_{treeOptions[v]}.png",
                    new Vector2(x, y), Random.Range(180f, 240f));
            }

            // Shrubs — fill space, smaller
            for (int i = 0; i < 28; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * halfW;
                float y = (float)(rng.NextDouble() * 2.0 - 1.0) * halfH;
                if (Mathf.Abs(x) < 120f && Mathf.Abs(y) < 160f) continue;
                int v = rng.Next(shrubOptions.Length);
                SpawnVegSprite($"Shrub_{i}",
                    $"{FW}shrubs/cartoon_world_shrub_{shrubOptions[v]}.png",
                    new Vector2(x, y), Random.Range(110f, 150f));
            }

            // Stones — small ground details, scatter freely
            for (int i = 0; i < 18; i++)
            {
                float x = (float)(rng.NextDouble() * 2.0 - 1.0) * halfW;
                float y = (float)(rng.NextDouble() * 2.0 - 1.0) * halfH;
                int v = rng.Next(stoneOptions.Length);
                SpawnVegSprite($"Stone_{i}",
                    $"{FW}stones/cartoon_world_stone_{stoneOptions[v]}.png",
                    new Vector2(x, y), Random.Range(70f, 110f));
            }
            #endif
        }

        private static void SpawnVegSprite(string name, string path, Vector2 pos, float size)
        {
#if UNITY_EDITOR
            EnsureSpriteImporter(path);
#endif
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp == null) return;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            // Sit just above the BG layers but below squad/enemies/chests so the
            // squad always renders in front of grass — set sibling early.
            go.transform.SetSiblingIndex(3);   // BG_Sky=0, BG_Ground=1, BG_Deco=2, veg starts at 3
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);     // pivot at base so things sit on the ground
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = false;
            // Slight tint variation for organic feel — random green/brown shift
            float h = Random.Range(0.92f, 1.05f);
            img.color = new Color(h, 1f, h * 0.96f, 1f);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ZONE-THEMED ENEMY ROSTERS
        // ═══════════════════════════════════════════════════════════════════
        // Each zone has its own elites (cycled across the 6 elite slots),
        // minion pool (cycled across the 13 minion slots), and a uniquely-named
        // boss. Names + sprites change per zone so deep zones feel different.

        private const string ID_BANDIT  = "Assets/BanditChibi/Assassin/PNG/PNG Sequences/Back - Idle/Back - Idle_";
        private const string ID_MERC    = "Assets/MercenariesChibi/Mercenaries_1/PNG/PNG Sequences/Idle/0_Mercenaries_Idle_";
        private const string ID_MEDUSA  = "Assets/MedusaChibi/Medusa_1/PNG/PNG Sequences/Idle/0_Medusa_Idle_";
        private const string ID_SAMURAI = "Assets/SamuraiChibi/Samurai_1/PNG/PNG Sequences/Idle/0_Samurai_Idle_";
        private const string ID_PERSIAN = "Assets/PersianWarriorChibi/Persian_and_Arab_Warriors_1/PNG/PNG Sequences/Idle/0_Persian_and_Arab_Warriors_Idle_";
        private const string ID_MIMIC   = "Assets/MimicChibi/Mimic_1/PNG/PNG Sequences/Idle/0_Mimic_Idle_";

        private struct EnemyKind
        {
            public string name;
            public string idlePath;
            public EnemyKind(string n, string p) { name = n; idlePath = p; }
        }

        private struct ZoneEnemies
        {
            public EnemyKind[] elites;   // cycled across the 6 elite slots
            public EnemyKind[] minions;  // cycled across the 13 minion slots
            public EnemyKind   boss;     // single themed boss per zone
        }

        // Indexed to match ZONES order; ZONE_ENEMIES[i] drives zone i.
        private static readonly ZoneEnemies[] ZONE_ENEMIES =
        {
            // 0 Greenwood Vale — forest bandits, wolves, brute boss
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Forest Goblin", ID_BANDIT), new EnemyKind("Shadow Wolf", ID_MERC), new EnemyKind("Forest Goblin", ID_BANDIT) },
                minions = new[] { new EnemyKind("Forest Goblin", ID_BANDIT), new EnemyKind("Shadow Wolf", ID_MERC) },
                boss = new EnemyKind("Stone Brute", ID_PERSIAN),
            },
            // 1 Mossfen Marsh — soggier bandits + a witch boss
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Bog Bandit", ID_BANDIT), new EnemyKind("Marsh Witch", ID_MEDUSA), new EnemyKind("Bog Mercenary", ID_MERC) },
                minions = new[] { new EnemyKind("Bog Bandit", ID_BANDIT), new EnemyKind("Bog Mercenary", ID_MERC) },
                boss = new EnemyKind("Mind Phantom", ID_MEDUSA),
            },
            // 2 Shadowmoor — ghostly witches + a dark alpha
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Shade Witch", ID_MEDUSA), new EnemyKind("Shade Stalker", ID_MERC), new EnemyKind("Shade Witch", ID_MEDUSA) },
                minions = new[] { new EnemyKind("Shade Witch", ID_MEDUSA), new EnemyKind("Shade Stalker", ID_MERC) },
                boss = new EnemyKind("Shadow Alpha", ID_MERC),
            },
            // 3 Emberpeak — fire-themed warriors + Mimic boss
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Ash Mercenary", ID_MERC), new EnemyKind("Ember Warrior", ID_PERSIAN), new EnemyKind("Ash Ronin", ID_SAMURAI) },
                minions = new[] { new EnemyKind("Ash Mercenary", ID_MERC), new EnemyKind("Ember Warrior", ID_PERSIAN) },
                boss = new EnemyKind("Mimic Lord", ID_MIMIC),
            },
            // 4 Frostspire — ronin + glacier warriors + frost sovereign
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Frost Ronin", ID_SAMURAI), new EnemyKind("Glacier Warrior", ID_PERSIAN), new EnemyKind("Frost Ronin", ID_SAMURAI) },
                minions = new[] { new EnemyKind("Frost Ronin", ID_SAMURAI), new EnemyKind("Glacier Warrior", ID_PERSIAN) },
                boss = new EnemyKind("Frost Sovereign", ID_SAMURAI),
            },
            // 5 Dusk Wastes — sand raiders + dune bandits + sand king
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Sand Raider", ID_PERSIAN), new EnemyKind("Dune Bandit", ID_BANDIT), new EnemyKind("Sand Raider", ID_PERSIAN) },
                minions = new[] { new EnemyKind("Sand Raider", ID_PERSIAN), new EnemyKind("Dune Bandit", ID_BANDIT) },
                boss = new EnemyKind("Sand King", ID_PERSIAN),
            },
            // 6 Voidreach — void witches + ronin + empress boss
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Void Witch", ID_MEDUSA), new EnemyKind("Void Ronin", ID_SAMURAI), new EnemyKind("Void Witch", ID_MEDUSA) },
                minions = new[] { new EnemyKind("Void Witch", ID_MEDUSA), new EnemyKind("Void Ronin", ID_SAMURAI) },
                boss = new EnemyKind("Void Empress", ID_MEDUSA),
            },
            // 7 Dragonspire — endgame cultists + a Dragonknight
            new ZoneEnemies {
                elites  = new[] { new EnemyKind("Dragon Cultist", ID_PERSIAN), new EnemyKind("Dragon Ronin", ID_SAMURAI), new EnemyKind("Hoardling", ID_MIMIC) },
                minions = new[] { new EnemyKind("Dragon Cultist", ID_PERSIAN), new EnemyKind("Dragon Ronin", ID_SAMURAI) },
                boss = new EnemyKind("Dragonknight", ID_PERSIAN),
            },
        };

        private static ZoneEnemies CurrentZoneEnemies()
        {
            int i = Mathf.Clamp(_zoneIndex, 0, ZONE_ENEMIES.Length - 1);
            return ZONE_ENEMIES[i];
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: MINION WAVES (small/weak enemies for routine combat)
        // ═══════════════════════════════════════════════════════════════════
        // Named enemies in BuildEnemies are elite encounters; minions are the
        // bulk of the population — scattered, weak, fast to clear. Player gets
        // XP/gold drip-feed without every fight being a boss-tier set piece.
        private static void BuildMinionWaves(string mapId)
        {
            // Position + size only — the enemy KIND comes from the active zone
            // table (so Greenwood gets goblins, Frostspire gets frost ronin, etc.).
            var spawns = new (Vector2 pos, float scale)[]
            {
                // Spread bottom→top so the player meets minions progressively as
                // they push up the long path toward the boss.
                (new Vector2(-280, -1180), 0.55f),
                (new Vector2( 320, -980),  0.55f),
                (new Vector2(  40, -760),  0.50f),
                (new Vector2(-360, -560),  0.60f),
                (new Vector2( 380, -360),  0.62f),
                (new Vector2(-120, -200),  0.55f),
                (new Vector2( 300,  40),   0.62f),
                (new Vector2(-340,  240),  0.58f),
                (new Vector2(  80,  460),  0.64f),
                (new Vector2( 360,  640),  0.58f),
                (new Vector2(-300,  840),  0.65f),
                (new Vector2( 120, 1040),  0.60f),
                (new Vector2(-160, 1240),  0.66f),
            };

            var z = CurrentZoneEnemies();
            if (z.minions == null || z.minions.Length == 0) return;
            for (int i = 0; i < spawns.Length; i++)
            {
                var kind = z.minions[i % z.minions.Length];
                var s = spawns[i];
                var e = MakeMapEnemy(s.pos, kind.name, kind.idlePath, mapId);
                // Shrink to minion size — scales the image AND the shadow proportionally
                if (e != null && e.rt != null)
                {
                    e.rt.sizeDelta = new Vector2(ENEMY_HEIGHT * s.scale, ENEMY_HEIGHT * s.scale);
                    if (e.shadow != null)
                    {
                        var srt = e.shadow.rectTransform;
                        srt.sizeDelta = new Vector2(140 * s.scale, 28 * s.scale);
                    }
                }
                _enemies.Add(e);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: SQUAD (hero + pet + ally)
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildSquad()
        {
            // 1) Player hero — class-driven sprite, alpha-cropped tight to the
            //    figure (clipThinSides drops the Knight's long spear from the
            //    width) so it stands grounded instead of floating.
            _player = MakeSquadmate("Hero", null, PLAYER_HEIGHT,
                new Vector2(0, -MAP_H * 0.40f), Vector2.zero);
            // Hero sprite loading — runs in both Editor + Player builds.
            // Was previously editor-only — that's why the hero rendered as
            // a gold-circle placeholder in the APK even though knight art
            // is in Resources.
            var hero = Sparq.Systems.HeroClassResolver.Resolve();
            if (hero != null && !string.IsNullOrEmpty(hero.idleBase))
            {
                // Try alpha-cropped frames first (editor-only — needs CPU-readable
                // textures). Falls back to plain SpriteLoader sequence in APK.
                _player.idleFrames = Sparq.UI.HeroPortrait.LoadCroppedFrames(
                    hero.idleBase, hero.idleCount, clipThinSides: true);
                if (_player.idleFrames == null || _player.idleFrames.Length == 0)
                    _player.idleFrames = LoadFrameSequence(hero.idleBase, hero.idleCount);
            }
            ApplyFirstFrame(_player);

            // 2) Pet ally — from PetService (single sprite, cropped tight too).
            //    Render height scales with evolution stage: Hatchling 0.85× →
            //    Elder 1.25×, so the pet visibly grows as it levels up.
            float petScale = 1f;
            try
            {
                int petLvl = Sparq.Core.SaveService.Data?.petLevel ?? 1;
                petScale = Sparq.Systems.PetService.EvolutionScale(Sparq.Systems.PetService.EvolutionStage(petLvl));
            }
            catch {}
            // Pet follow offset — kept tight so pet hugs the hero's hip (not a
            // half-screen-away orbiting moon). Was (-95, -55), then (-60, -35).
            // Tester still wanted them closer — now (-40, -20). Pet practically
            // overlaps the hero's leg, reads as one combat unit.
            var petOffset = new Vector2(-40, -20);
            var petMate = MakeSquadmate("Pet", null, ALLY_HEIGHT * petScale,
                _player.rt.anchoredPosition + petOffset,
                petOffset);
            try
            {
                var pet = Sparq.Systems.PetService.Active();
                if (pet != null)
                {
                    var sp = Sparq.Systems.PetService.FindSpecies(pet.speciesId);
                    if (sp != null && !string.IsNullOrEmpty(sp.spritePath))
                    {
                        var single = Sparq.UI.HeroPortrait.LoadCropped(sp.spritePath)
                                     ?? Sparq.Core.SpriteLoader.Load(sp.spritePath);
                        if (single != null) petMate.idleFrames = new[] { single };
                    }
                    petMate.name = pet.nickname ?? sp?.name ?? "Pet";
                }
            }
            catch {}
            ApplyFirstFrame(petMate);
            _allies.Add(petMate);

            // Squad is Hero + pet. The pet walks at the hero's back-left as a
            // tight companion cluster (not stacked directly under him), and faces
            // the same way the hero does so the duo reads as one moving unit.
            if (_allies.Count > 0 && _allies[0] != null)
            {
                _allies[0].followOffset = new Vector2(-40, -20);   // tighter still — matches the spawn offset
                _allies[0].rt.anchoredPosition = _player.rt.anchoredPosition + _allies[0].followOffset;
            }
        }

        // Build one squadmate slot (player or ally) and return its handle
        private static Squadmate MakeSquadmate(string name, Sprite[] idleFrames, float height,
                                               Vector2 spawnPos, Vector2 followOffset)
        {
            var sm = new Squadmate { name = name, idleFrames = idleFrames, followOffset = followOffset };
            var go = new GameObject($"Squad_{name}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            sm.rt = go.GetComponent<RectTransform>();
            sm.rt.anchorMin = sm.rt.anchorMax = new Vector2(0.5f, 0.5f);
            sm.rt.pivot = new Vector2(0.5f, 0.5f);
            sm.rt.sizeDelta = new Vector2(height, height);
            sm.rt.anchoredPosition = spawnPos;
            sm.img = go.GetComponent<Image>();
            sm.img.preserveAspect = true;
            sm.img.raycastTarget = false;

            // Soft contact shadow at the base. The sprite is alpha-cropped so the
            // figure fills the box and the feet land on the box bottom — so a tight
            // ellipse just above the base now sits right under the feet (no float).
            var sh = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(go.transform, false);
            sh.transform.SetAsFirstSibling();
            var shRT = sh.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0.5f, 0); shRT.anchorMax = new Vector2(0.5f, 0);
            shRT.pivot = new Vector2(0.5f, 0.5f);
            shRT.anchoredPosition = new Vector2(0, height * 0.05f);
            shRT.sizeDelta = new Vector2(height * 0.56f, height * 0.15f);
            var shImg = sh.GetComponent<Image>();
            shImg.color = new Color(0, 0, 0, 0.40f);
            shImg.sprite = MakeEllipseSprite();
            shImg.raycastTarget = false;
            return sm;
        }

        private static void ApplyFirstFrame(Squadmate sm)
        {
            if (sm.img == null) return;
            if (sm.idleFrames != null && sm.idleFrames.Length > 0)
            {
                sm.img.sprite = sm.idleFrames[0];
                sm.img.color = Color.white;
            }
            else
            {
                // No character art loaded — fall back to a soft circle placeholder
                // tinted by role so testers can still see WHERE the hero is on
                // the map. Player = warm gold, ally = cool blue, enemy = red.
                // Better than an invisible character or a white box.
                var circleSp = Sparq.Core.SpriteLoader.Load(
                    "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png");
                if (circleSp != null) sm.img.sprite = circleSp;
                bool isPlayer = (sm == _player);
                sm.img.color = isPlayer
                    ? new Color(1f, 0.78f, 0.22f, 1f)        // gold for hero
                    : new Color(0.45f, 0.78f, 1f, 1f);       // light blue for allies
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: CHESTS
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildChests(string mapId)
        {
            // Hand-placed chest spawns spread up the tall map. Tier mix: mostly
            // wood/silver with rarer gold higher up. Positions are map-content
            // local coords (origin = map center; +y = deeper toward the boss).
            var spawns = new (Vector2 pos, string tier, int gold, string lore)[]
            {
                (new Vector2(-300, -700), "wood", 25,
                    "A traveler's lost satchel. Coins clink inside."),
                (new Vector2( 300, -360), "silver", 60,
                    "A merchant's strongbox, half-buried in the moss."),
                (new Vector2(-220, -40),  "wood", 30,
                    "Old wooden chest, worn by rain."),
                (new Vector2( 360,  260), "silver", 75,
                    "A scout's stash, marked with a tiny rune."),
                (new Vector2(-380,  560), "gold", 120,
                    "An ornate chest with gold filigree — clearly someone's hoard."),
                (new Vector2( 260,  860), "wood", 35,
                    "A rotted crate snagged on the path."),
                (new Vector2(-300, 1140), "silver", 90,
                    "A warded coffer, humming faintly."),
                (new Vector2( 380, 1360), "gold", 160,
                    "A dragon-marked vault — the deeper you go, the richer the spoils."),
            };

            float lm = CurrentZone().lootMult;
            foreach (var s in spawns)
            {
                var chest = MakeChest(s.pos, s.tier);
                chest.rewardGold = Mathf.RoundToInt(s.gold * lm);
                chest.lore = s.lore;
                _chests.Add(chest);
            }
        }

        private static Chest MakeChest(Vector2 pos, string tier)
        {
            string closed, open;
            switch (tier)
            {
                case "gold":   closed = CHEST_GOLD_CLOSED;   open = CHEST_GOLD_OPEN;   break;
                case "silver": closed = CHEST_SILVER_CLOSED; open = CHEST_SILVER_OPEN; break;
                default:       closed = CHEST_WOOD_CLOSED;   open = CHEST_WOOD_CLOSED; break;
            }

            var go = new GameObject($"Chest_{tier}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_mapContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(CHEST_SIZE, CHEST_SIZE);
            rt.anchoredPosition = pos;

            var c = new Chest { rt = rt };
            c.img = go.GetComponent<Image>();
            c.img.preserveAspect = true;
            c.img.raycastTarget = true;
#if UNITY_EDITOR
            EnsureSpriteImporter(closed); EnsureSpriteImporter(open);
#endif
            c.closedSprite = Sparq.Core.SpriteLoader.Load(closed);
            c.openSprite   = Sparq.Core.SpriteLoader.Load(open);
            if (c.closedSprite != null) c.img.sprite = c.closedSprite;
            // Start hidden — RevealLoop ramps alpha as the player approaches.
            // Loot stays a mystery until you're close enough to spot it.
            c.img.color = new Color(1f, 1f, 1f, CHEST_HIDDEN_ALPHA);

            // Gentle bob so chests catch the eye (only obvious once revealed)
            _runner?.StartCoroutine(ChestBob(rt, Random.Range(0f, 6.28f)));

            // Tap → open
            var captured = c;
            go.GetComponent<Button>().onClick.AddListener(() => OnChestTapped(captured));
            return c;
        }

        private static IEnumerator ChestBob(RectTransform rt, float phase)
        {
            if (Sparq.Systems.Accessibility.ReducedMotion) yield break;   // calm mode: static chests
            Vector2 home = rt.anchoredPosition;
            while (rt != null && _root != null)
            {
                float bob = Mathf.Sin(Time.time * 1.5f + phase) * 4f;
                rt.anchoredPosition = home + new Vector2(0, bob);
                yield return null;
            }
        }

        private static void OnChestTapped(Chest c)
        {
            if (c == null || c.opened) return;
            c.opened = true;
            // Swap to open sprite + small pulse
            if (c.openSprite != null) c.img.sprite = c.openSprite;
            _runner?.StartCoroutine(ChestPopAnim(c.rt));

            // Award coins
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null) { d.sparqCoins += c.rewardGold; Sparq.Core.SaveService.Save(); }
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
            }
            catch {}

            // Pop-up: lore + gold reward
            ShowChestPopup(c.rewardGold, c.lore);
        }

        private static IEnumerator ChestPopAnim(RectTransform rt)
        {
            float t = 0; float dur = 0.30f;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                float scale = 1f + Mathf.Sin(p * Mathf.PI) * 0.30f;   // single bounce
                rt.localScale = new Vector3(scale, scale, 1f);
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static void ShowChestPopup(int gold, string lore)
        {
            var popup = new GameObject("ChestPopup", typeof(RectTransform), typeof(Image));
            popup.transform.SetParent(_root.transform, false);
            var rt = popup.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(720, 220);
            popup.GetComponent<Image>().color = new Color(0.10f, 0.07f, 0.18f, 0.95f);

            // Gold gain headline
            var headline = MakeText(popup.transform, "Headline",
                $"<color=#FFD466>+{gold} GOLD</color>",
                new Vector2(0.5f, 0.72f), new Vector2(700, 60), 44, FontStyles.Bold, Color.white);
            headline.alignment = TextAlignmentOptions.Center;
            headline.outlineWidth = 0.30f; headline.outlineColor = Color.black;
            headline.richText = true;

            // Lore subtitle
            var loreTxt = MakeText(popup.transform, "Lore", $"<i>{lore}</i>",
                new Vector2(0.5f, 0.30f), new Vector2(680, 80), 22, FontStyles.Italic,
                new Color(1f, 1f, 0.85f, 0.92f));
            loreTxt.alignment = TextAlignmentOptions.Center;
            loreTxt.outlineWidth = 0.18f; loreTxt.outlineColor = Color.black;
            loreTxt.richText = true;

            _runner?.StartCoroutine(PopupAutoClose(popup, 2.5f));
        }

        private static IEnumerator PopupAutoClose(GameObject popup, float lifetime)
        {
            yield return new WaitForSeconds(lifetime);
            if (popup == null) yield break;
            // Fade out
            float t = 0; float fade = 0.25f;
            var img = popup.GetComponent<Image>();
            Color baseC = img != null ? img.color : Color.white;
            var texts = popup.GetComponentsInChildren<TMP_Text>(true);
            while (t < fade && popup != null)
            {
                float p = t / fade;
                if (img != null) img.color = new Color(baseC.r, baseC.g, baseC.b, baseC.a * (1f - p));
                foreach (var tm in texts) if (tm != null)
                    tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, 1f - p);
                t += Time.deltaTime; yield return null;
            }
            if (popup != null) Object.Destroy(popup);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: SCATTERED LOOT (coins / gems / xp stars / gear caches)
        // ═══════════════════════════════════════════════════════════════════
        // Litter the whole play area with little pickups so roaming feels
        // rewarding. The squad scoops them up by walking over them — no tapping,
        // works in AUTO mode too.
        private static void BuildCollectibles(string mapId)
        {
            float halfW = MAP_W * 0.5f - PLAY_AREA_MARGIN_X - 40f;
            float halfH = MAP_H * 0.5f - PLAY_AREA_MARGIN_Y - 40f;
            Vector2 spawnPt = new Vector2(0, -MAP_H * 0.40f);   // player start — keep a clear ring around it
            const int TARGET = 46;   // scaled up for the taller map
            int placed = 0, guard = 0;
            while (placed < TARGET && guard++ < 600)
            {
                Vector2 p = new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
                if (Vector2.Distance(p, spawnPt) < 170f) continue;   // don't bury the player at spawn
                bool tooClose = false;
                foreach (var ex in _collectibles)
                    if (ex.rt != null && Vector2.Distance(ex.rt.anchoredPosition, p) < 150f) { tooClose = true; break; }
                if (tooClose) continue;
                _collectibles.Add(MakeCollectible(p, RollLootKind()));
                placed++;
            }
        }

        private static string RollLootKind()
        {
            float r = Random.value;
            if (r < 0.55f) return "coin";   // most common
            if (r < 0.77f) return "gem";    // shiny, worth more coins
            if (r < 0.92f) return "star";   // XP
            return "gear";                  // gear cache — coins + a little XP
        }

        private static Collectible MakeCollectible(Vector2 pos, string kind)
        {
            var col = new Collectible { kind = kind };
            string icon; float size; Color glow;
            switch (kind)
            {
                case "gem":  icon = ICON_GEM;  size = 80f; glow = new Color(0.72f, 0.45f, 1f, 0.50f);  col.coins = Random.Range(30, 56); break;
                case "star": icon = ICON_STAR; size = 76f; glow = new Color(1f, 0.85f, 0.40f, 0.50f);  col.xp    = Random.Range(12, 26); break;
                case "gear": icon = ICON_GEAR; size = 82f; glow = new Color(0.55f, 0.80f, 1f, 0.50f);  col.coins = Random.Range(20, 41); col.xp = 10; break;
                default:     icon = ICON_COIN; size = 66f; glow = new Color(1f, 0.85f, 0.35f, 0.45f);  col.coins = Random.Range(8, 19);  break;
            }
            // Deeper zones drop richer loot.
            float lm = CurrentZone().lootMult;
            col.coins = Mathf.RoundToInt(col.coins * lm);
            col.xp    = Mathf.RoundToInt(col.xp * lm);

            var go = new GameObject($"Loot_{kind}", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(_mapContent, false);
            col.rt = go.GetComponent<RectTransform>();
            col.rt.anchorMin = col.rt.anchorMax = new Vector2(0.5f, 0.5f);
            col.rt.pivot = new Vector2(0.5f, 0.5f);
            col.rt.sizeDelta = new Vector2(size, size);
            col.rt.anchoredPosition = pos;
            // Start fully hidden — RevealLoop fades it in as the squad approaches.
            col.cg = go.GetComponent<CanvasGroup>();
            col.cg.alpha = LOOT_HIDDEN_ALPHA;

            // Soft glow halo behind the icon so loot pops against the busy map.
            var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGO.transform.SetParent(go.transform, false);
            var gRT = glowGO.GetComponent<RectTransform>();
            gRT.anchorMin = Vector2.zero; gRT.anchorMax = Vector2.one;
            gRT.offsetMin = new Vector2(-size * 0.32f, -size * 0.32f);
            gRT.offsetMax = new Vector2(size * 0.32f, size * 0.32f);
            var gImg = glowGO.GetComponent<Image>();
            gImg.sprite = MakeSoftCircleSprite(); gImg.color = glow; gImg.raycastTarget = false;

            // Icon
            var icGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icGO.transform.SetParent(go.transform, false);
            var icRT = icGO.GetComponent<RectTransform>();
            icRT.anchorMin = Vector2.zero; icRT.anchorMax = Vector2.one;
            icRT.offsetMin = Vector2.zero; icRT.offsetMax = Vector2.zero;
            col.img = icGO.GetComponent<Image>();
            col.img.preserveAspect = true; col.img.raycastTarget = false;
#if UNITY_EDITOR
            EnsureSpriteImporter(icon);
#endif
            var sp = Sparq.Core.SpriteLoader.Load(icon);
            if (sp != null) col.img.sprite = sp;

            _runner?.StartCoroutine(LootBob(col));
            return col;
        }

        // Gentle floaty bob — stops the instant the loot is collected so the
        // pickup animation has sole control of the transform.
        private static IEnumerator LootBob(Collectible c)
        {
            if (c == null || c.rt == null) yield break;
            if (Sparq.Systems.Accessibility.ReducedMotion) yield break;   // calm mode: static loot
            Vector2 home = c.rt.anchoredPosition;
            float phase = Random.Range(0f, 6.28f);
            while (c.rt != null && _root != null && !c.collected)
            {
                float bob = Mathf.Sin(Time.time * 2.0f + phase) * 5f;
                c.rt.anchoredPosition = home + new Vector2(0, bob);
                yield return null;
            }
        }

        // Each frame, scoop up any loot the player is standing on. Runs in both
        // manual and AUTO mode.
        private static IEnumerator CollectiblePickupLoop()
        {
            while (_root != null)
            {
                if (_battleHandoff || _player == null || _player.rt == null) { yield return null; continue; }
                Vector2 pp = _player.rt.anchoredPosition;
                foreach (var c in _collectibles)
                {
                    if (c == null || c.collected || c.rt == null) continue;
                    if (Vector2.Distance(pp, c.rt.anchoredPosition) <= LOOT_PICKUP_RADIUS)
                        CollectLoot(c);
                }
                yield return null;
            }
        }

        private static void CollectLoot(Collectible c)
        {
            if (c == null || c.collected) return;
            c.collected = true;

            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null)
                {
                    if (c.coins > 0) d.sparqCoins += c.coins;
                    if (c.xp > 0) GrantXp(d, c.xp);
                    Sparq.Core.SaveService.Save();
                    RefreshHudStats();   // coins + (loot XP can level you) level/XP
                }
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
            }
            catch {}

            // Floating "+N" + a quick collect pop.
            string txt = c.coins > 0 ? $"+{c.coins}" : $"+{c.xp} XP";
            Color col = c.kind == "gem"  ? new Color(0.82f, 0.58f, 1f)
                      : c.kind == "star" ? new Color(1f, 0.86f, 0.46f)
                      : c.kind == "gear" ? new Color(0.66f, 0.86f, 1f)
                                         : new Color(1f, 0.88f, 0.45f);
            FloatLootText(c.rt.anchoredPosition, txt, col);
            if (c.rt != null) _runner?.StartCoroutine(LootCollectAnim(c.rt));
        }

        // XP grant via the single canonical curve (Sparq.Systems.Progression).
        private static void GrantXp(Sparq.Core.PlayerData d, int xp)
        {
            Sparq.Systems.Progression.GrantXp(d, xp);
        }

        private static IEnumerator LootCollectAnim(RectTransform rt)
        {
            if (rt == null) yield break;
            var imgs = rt.GetComponentsInChildren<Image>();
            Vector3 baseScale = rt.localScale;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f, dur = 0.28f;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                rt.localScale = baseScale * (1f + 0.55f * p);
                rt.anchoredPosition = basePos + new Vector2(0, 34f * p);
                foreach (var im in imgs) if (im != null) { var cc = im.color; cc.a = 1f - p; im.color = cc; }
                t += Time.deltaTime; yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void FloatLootText(Vector2 pos, string text, Color color)
        {
            if (_mapContent == null) return;
            var t = MakeText(_mapContent, "LootFloat", $"<b>{text}</b>",
                new Vector2(0.5f, 0.5f), new Vector2(240, 64), 40, FontStyles.Bold, color);
            t.rectTransform.anchoredPosition = pos + new Vector2(0, 64);
            t.alignment = TextAlignmentOptions.Center;
            t.outlineWidth = 0.30f; t.outlineColor = Color.black;
            t.richText = true; t.raycastTarget = false;
            _runner?.StartCoroutine(FloatUpAndFade(t));
        }

        private static IEnumerator FloatUpAndFade(TMP_Text t)
        {
            if (t == null) yield break;
            Vector2 home = t.rectTransform.anchoredPosition;
            float dur = 0.9f, el = 0f;
            while (el < dur && t != null)
            {
                float p = el / dur;
                t.rectTransform.anchoredPosition = home + new Vector2(0, 80f * p);
                var c = t.color; c.a = 1f - p; t.color = c;
                el += Time.deltaTime; yield return null;
            }
            if (t != null) Object.Destroy(t.gameObject);
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: ENEMIES
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildEnemies(string mapId)
        {
            // Position-only layout — the 6 elite slots cycle through the active
            // zone's elite types, and the 7th slot at the top is the themed boss.
            var elitePositions = new[]
            {
                new Vector2(-450, -900),
                new Vector2( 460, -500),
                new Vector2(  80, -120),
                new Vector2(-380,  300),
                new Vector2( 420,  700),
                new Vector2(  60, 1080),
            };
            var bossPos = new Vector2(-200, 1420);

            var z = CurrentZoneEnemies();
            if (z.elites == null || z.elites.Length == 0) return;

            // Elites cycle through the zone's elite pool.
            for (int i = 0; i < elitePositions.Length; i++)
            {
                var kind = z.elites[i % z.elites.Length];
                var e = MakeMapEnemy(elitePositions[i], kind.name, kind.idlePath, mapId);
                _enemies.Add(e);
            }

            // Boss — themed per zone, marked isBoss + scaled 1.45×.
            var be = MakeMapEnemy(bossPos, z.boss.name, z.boss.idlePath, mapId);
            if (be != null)
            {
                be.isBoss = true;
                if (be.rt != null) be.rt.sizeDelta = new Vector2(ENEMY_HEIGHT * 1.45f, ENEMY_HEIGHT * 1.45f);
                if (be.shadow != null) be.shadow.rectTransform.sizeDelta *= 1.45f;
                _enemies.Add(be);
            }
        }

        private static MapEnemy MakeMapEnemy(Vector2 pos, string enemyKey, string idlePath, string biome)
        {
            var go = new GameObject($"MapEnemy_{enemyKey}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_mapContent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ENEMY_HEIGHT, ENEMY_HEIGHT);
            rt.anchoredPosition = pos;

            var e = new MapEnemy { rt = rt, enemyKey = enemyKey, biome = biome };
            e.img = go.GetComponent<Image>();
            e.img.preserveAspect = true;
            e.img.raycastTarget = false;
            // Alpha-crop tight so the enemy stands grounded (feet at the box base
            // → shadow lands under them) instead of floating in sprite padding.
            // Runs in both Editor + Player builds. SpriteLoader handles the fork.
            e.idleFrames = Sparq.UI.HeroPortrait.LoadCroppedFrames(idlePath, 8);
            if (e.idleFrames == null || e.idleFrames.Length == 0)
                e.idleFrames = LoadFrameSequence(idlePath, 8);
            if (e.idleFrames != null && e.idleFrames.Length > 0)
            {
                e.img.sprite = e.idleFrames[0];
                // Start ghosted — RevealLoop ramps alpha as the player approaches.
                e.img.color = new Color(1f, 1f, 1f, ENEMY_HIDDEN_ALPHA);
            }
            else
            {
                // No enemy art loaded — fall back to a red circle placeholder
                // so the white box doesn't show. Same approach as hero gold circle.
                var circleSp = Sparq.Core.SpriteLoader.Load(
                    "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png");
                if (circleSp != null) e.img.sprite = circleSp;
                // Red tint, slightly transparent so it still "ghosts" until approached.
                e.img.color = new Color(0.95f, 0.30f, 0.30f, ENEMY_HIDDEN_ALPHA);
            }

            // Enemy shadow — also faded; RevealLoop tracks the parent image alpha.
            var sh = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(go.transform, false);
            sh.transform.SetAsFirstSibling();
            var shRT = sh.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0.5f, 0); shRT.anchorMax = new Vector2(0.5f, 0);
            shRT.pivot = new Vector2(0.5f, 0.5f);
            shRT.anchoredPosition = new Vector2(0, 14);
            shRT.sizeDelta = new Vector2(140, 28);
            var shImg = sh.GetComponent<Image>();
            shImg.color = new Color(0.20f, 0.0f, 0.0f, 0.55f * ENEMY_HIDDEN_ALPHA);
            shImg.sprite = MakeEllipseSprite();
            shImg.raycastTarget = false;
            e.shadow = shImg;

            // "!" exclamation marker over enemy head — invisible until close
            var bang = new GameObject("Bang", typeof(RectTransform));
            bang.transform.SetParent(go.transform, false);
            var bRT = bang.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(0, 4);
            bRT.sizeDelta = new Vector2(60, 60);
            var btm = bang.AddComponent<TextMeshProUGUI>();
            btm.text = "<color=#FF5566><b>!</b></color>";
            btm.fontSize = 56;
            btm.fontStyle = FontStyles.Bold;
            btm.alignment = TextAlignmentOptions.Center;
            btm.outlineWidth = 0.34f;
            btm.outlineColor = Color.black;
            btm.raycastTarget = false;
            btm.richText = true;
            // Start invisible — RevealLoop fades it in only when player is close
            // enough to see the threat icon. Hidden enemies have no warning.
            btm.alpha = 0f;
            e.bang = btm;
            return e;
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: JOYSTICK (drag-to-move, replaces tap-to-walk)
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildJoystick()
        {
            var root = new GameObject("Joystick",
                typeof(RectTransform), typeof(Image), typeof(JoystickInput));
            root.transform.SetParent(_root.transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(60, 60);
            rt.sizeDelta = new Vector2(220, 220);
            _joystickBg = rt;
            _joystickRadius = 100f;

            var bgImg = root.GetComponent<Image>();
            bgImg.color = Color.white;
            bgImg.raycastTarget = true;
#if UNITY_EDITOR
            EnsureSpriteImporter(JOYSTICK_BG_PATH);
#endif
            var sp = Sparq.Core.SpriteLoader.Load(JOYSTICK_BG_PATH);
            if (sp != null) bgImg.sprite = sp;

            // Handle (the inner draggable knob)
            var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(root.transform, false);
            _joystickHandle = handleGO.GetComponent<RectTransform>();
            _joystickHandle.anchorMin = new Vector2(0.5f, 0.5f);
            _joystickHandle.anchorMax = new Vector2(0.5f, 0.5f);
            _joystickHandle.pivot = new Vector2(0.5f, 0.5f);
            _joystickHandle.sizeDelta = new Vector2(110, 110);
            _joystickHandle.anchoredPosition = Vector2.zero;
            var hImg = handleGO.GetComponent<Image>();
            hImg.color = Color.white;
            hImg.raycastTarget = false;
#if UNITY_EDITOR
            EnsureSpriteImporter(JOYSTICK_HANDLE_PATH);
#endif
            var hsp = Sparq.Core.SpriteLoader.Load(JOYSTICK_HANDLE_PATH);
            if (hsp != null) hImg.sprite = hsp;
        }

        // Called by JoystickInput component on pointer events
        internal static void OnJoystickDrag(PointerEventData ev)
        {
            // Touching the stick takes manual control — cancel auto-pilot.
            if (_autoMode) { _autoMode = false; UpdateAutoButtonVisual(); }
            if (_joystickBg == null || _joystickHandle == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickBg, ev.position, ev.pressEventCamera, out local);
            // local is relative to bg pivot (0,0). We want centered at (size/2, size/2)
            Vector2 center = _joystickBg.sizeDelta * 0.5f;
            Vector2 delta = local - center;
            float mag = delta.magnitude;
            if (mag > _joystickRadius) delta = delta * (_joystickRadius / mag);
            _joystickHandle.anchoredPosition = delta;
            _joystickInput = delta / _joystickRadius;     // -1..1
        }

        internal static void OnJoystickRelease()
        {
            if (_joystickHandle != null) _joystickHandle.anchoredPosition = Vector2.zero;
            _joystickInput = Vector2.zero;
        }

        // ═══════════════════════════════════════════════════════════════════
        // BUILD: HUD (top resource bar, back button)
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildTopHud()
        {
            // Dark plate at the top with resource chips
            var plate = MakeImage(_root.transform, "TopHud", new Color(0.10f, 0.06f, 0.18f, 0.92f));
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(0.5f, 1);
            prt.sizeDelta = new Vector2(0, 90);
            prt.anchoredPosition = Vector2.zero;
            plate.raycastTarget = false;

            // Gold rule line under the plate
            var rule = MakeImage(plate.transform, "Rule", new Color(1f, 0.78f, 0.30f, 0.95f));
            var rrt = rule.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 0); rrt.anchorMax = new Vector2(1, 0);
            rrt.pivot = new Vector2(0.5f, 0);
            rrt.sizeDelta = new Vector2(0, 3);
            rule.raycastTarget = false;

            int coins = 0; int level = 1; int xp = 0;
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null) { coins = d.sparqCoins; level = d.level; xp = d.currentXP; }
            } catch {}

            // Coins chip (left) — keep a handle so loot pickups bump it live.
            _coinLabel = BuildResourceChip(plate.transform, ICON_COIN, coins.ToString("N0"),
                new Color(1f, 0.85f, 0.40f), new Vector2(0, 0.5f), new Vector2(40, -10));

            // Level chip (center)
            _levelLabel = BuildResourceChip(plate.transform, ICON_BOLT, $"Lv {level}",
                new Color(0.55f, 0.85f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0, -10));

            // XP chip (right)
            _xpLabel = BuildResourceChip(plate.transform, ICON_HEART, xp.ToString("N0") + " XP",
                new Color(0.95f, 0.55f, 0.55f), new Vector2(1, 0.5f), new Vector2(-220, -10));

            // Zone name banner — slim strip just under the HUD plate.
            var zoneGO = new GameObject("ZoneName", typeof(RectTransform));
            zoneGO.transform.SetParent(_root.transform, false);
            var zrt = zoneGO.GetComponent<RectTransform>();
            zrt.anchorMin = new Vector2(0.5f, 1); zrt.anchorMax = new Vector2(0.5f, 1);
            zrt.pivot = new Vector2(0.5f, 1);
            zrt.anchoredPosition = new Vector2(0, -96);
            zrt.sizeDelta = new Vector2(640, 44);
            _zoneLabel = zoneGO.AddComponent<TextMeshProUGUI>();
            _zoneLabel.text = CurrentZone().name;
            _zoneLabel.fontSize = 28;
            _zoneLabel.fontStyle = FontStyles.Bold;
            _zoneLabel.alignment = TextAlignmentOptions.Center;
            _zoneLabel.color = new Color(1f, 0.92f, 0.65f);
            _zoneLabel.outlineWidth = 0.25f;
            _zoneLabel.outlineColor = Color.black;
            _zoneLabel.raycastTarget = false;

            // Vitality strip — today's self-care buff, shown right under the zone
            // name so the wellness→power link is visible before you even fight.
            var vitGO = new GameObject("VitalityHud", typeof(RectTransform));
            vitGO.transform.SetParent(_root.transform, false);
            var vrt = vitGO.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0.5f, 1); vrt.anchorMax = new Vector2(0.5f, 1);
            vrt.pivot = new Vector2(0.5f, 1);
            vrt.anchoredPosition = new Vector2(0, -140);
            vrt.sizeDelta = new Vector2(680, 36);
            _vitalityLabel = vitGO.AddComponent<TextMeshProUGUI>();
            _vitalityLabel.fontSize = 24;
            _vitalityLabel.fontStyle = FontStyles.Bold;
            _vitalityLabel.alignment = TextAlignmentOptions.Center;
            _vitalityLabel.outlineWidth = 0.22f;
            _vitalityLabel.outlineColor = Color.black;
            _vitalityLabel.raycastTarget = false;
            UpdateVitalityLabel();
        }

        // Refresh the explore-HUD vitality strip from VitalityService.
        private static void UpdateVitalityLabel()
        {
            if (_vitalityLabel == null) return;
            int buff = 0;
            try { buff = Sparq.Systems.VitalityService.BuffPercent(); } catch {}
            if (buff > 0)
            {
                _vitalityLabel.text = $"<color=#7CFC8A>VITALITY  +{buff}% squad power</color>";
                _vitalityLabel.richText = true;
            }
            else
            {
                _vitalityLabel.text = "<color=#FFFFFF99>Do today's self-care to power up your squad</color>";
                _vitalityLabel.richText = true;
            }
        }

        private static TMP_Text BuildResourceChip(Transform parent, string iconPath, string text,
                                               Color tint, Vector2 anchor, Vector2 anchoredPos)
        {
            var chip = new GameObject("Chip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(220, 60);
            chip.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.10f, 0.85f);
            chip.GetComponent<Image>().raycastTarget = false;

            // Icon
            var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(chip.transform, false);
            var icRT = ic.GetComponent<RectTransform>();
            icRT.anchorMin = new Vector2(0, 0.5f); icRT.anchorMax = new Vector2(0, 0.5f);
            icRT.pivot = new Vector2(0, 0.5f);
            icRT.anchoredPosition = new Vector2(8, 0);
            icRT.sizeDelta = new Vector2(48, 48);
            var icImg = ic.GetComponent<Image>();
            icImg.preserveAspect = true; icImg.raycastTarget = false;
#if UNITY_EDITOR
            EnsureSpriteImporter(iconPath);
#endif
            var sp = Sparq.Core.SpriteLoader.Load(iconPath);
            if (sp != null) icImg.sprite = sp;

            var label = MakeText(chip.transform, "Lbl", text,
                new Vector2(1, 0.5f), new Vector2(150, 40), 22, FontStyles.Bold, tint);
            label.alignment = TextAlignmentOptions.Center;
            var lRT = label.rectTransform;
            lRT.pivot = new Vector2(1, 0.5f);
            lRT.anchoredPosition = new Vector2(-10, 0);
            label.outlineWidth = 0.20f; label.outlineColor = Color.black;
            label.raycastTarget = false;
            return label;
        }

        private static void BuildBackButton()
        {
            var btn = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            btn.transform.SetParent(_root.transform, false);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -110);
            rt.sizeDelta = new Vector2(160, 70);
            btn.GetComponent<Image>().color = new Color(1f, 0.78f, 0.30f, 0.95f);
            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Hide();
                // Re-open the lobby instead of falling back to the broken old
                // home scene. The lobby is now Sparq's true home page.
                try { Sparq.UI.HomeLobbyPanel.Show(); }
                catch (System.Exception ex) { Debug.LogError($"[WorldExplorePanel] Failed to reopen lobby: {ex.Message}"); }
            });

            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            lblGO.transform.SetParent(btn.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = "← BACK";
            tm.fontSize = 28;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = new Color(0.10f, 0.05f, 0.18f);
            tm.outlineWidth = 0.18f;
            tm.outlineColor = new Color(1f, 0.95f, 0.7f);
            tm.raycastTarget = false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // RUNTIME LOOPS
        // ═══════════════════════════════════════════════════════════════════
        // No smoothing — stick release = instant stop. Drift was the "loose"
        // feel the user complained about. Direct input + deadzone + power
        // curve = tight, immediate response.
        private static IEnumerator MovementLoop()
        {
            while (_root != null)
            {
                if (_battleHandoff) { yield return null; continue; }
                if (_player != null && _player.rt != null)
                {
                    Vector2 raw = _joystickInput;
                    float mag = raw.magnitude;

                    if (mag >= JOYSTICK_DEADZONE)
                    {
                        // Re-map [DEADZONE..1] → [0..1] so we don't jump from
                        // 0% to ~25% speed the moment we cross the deadzone.
                        float remapped = (mag - JOYSTICK_DEADZONE) / (1f - JOYSTICK_DEADZONE);
                        // Power curve — at 2.0 a half-deflection is only 25% speed,
                        // so you really have to commit to going fast. Tight feel.
                        float curved = Mathf.Pow(Mathf.Clamp01(remapped), INPUT_RESPONSE_POW);
                        Vector2 dirUnit = raw / mag;
                        Vector2 step = dirUnit * curved * (PLAYER_MAX_SPEED * Time.deltaTime);

                        Vector2 next = _player.rt.anchoredPosition + step;
                        // Clamp to PLAYABLE area — not the full map sprite. This
                        // is the visible green island; water/sky borders are off-limits.
                        float halfW = MAP_W * 0.5f - PLAY_AREA_MARGIN_X;
                        float halfH = MAP_H * 0.5f - PLAY_AREA_MARGIN_Y;
                        next.x = Mathf.Clamp(next.x, -halfW, halfW);
                        next.y = Mathf.Clamp(next.y, -halfH, halfH);
                        _player.rt.anchoredPosition = next;

                        // Face direction (only flip when input is meaningful)
                        if (Mathf.Abs(dirUnit.x) > 0.10f)
                        {
                            var s = _player.rt.localScale;
                            s.x = (dirUnit.x >= 0) ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                            _player.rt.localScale = s;
                        }
                    }
                    // No else needed — releasing the stick instantly halts movement.
                }
                yield return null;
            }
        }

        // Fog-of-war reveal: every frame, fade chests and enemies based on
        // distance from the player. Inner radius = full alpha, outer radius =
        // hidden alpha. Smooth band between gives a nice "approaching the
        // unknown" feel without ever fully popping in.
        private static IEnumerator RevealLoop()
        {
            const float REVEAL_BAND = 60f;   // soft falloff — reveal eases in over this distance
            while (_root != null)
            {
                if (_player != null && _player.rt != null)
                {
                    Vector2 p = _player.rt.anchoredPosition;

                    foreach (var c in _chests)
                    {
                        if (c == null || c.img == null || c.rt == null) continue;
                        if (c.opened) continue;   // opened chests stay full-alpha
                        float d = Vector2.Distance(p, c.rt.anchoredPosition);
                        // 0..1 reveal: 1 when inside reveal radius, 0 when far
                        float t = Mathf.InverseLerp(
                            CHEST_REVEAL_RADIUS + REVEAL_BAND, CHEST_REVEAL_RADIUS, d);
                        float a = Mathf.Lerp(CHEST_HIDDEN_ALPHA, 1f, Mathf.Clamp01(t));
                        var col = c.img.color; col.a = a; c.img.color = col;
                    }

                    foreach (var e in _enemies)
                    {
                        if (e == null || e.img == null || e.rt == null) continue;
                        if (e.defeated) continue;
                        float d = Vector2.Distance(p, e.rt.anchoredPosition);
                        float t = Mathf.InverseLerp(
                            ENEMY_REVEAL_RADIUS + REVEAL_BAND, ENEMY_REVEAL_RADIUS, d);
                        float a = Mathf.Lerp(ENEMY_HIDDEN_ALPHA, 1f, Mathf.Clamp01(t));
                        var col = e.img.color; col.a = a; e.img.color = col;
                        // Shadow follows the body alpha but capped at its base 0.55
                        if (e.shadow != null)
                        {
                            var sc = e.shadow.color; sc.a = 0.55f * a; e.shadow.color = sc;
                        }
                        // "!" only appears when the enemy is fully revealed —
                        // hidden enemies give no warning at all.
                        if (e.bang != null)
                            e.bang.alpha = Mathf.Clamp01((t - 0.6f) / 0.4f);
                    }

                    // Scattered loot — fully hidden until the squad is close, then
                    // fades in. (Collected pieces run their own pop, so skip them.)
                    foreach (var c in _collectibles)
                    {
                        if (c == null || c.collected || c.cg == null || c.rt == null) continue;
                        float d = Vector2.Distance(p, c.rt.anchoredPosition);
                        float t = Mathf.InverseLerp(
                            LOOT_REVEAL_RADIUS + REVEAL_BAND, LOOT_REVEAL_RADIUS, d);
                        c.cg.alpha = Mathf.Lerp(LOOT_HIDDEN_ALPHA, 1f, Mathf.Clamp01(t));
                    }
                }
                yield return null;
            }
        }

        // Allies trail the player smoothly with their assigned offset.
        // They lerp toward (player + offset) so they follow but don't snap.
        private static IEnumerator AllyFollowLoop()
        {
            while (_root != null)
            {
                if (_battleHandoff) { yield return null; continue; }
                if (_player != null && _player.rt != null)
                {
                    // The whole squad faces whichever way the hero is facing —
                    // reads as one cohesive unit instead of the pet wheeling around
                    // to face its own follow vector (the old "moving weird" look).
                    float heroFaceSign = Mathf.Sign(_player.rt.localScale.x == 0 ? 1f : _player.rt.localScale.x);
                    foreach (var a in _allies)
                    {
                        if (a == null || a.rt == null) continue;
                        Vector2 target = _player.rt.anchoredPosition + a.followOffset;
                        Vector2 cur = a.rt.anchoredPosition;
                        Vector2 dir = target - cur;
                        float dist = dir.magnitude;
                        if (dist > ARRIVAL_THRESHOLD)
                        {
                            Vector2 step = dir.normalized * Mathf.Min(ALLY_FOLLOW_SPEED * Time.deltaTime, dist);
                            a.rt.anchoredPosition = cur + step;
                        }
                        // Match the hero's facing every frame (no independent flips).
                        var s = a.rt.localScale;
                        s.x = heroFaceSign * Mathf.Abs(s.x);
                        a.rt.localScale = s;
                    }
                }
                yield return null;
            }
        }

        private static IEnumerator CameraFollowLoop()
        {
            while (_root != null)
            {
                if (_player != null && _player.rt != null && _mapContent != null && _viewport != null)
                {
                    Vector2 desired = -_player.rt.anchoredPosition;
                    Vector2 viewSize = _viewport.rect.size;
                    float maxX = (MAP_W - viewSize.x) * 0.5f;
                    float maxY = (MAP_H - viewSize.y) * 0.5f;
                    if (maxX < 0) maxX = 0;
                    if (maxY < 0) maxY = 0;
                    desired.x = Mathf.Clamp(desired.x, -maxX, maxX);
                    desired.y = Mathf.Clamp(desired.y, -maxY, maxY);
                    _mapContent.anchoredPosition = Vector2.Lerp(
                        _mapContent.anchoredPosition, desired, Time.deltaTime * 6f);
                }
                yield return null;
            }
        }

        private static IEnumerator IdleAnimateLoop()
        {
            float frameDur = 0.13f;
            while (_root != null)
            {
                AnimateFrames(_player, frameDur);
                foreach (var a in _allies) AnimateFrames(a, frameDur);
                foreach (var e in _enemies)
                {
                    if (e == null || e.defeated || e.img == null || e.idleFrames == null || e.idleFrames.Length == 0) continue;
                    e.frameTimer += Time.deltaTime;
                    if (e.frameTimer >= frameDur)
                    {
                        e.frameTimer = 0;
                        e.frameIdx = (e.frameIdx + 1) % e.idleFrames.Length;
                        e.img.sprite = e.idleFrames[e.frameIdx];
                    }
                }
                yield return null;
            }
        }

        private static void AnimateFrames(Squadmate sm, float frameDur)
        {
            if (sm == null || sm.img == null || sm.idleFrames == null || sm.idleFrames.Length == 0) return;
            sm.frameTimer += Time.deltaTime;
            if (sm.frameTimer >= frameDur)
            {
                sm.frameTimer = 0;
                sm.frameIdx = (sm.frameIdx + 1) % sm.idleFrames.Length;
                sm.img.sprite = sm.idleFrames[sm.frameIdx];
            }
        }

        // Auto-trigger battle when player walks within ENEMY_AGGRO_RADIUS of an enemy
        private static IEnumerator EnemyProximityLoop()
        {
            while (_root != null)
            {
                if (_battleHandoff) { yield return null; continue; }
                if (_player != null && _player.rt != null)
                {
                    Vector2 pp = _player.rt.anchoredPosition;
                    foreach (var e in _enemies)
                    {
                        if (e == null || e.defeated || e.rt == null) continue;
                        if (Vector2.Distance(pp, e.rt.anchoredPosition) <= ENEMY_AGGRO_RADIUS)
                        {
                            _battleHandoff = true;
                            _runner.StartCoroutine(LaunchBattleAndReturn(e));
                            break;
                        }
                    }
                }
                yield return null;
            }
        }

        private static IEnumerator LaunchBattleAndReturn(MapEnemy e)
        {
            // Brief pause so the encounter reads
            yield return new WaitForSeconds(0.15f);
            // Stop map music — battle takes over
            try { Sparq.UI.MapBgm.Stop(); } catch {}
            // Hide the world panel (we'll bring it back when battle ends)
            if (_root != null) _root.SetActive(false);

            // Launch the squad battle — regular enemies get one quick skirmish,
            // the boss gets the full 3-wave set piece. Zone difficulty scales it.
            // The first arg is the on-screen STAGE TITLE — pair the zone name
            // with the enemy walked-into so the banner reads e.g.
            //   "Greenwood Vale — Forest Goblin"   (or "Greenwood Vale — Stone Brute" on boss)
            // instead of bare "Forest Goblin" (the internal sprite key).
            bool wasBoss = e != null && e.isBoss;
            string stageTitle = $"{CurrentZone().name} — {e.enemyKey}";
            try { Sparq.Systems.SquadBattle.Start(stageTitle, e.biome, e.isBoss, CurrentZone().difficulty); } catch {}

            // Wait until the battle root is destroyed (player closed via win/lose/flee)
            while (GameObject.Find("SquadBattleRoot") != null) yield return null;

            // Did the squad actually win? A loss/flee leaves the enemy standing.
            bool won = false;
            try { won = Sparq.Systems.SquadBattle.LastBattleWon; } catch {}

            // Return to world.
            if (_root != null) _root.SetActive(true);
            try { Sparq.UI.MapBgm.Ensure(); } catch {}

            if (!won)
            {
                // DEFEAT — the enemy stays on the map; no reward, no advancement.
                // Stop AUTO (so it doesn't march straight back into the same loss)
                // and retreat the squad clear of the aggro radius to regroup.
                _battleHandoff = false;
                if (_autoMode) { _autoMode = false; UpdateAutoButtonVisual(); }
                if (_player != null && _player.rt != null)
                {
                    float floorY = -(MAP_H * 0.5f - PLAY_AREA_MARGIN_Y);
                    var pos = _player.rt.anchoredPosition;
                    pos.y = Mathf.Max(floorY, pos.y - (ENEMY_AGGRO_RADIUS + 180f));
                    _player.rt.anchoredPosition = pos;
                }
                ShowDefeatHint();
                yield break;
            }

            // VICTORY — reflect any level-up/XP/coins the win granted, then
            // clear the enemy and collect the spoils.
            RefreshHudStats();
            if (e != null && e.rt != null)
            {
                e.defeated = true;
                Object.Destroy(e.rt.gameObject);
            }
            // Boss defeated → guaranteed loot drop (gear + gold) to feed the chase.
            if (wasBoss) GrantBossReward();
            // Step the player back slightly so they don't immediately re-aggro a nearby enemy
            if (_player != null && _player.rt != null)
            {
                _player.rt.anchoredPosition += new Vector2(0, -60);
            }
            _battleHandoff = false;

            // If all enemies cleared → either auto-advance to the next zone
            // (AUTO on = endless idle progression) or finish back to the map.
            bool allDead = true;
            foreach (var en in _enemies) if (en != null && !en.defeated) { allDead = false; break; }
            if (allDead)
            {
                yield return new WaitForSeconds(0.5f);
                // Zone cleared → bank progress and move to the next zone.
                AdvanceZone(stayInMap: _autoMode);
            }
        }

        // Brief on-map nudge after a loss: you must get stronger to win, not just
        // walk past. Auto-fades; doesn't block input.
        private static void ShowDefeatHint()
        {
            if (_root == null) return;
            var banner = MakeText(_root.transform, "DefeatHint",
                "<color=#FF8080><b>DEFEATED</b></color>\n<size=42><color=#FFFFFFDD>Level up your squad, then try again.</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(980, 220), 64, FontStyles.Bold, Color.white);
            banner.alignment = TextAlignmentOptions.Center;
            banner.outlineWidth = 0.30f; banner.outlineColor = Color.black;
            banner.richText = true; banner.raycastTarget = false;
            _runner.StartCoroutine(FadeAndDestroy(banner.gameObject, 2.2f));
        }

        // Persist the furthest zone reached.
        private static void SaveZoneProgress()
        {
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null && _zoneIndex > d.worldZoneIndex)
                {
                    d.worldZoneIndex = _zoneIndex;
                    Sparq.Core.SaveService.Save();
                }
            }
            catch {}
        }

        // Advance to the next zone. stayInMap (AUTO) rebuilds the world in place
        // and keeps idling; otherwise we flash a "ZONE CLEARED" banner and return
        // to the lobby (progress is saved either way).
        private static void AdvanceZone(bool stayInMap)
        {
            _zoneIndex++;
            SaveZoneProgress();
            _biome = CurrentZone().biomeKey;

            if (!stayInMap)
            {
                ShowZoneClearedAndReturn();
                return;
            }

            // Swap the backdrop to the new zone, then rebuild everything.
            RefreshZoneBackdrop();

            foreach (var en in _enemies)
                if (en != null && en.rt != null) Object.Destroy(en.rt.gameObject);
            _enemies.Clear();
            foreach (var c in _collectibles)
                if (c != null && c.rt != null) Object.Destroy(c.rt.gameObject);
            _collectibles.Clear();

            BuildEnemies(_biome);
            BuildMinionWaves(_biome);
            BuildChests(_biome);
            BuildCollectibles(_biome);

            // Recentre the squad at the bottom so it walks "up" into the new zone.
            if (_player != null && _player.rt != null)
                _player.rt.anchoredPosition = new Vector2(0, -(MAP_H * 0.5f - PLAY_AREA_MARGIN_Y) + 200f);

            UpdateZoneLabel();

            // Brief zone banner (auto-fades; does NOT close the panel).
            var z = CurrentZone();
            var banner = MakeText(_root.transform, "ZoneBanner",
                $"<color=#FFD466><b>{z.name.ToUpper()}</b></color>",
                new Vector2(0.5f, 0.5f), new Vector2(960, 180), 72, FontStyles.Bold, Color.white);
            banner.alignment = TextAlignmentOptions.Center;
            banner.richText = true; banner.raycastTarget = false;
            try { banner.outlineWidth = 0.30f; banner.outlineColor = Color.black; } catch {}
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}
            _runner.StartCoroutine(FadeAndDestroy(banner.gameObject, 1.6f));
        }

        private static IEnumerator FadeAndDestroy(GameObject go, float life)
        {
            var tm = go != null ? go.GetComponent<TMP_Text>() : null;
            float t = 0f;
            while (t < life && go != null)
            {
                t += Time.unscaledDeltaTime;
                if (tm != null && t > life - 0.5f)
                {
                    var c = tm.color; c.a = Mathf.Lerp(1f, 0f, (t - (life - 0.5f)) / 0.5f); tm.color = c;
                }
                yield return null;
            }
            if (go != null) Object.Destroy(go);
        }

        // ═══════════════════════════════════════════════════════════════════
        // IDLE / AUTO-PILOT
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildAutoButton()
        {
            var go = MakeImage(_root.transform, "AutoBtn", new Color(0.30f, 0.62f, 0.42f, 1f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-28, 340);
            rt.sizeDelta = new Vector2(190, 96);
            var btn = go.gameObject.AddComponent<Button>();
            _autoBtnImg = go;
            btn.targetGraphic = go;
            btn.onClick.AddListener(ToggleAuto);

            _autoBtnLbl = MakeText(go.transform, "L", "AUTO  OFF",
                new Vector2(0.5f, 0.5f), new Vector2(180, 90), 34, FontStyles.Bold, Color.white);
            _autoBtnLbl.alignment = TextAlignmentOptions.Center;
            try { _autoBtnLbl.outlineWidth = 0.22f; _autoBtnLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            UpdateAutoButtonVisual();

            // Idle earn-rate chip just above the AUTO button — shows the squad's
            // current AFK income (Vitality-boosted) so players see the idle value.
            int rate = 0, vitBonus = 0;
            try { rate = Sparq.Systems.AfkRewardService.EffectiveCoinsPerHour(); } catch {}
            try { vitBonus = Sparq.Systems.AfkRewardService.VitalityBonusPercent(); } catch {}
            var rateChip = MakeImage(_root.transform, "AfkRateChip", new Color(0.08f, 0.08f, 0.12f, 0.92f));
            var rcRT = rateChip.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(1, 0); rcRT.anchorMax = new Vector2(1, 0);
            rcRT.pivot = new Vector2(1, 0);
            rcRT.anchoredPosition = new Vector2(-28, 448);
            rcRT.sizeDelta = new Vector2(190, 56);
            // Show the vitality bump inline so the boost is felt: "+840/hr  +20%"
            string rateText = vitBonus > 0
                ? $"<color=#FFCB4D>+{rate:N0}</color>/hr <size=70%><color=#7CFC8A>+{vitBonus}%</color></size>"
                : $"<color=#FFCB4D>+{rate:N0}</color>/hr";
            var rateLbl = MakeText(rateChip.transform, "L", rateText,
                new Vector2(0.5f, 0.5f), new Vector2(180, 50), 26, FontStyles.Bold, CREAM);
            rateLbl.alignment = TextAlignmentOptions.Center;
            rateLbl.richText = true;
            try { rateLbl.outlineWidth = 0.2f; rateLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
        }

        private static readonly Color CREAM = new Color(1f, 0.97f, 0.85f, 1f);

        private static void ToggleAuto()
        {
            _autoMode = !_autoMode;
            if (!_autoMode) _joystickInput = Vector2.zero;   // halt when turning off
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            UpdateAutoButtonVisual();
        }

        private static void UpdateAutoButtonVisual()
        {
            if (_autoBtnImg != null)
                _autoBtnImg.color = _autoMode
                    ? new Color(0.30f, 0.72f, 0.40f, 1f)   // green = ON
                    : new Color(0.40f, 0.40f, 0.48f, 1f);  // grey = OFF
            if (_autoBtnLbl != null)
                _autoBtnLbl.text = _autoMode ? "AUTO  ON" : "AUTO  OFF";
        }

        // Steers the squad toward the nearest target each frame while AUTO is on.
        // Reuses the joystick input channel — MovementLoop moves the player as if
        // the stick were pushed, and EnemyProximityLoop fires battles on contact.
        private static IEnumerator AutoPilotLoop()
        {
            while (_root != null)
            {
                if (!_autoMode || _battleHandoff || _player == null || _player.rt == null)
                { yield return null; continue; }

                Vector2 pp = _player.rt.anchoredPosition;

                // Priority: nearest live enemy, then nearest unopened chest.
                RectTransform targetRT = null;
                Chest targetChest = null;
                float best = float.MaxValue;
                foreach (var e in _enemies)
                {
                    if (e == null || e.defeated || e.rt == null) continue;
                    float d = Vector2.Distance(pp, e.rt.anchoredPosition);
                    if (d < best) { best = d; targetRT = e.rt; targetChest = null; }
                }
                if (targetRT == null)
                {
                    foreach (var c in _chests)
                    {
                        if (c == null || c.opened || c.rt == null) continue;
                        float d = Vector2.Distance(pp, c.rt.anchoredPosition);
                        if (d < best) { best = d; targetRT = c.rt; targetChest = c; }
                    }
                }

                if (targetRT == null)
                {
                    // Nothing left to do — stand still (stage is effectively cleared).
                    _joystickInput = Vector2.zero;
                    yield return null; continue;
                }

                // Chest in reach → grab it silently and move on.
                if (targetChest != null && best <= AUTO_CHEST_PICKUP_RADIUS)
                {
                    CollectChestSilent(targetChest);
                    _joystickInput = Vector2.zero;
                    yield return null; continue;
                }

                // Drive toward the target at full deflection; proximity loops
                // handle the actual battle / pickup trigger.
                Vector2 dir = targetRT.anchoredPosition - pp;
                _joystickInput = dir.sqrMagnitude > 1f ? dir.normalized : Vector2.zero;
                yield return null;
            }
        }

        // Auto-collect a chest without the blocking lore popup (used by AUTO mode).
        private static void CollectChestSilent(Chest c)
        {
            if (c == null || c.opened) return;
            c.opened = true;
            if (c.openSprite != null && c.img != null) c.img.sprite = c.openSprite;
            _runner?.StartCoroutine(ChestPopAnim(c.rt));
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null) { d.sparqCoins += c.rewardGold; Sparq.Core.SaveService.Save(); }
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
            }
            catch {}
        }

        // Manual (non-AUTO) zone clear: celebrate + return to the lobby. The new
        // zone is already saved, so re-entering the map resumes there.
        private static void ShowZoneClearedAndReturn()
        {
            string next = CurrentZone().name;
            var banner = MakeText(_root.transform, "ZoneCleared",
                $"<color=#FFD466><b>ZONE CLEARED!</b></color>\n<size=44><color=#FFFFFFDD>Next: {next}</color></size>",
                new Vector2(0.5f, 0.5f), new Vector2(980, 240), 78, FontStyles.Bold, Color.white);
            banner.alignment = TextAlignmentOptions.Center;
            banner.outlineWidth = 0.30f; banner.outlineColor = Color.black;
            banner.richText = true; banner.raycastTarget = false;
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}
            _runner.StartCoroutine(StageCompleteCloseAfter(2.2f));
        }

        private static IEnumerator StageCompleteCloseAfter(float wait)
        {
            yield return new WaitForSeconds(wait);
            Hide();
        }

        // Refresh the top-HUD zone name label to the current zone.
        private static void UpdateZoneLabel()
        {
            if (_zoneLabel != null) _zoneLabel.text = CurrentZone().name;
        }

        // Refresh the coin / level / XP chips from the save (call after a fight or
        // loot pickup so leveling shows immediately without reopening the map).
        private static void RefreshHudStats()
        {
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d == null) return;
                if (_coinLabel  != null) _coinLabel.text  = d.sparqCoins.ToString("N0");
                if (_levelLabel != null) _levelLabel.text = $"Lv {d.level}";
                if (_xpLabel    != null) _xpLabel.text    = d.currentXP.ToString("N0") + " XP";
            }
            catch {}
        }

        // Guaranteed boss drop: a gear item (rarity skews up with zone) + a gold
        // bonus, surfaced via the chest popup so the win pays off into the gear loop.
        private static void GrantBossReward()
        {
            string itemName = "Spoils"; var rarityCol = new Color(1f, 0.85f, 0.4f);
            bool isSignature = false;
            int gold = Mathf.RoundToInt(150 * CurrentZone().lootMult);
            try
            {
                int lvl = Sparq.Core.SaveService.Data?.level ?? 1;
                Sparq.Systems.EquipmentService.Item drop = null;

                // First-time clear of a defined zone → guaranteed signature legendary.
                // (Endless zones past the defined list have no signatureItemId.)
                string sigId = CurrentZone().signatureItemId;
                if (!string.IsNullOrEmpty(sigId))
                {
                    bool alreadyOwned = false;
                    try { alreadyOwned = Sparq.Systems.EquipmentService.OwnedIds().Contains(sigId); } catch {}
                    if (!alreadyOwned)
                    {
                        drop = Sparq.Systems.EquipmentService.ById(sigId);
                        isSignature = (drop != null);
                    }
                }

                // Repeat clears + endless zones → biased random roll (keep best of N).
                if (drop == null)
                {
                    drop = Sparq.Systems.EquipmentService.RollLoot(lvl);
                    int rolls = 1 + Mathf.Min(3, _zoneIndex / 2);
                    for (int i = 1; i < rolls; i++)
                    {
                        var alt = Sparq.Systems.EquipmentService.RollLoot(lvl);
                        if (alt != null && drop != null && (int)alt.rarity > (int)drop.rarity) drop = alt;
                    }
                }

                if (drop != null)
                {
                    Sparq.Systems.EquipmentService.Grant(drop.id);
                    itemName = drop.name;
                    rarityCol = Sparq.Systems.EquipmentService.RarityColor(drop.rarity);
                }
                var d = Sparq.Core.SaveService.Data;
                if (d != null) { d.sparqCoins += gold; Sparq.Core.SaveService.Save(); }
                if (_coinLabel != null && d != null) _coinLabel.text = d.sparqCoins.ToString("N0");
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}

                // Zone-clear ally unlock (paired with the signature drop). If
                // newly unlocked, fire the recruit celebration so the player
                // sees their new squadmate on the spot.
                string allyId = Sparq.Systems.AllyRoster.AllyForZone(_zoneIndex);
                if (!string.IsNullOrEmpty(allyId) && Sparq.Systems.AllyRoster.TryUnlock(allyId))
                {
                    try { Sparq.UI.AllyRecruitPopup.Show(allyId); }
                    catch (System.Exception ex) { Debug.LogWarning($"[WorldExplore] ally recruit popup failed: {ex.Message}"); }
                }
            }
            catch (System.Exception ex) { Debug.LogWarning($"[WorldExplore] boss reward failed: {ex.Message}"); }

            // Reward popup (reuses the chest popup styling).
            if (_root != null)
            {
                var popup = new GameObject("BossReward", typeof(RectTransform), typeof(Image));
                popup.transform.SetParent(_root.transform, false);
                var rt = popup.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(760, 260);
                popup.GetComponent<Image>().color = new Color(0.10f, 0.07f, 0.18f, 0.96f);

                // Header reads BIG on a signature drop so the moment lands.
                string headText = isSignature
                    ? $"<color=#FFD466>SIGNATURE DROP!</color>\n<size=60%><color=#FFFFFFDD>{CurrentZone().name} cleared</color></size>"
                    : "<color=#FFD466>BOSS DEFEATED!</color>";
                var head = MakeText(popup.transform, "Head", headText,
                    new Vector2(0.5f, 0.78f), new Vector2(720, 86), isSignature ? 44 : 40, FontStyles.Bold, Color.white);
                head.alignment = TextAlignmentOptions.Center; head.richText = true;
                head.outlineWidth = 0.3f; head.outlineColor = Color.black;

                string hex = ColorUtility.ToHtmlStringRGB(rarityCol);
                var drop = MakeText(popup.transform, "Drop",
                    $"<color=#{hex}><b>{itemName}</b></color>\n<color=#FFD466>+{gold} gold</color>",
                    new Vector2(0.5f, 0.36f), new Vector2(720, 110), 30, FontStyles.Bold, Color.white);
                drop.alignment = TextAlignmentOptions.Center; drop.richText = true;
                drop.outlineWidth = 0.2f; drop.outlineColor = Color.black;

                _runner?.StartCoroutine(PopupAutoClose(popup, 2.8f));
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════
        private static Sprite[] LoadFrameSequence(string base3DigitPath, int count)
        {
            // Runs in both Editor + Player builds. Was previously editor-only —
            // so enemy + companion frame sequences returned empty in APK and
            // characters rendered as default white boxes.
            var arr = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                string num = i.ToString().PadLeft(3, '0');
                string path = base3DigitPath + num + ".png";
#if UNITY_EDITOR
                EnsureSpriteImporter(path);
#endif
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null) arr.Add(sp);
            }
            return arr.ToArray();
        }

        #if UNITY_EDITOR
        private static void EnsureSpriteImporter(string path)
        {
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
            { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
            if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
            if (imp.spriteImportMode != UnityEditor.SpriteImportMode.Single)
            { imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
        #endif

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            Vector2 anchor, Vector2 size, int fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.sizeDelta = size; rt.pivot = new Vector2(0.5f, 0.5f);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.fontStyle = style;
            tm.color = color;
            return tm;
        }

        private static Sprite _ellipseSp;
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

        // Soft radial circle (white, alpha falls off to the edge) — used as the
        // glow halo behind scattered loot so it pops on the busy map.
        private static Sprite _softCircleSp;
        private static Sprite MakeSoftCircleSprite()
        {
            if (_softCircleSp != null) return _softCircleSp;
            int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Bilinear;
            float r = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - r) / r, dy = (y - r) / r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;   // ease the falloff so the centre stays bright
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _softCircleSp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _softCircleSp;
        }

        private class RunnerMB : MonoBehaviour { }
    }

    /// <summary>
    /// Joystick drag handler — captures pointer events and forwards them to
    /// WorldExplorePanel for input vector processing.
    /// </summary>
    public class JoystickInput : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
    {
        private int _activePointerId = int.MinValue;
        public void OnPointerDown(PointerEventData ev)
        {
            _activePointerId = ev.pointerId;
            WorldExplorePanel.OnJoystickDrag(ev);
        }
        public void OnDrag(PointerEventData ev)
        {
            // Only respond to the originally captured pointer — prevents a second
            // finger from hijacking the stick mid-move.
            if (ev.pointerId != _activePointerId && _activePointerId != int.MinValue) return;
            WorldExplorePanel.OnJoystickDrag(ev);
        }
        public void OnPointerUp(PointerEventData ev)
        {
            if (ev.pointerId != _activePointerId && _activePointerId != int.MinValue) return;
            _activePointerId = int.MinValue;
            WorldExplorePanel.OnJoystickRelease();
        }
        // EndDrag fires more reliably than PointerUp when the finger drags off
        // the joystick rect before lifting — catches the "stuck forward" bug
        // where the character kept moving after release.
        public void OnEndDrag(PointerEventData ev)
        {
            if (ev.pointerId != _activePointerId && _activePointerId != int.MinValue) return;
            _activePointerId = int.MinValue;
            WorldExplorePanel.OnJoystickRelease();
        }
        // Safety net: every frame, verify the captured pointer/touch is still
        // physically pressed. If the Android touch ended without firing
        // PointerUp (a known UGUI bug on some devices), release manually.
        private void Update()
        {
            if (_activePointerId == int.MinValue) return;
            // On touch devices, check if the corresponding touch is still active.
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                    if (Input.GetTouch(i).fingerId == _activePointerId) return;
                // Captured touch is gone — release.
                _activePointerId = int.MinValue;
                WorldExplorePanel.OnJoystickRelease();
            }
            else if (_activePointerId == -1)
            {
                // Mouse path (editor / Bluetooth mouse). pointerId -1 = left mouse.
                if (!Input.GetMouseButton(0))
                {
                    _activePointerId = int.MinValue;
                    WorldExplorePanel.OnJoystickRelease();
                }
            }
            else if (Input.touchCount == 0 && _activePointerId >= 0)
            {
                // Touch ID was captured but no touches active — release.
                _activePointerId = int.MinValue;
                WorldExplorePanel.OnJoystickRelease();
            }
        }
    }
}
