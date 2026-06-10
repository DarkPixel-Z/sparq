using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.Systems
{
    /// <summary>
    /// Real combat scene. Tap-to-attack turn-based fight.
    /// Karu vs an enemy chosen by trial type. Victory drops XP + gold.
    /// </summary>
    public static class BattleScene
    {
        // Palette
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color HP_GREEN   = new Color(0.45f, 0.85f, 0.45f);
        private static readonly Color HP_RED     = new Color(0.85f, 0.35f, 0.40f);
        private static readonly Color BG_DARK    = new Color(0.04f, 0.03f, 0.10f, 0.96f);

        // Enemy options keyed by trial title
        private static readonly (string title, string sprite, int hp, int dmg, int xp, int gold)[] ENEMIES = new[]
        {
            ("Forest Goblin",  "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brawler/Brigading-Brawler.png", 60, 8, 30, 25),
            ("Shadow Wolf",    "Assets/2D Fantasy Monster Sprite Pack/Monsters/Hellhound/Darkness-Hellhound.png", 100, 12, 50, 50),
            ("Mind Phantom",   "Assets/2D Fantasy Monster Sprite Pack/Monsters/Spectre/Nightmare-Spectre.png", 50, 6, 25, 20),
            ("Stone Brute",    "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brute/Shadow-Brute.png", 80, 10, 35, 30),
        };

        // ───────── Player state ─────────
        private static int _playerHP, _playerMaxHP, _playerDmg;
        private static int _enemyHP, _enemyMaxHP, _enemyDmg;
        private static int _xpReward, _goldReward;
        private static string _enemyName;
        // ───────── Biome backdrop config ─────────
        private struct Biome
        {
            public string bgPath;
            public Color  bgTint;
            public Color  vignette;
            public string groundTile;
            public Color  groundTint;
            public string[] treeAssets;
            public Color  treeTint;
            public Color  fireflyColor;
        }

        private static Biome BiomeFor(string enemyTitle)
        {
            const string FW = "Assets/Fantasy World 2D/Sprites/PNG/";
            const string LL = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/";
            string t = (enemyTitle ?? "").ToLower();

            if (t.Contains("wolf") || t.Contains("hound"))
                return new Biome {
                    bgPath = LL + "Background_05.png",
                    bgTint = new Color(0.45f, 0.40f, 0.55f),
                    vignette = new Color(0.05f, 0.0f, 0.10f, 0.55f),
                    groundTile = FW + "tiles/tile_earth_1/tile_earth_1_1.png",
                    groundTint = new Color(0.42f, 0.38f, 0.45f),
                    treeAssets = new[]{ FW + "decor/cartoon_world_tree_3.png", FW + "decor/cartoon_world_tree_7.png", FW + "decor/cartoon_world_tree_11.png", FW + "decor/cartoon_world_tree_17.png", FW + "decor/cartoon_world_tree_21.png", FW + "decor/cartoon_world_tree_4.png" },
                    treeTint = new Color(0.45f, 0.45f, 0.55f),
                    fireflyColor = new Color(0.65f, 0.75f, 1f, 0.85f),
                };

            if (t.Contains("phantom") || t.Contains("spectre") || t.Contains("ghost"))
                return new Biome {
                    bgPath = LL + "Background_07.png",
                    bgTint = new Color(0.55f, 0.45f, 0.70f),
                    vignette = new Color(0.05f, 0.0f, 0.15f, 0.55f),
                    groundTile = FW + "tiles/tile_earth_1/tile_earth_1_1.png",
                    groundTint = new Color(0.40f, 0.35f, 0.50f),
                    treeAssets = new[]{ FW + "decor/cartoon_world_tree_22.png", FW + "decor/cartoon_world_tree_18.png", FW + "decor/cartoon_world_tree_14.png", FW + "decor/cartoon_world_tree_10.png", FW + "decor/cartoon_world_tree_6.png", FW + "decor/cartoon_world_tree_2.png" },
                    treeTint = new Color(0.55f, 0.45f, 0.65f),
                    fireflyColor = new Color(0.85f, 0.55f, 1f, 0.85f),
                };

            if (t.Contains("brute") || t.Contains("stone"))
                return new Biome {
                    bgPath = LL + "Background_02.png",
                    bgTint = new Color(0.75f, 0.62f, 0.50f),
                    vignette = new Color(0.10f, 0.05f, 0.0f, 0.45f),
                    groundTile = FW + "tiles/tile_earth_1/tile_earth_1_1.png",
                    groundTint = new Color(0.62f, 0.55f, 0.45f),
                    treeAssets = new[]{ FW + "decor/cartoon_world_stone_1.png", FW + "decor/cartoon_world_stone_3.png", FW + "decor/cartoon_world_tree_8.png", FW + "decor/cartoon_world_stone_5.png", FW + "decor/cartoon_world_tree_15.png", FW + "decor/cartoon_world_stone_2.png" },
                    treeTint = new Color(0.78f, 0.72f, 0.62f),
                    fireflyColor = new Color(1f, 0.78f, 0.45f, 0.85f),
                };

            // default: forest (Goblin + fallback)
            return new Biome {
                bgPath = LL + "Background_03.png",
                bgTint = new Color(0.65f, 0.55f, 0.70f),
                vignette = new Color(0, 0, 0, 0.45f),
                groundTile = FW + "tiles/tile_grass_1/tile_grass_1_1.png",
                groundTint = new Color(0.55f, 0.50f, 0.45f),
                treeAssets = new[]{ FW + "decor/cartoon_world_tree_1.png", FW + "decor/cartoon_world_tree_5.png", FW + "decor/cartoon_world_tree_8.png", FW + "decor/cartoon_world_tree_12.png", FW + "decor/cartoon_world_tree_15.png", FW + "decor/cartoon_world_tree_19.png" },
                treeTint = new Color(0.85f, 0.85f, 0.78f),
                fireflyColor = new Color(1f, 0.92f, 0.55f, 0.85f),
            };
        }
        private static GameObject _root;
        private static MonoBehaviour _runner;
        private static Slider _playerHpBar, _enemyHpBar;
        private static TMP_Text _playerHpText, _enemyHpText, _statusText;
        private static Button _attackBtn; // legacy ref (kept for safety)
        private static Button _strikeBtn, _powerBtn, _guardBtn, _fleeBtn;
        private static SpriteRenderer _enemySR;
        private static AudioSource _audio;
        private static Sparq.UI.HitFlash _playerFlash, _enemyFlash;
        private static RectTransform _playerRT, _enemyRT;
        private static Vector2 _playerBase, _enemyBase;

        // ───────── Focus + combo state ─────────
        private const int FOCUS_MAX = 3;
        private static int _focus;
        private static bool _guardActive;
        private static Image[] _focusPips;
        // combo
        private static bool _comboWindowOpen;
        private static int _comboStage;       // 0=none, 1=after strike, 2=after follow-up
        private static float _comboRingScale; // 1.0 → 0.2 over the window
        private static GameObject _comboRing;
        private static Coroutine _comboRoutine;

        // Real WAV clips loaded once
        private static AudioClip[] _swingClips;       // Barbarian attacks 1-5
        private static AudioClip[] _gruntClips;       // Barbarian grunts 1-6
        private static AudioClip _swordClip;          // Dark Knight sword
        private static AudioClip _hitClip;            // BarbarianHit
        private static AudioClip _thunderClip;        // Thunder (crits)
        private static AudioClip _painClip;           // Karu pain
        private static AudioClip _explosionClip;      // Defeat

        private static void EnsureClips()
        {
            #if UNITY_EDITOR
            if (_swordClip == null)
                _swordClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Dark Knight/Sounds/sword.wav");
            if (_painClip == null)
                _painClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Dark Knight/Sounds/pain.wav");
            if (_explosionClip == null)
                _explosionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Dark Knight/Sounds/DeathExplosion.wav");
            if (_hitClip == null)
                _hitClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/FeelDemos/Barbarians/Sounds/FeelBarbarianHit.wav");
            if (_thunderClip == null)
                _thunderClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/FeelDemos/Barbarians/Sounds/FeelBarbarianThunder.wav");
            if (_swingClips == null)
            {
                _swingClips = new AudioClip[5];
                for (int i = 0; i < 5; i++)
                    _swingClips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                        $"Assets/Feel/FeelDemos/Barbarians/Sounds/FeelBarbariansAttack{i+1}.wav");
            }
            if (_gruntClips == null)
            {
                _gruntClips = new AudioClip[6];
                for (int i = 0; i < 6; i++)
                    _gruntClips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                        $"Assets/Feel/FeelDemos/Barbarians/Sounds/FeelBarbariansGrunt{i+1}.wav");
            }
            #endif
        }

        private static void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (_audio == null || clip == null) return;
            _audio.pitch = pitch;
            _audio.PlayOneShot(clip, volume);
            _audio.pitch = 1f;
        }

        private static AudioClip RandomFrom(AudioClip[] arr)
        {
            if (arr == null || arr.Length == 0) return null;
            for (int tries = 0; tries < 6; tries++)
            {
                var c = arr[Random.Range(0, arr.Length)];
                if (c != null) return c;
            }
            return null;
        }

        // Optional context — set by StageMapPanel before Start()
        public static int CurrentStageIdx = 0;
        public static int StageHpMul = 100, StageDmgMul = 100, StageXpReward = 0, StageGoldReward = 0;
        public static string StageOverrideName = null;

        public static void Start(string trialTitle = "Forest Goblin")
        {
            // Pick enemy
            var enemy = ENEMIES[0];
            foreach (var e in ENEMIES)
            {
                if (trialTitle != null && trialTitle.ToLower().Contains(e.title.ToLower().Split(' ')[0].ToLower()))
                { enemy = e; break; }
            }

            // Base stats + equipment bonuses + active pet bonuses
            var (gearAtk, gearDef, gearHp) = EquipmentService.TotalStats();
            int petAtk = 0, petDef = 0, petHp = 0;
            try
            {
                var pet = Sparq.Systems.PetService.Active();
                if (pet != null) (petAtk, petDef, petHp) = Sparq.Systems.PetService.StatsOf(pet);
            } catch {}
            _playerMaxHP = 100 + gearHp + petHp;
            _playerHP = _playerMaxHP;
            _playerDmg = 14 + gearAtk + petAtk;

            // Apply stage difficulty multipliers if launched from map
            int hpMul  = CurrentStageIdx > 0 ? StageHpMul  : 100;
            int dmgMul = CurrentStageIdx > 0 ? StageDmgMul : 100;

            _enemyMaxHP = Mathf.RoundToInt(enemy.hp * hpMul / 100f);
            _enemyHP = _enemyMaxHP;
            _enemyDmg = Mathf.Max(2, Mathf.RoundToInt(enemy.dmg * dmgMul / 100f) - Mathf.RoundToInt((gearDef + petDef) * 0.5f));

            _xpReward  = CurrentStageIdx > 0 ? StageXpReward  : enemy.xp;
            _goldReward= CurrentStageIdx > 0 ? StageGoldReward : enemy.gold;
            _enemyName = !string.IsNullOrEmpty(StageOverrideName) ? StageOverrideName : enemy.title;

            BuildUI(enemy.sprite);
        }

        // ───────── UI build ─────────
        private static void BuildUI(string enemySpritePath)
        {
            EnsureClips();

            // Tear down any prior scene
            var prev = GameObject.Find("BattleScene");
            if (prev != null) Object.Destroy(prev);

            _root = new GameObject("BattleScene",
                typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(GraphicRaycaster), typeof(AudioSource), typeof(Sparq.UI.ScreenShake));
            _audio = _root.GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.volume = 0.85f;
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14500;
            var cs = _root.GetComponent<UnityEngine.UI.CanvasScaler>();
            cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // BG — fully opaque battle environment so home doesn't bleed through
            var bg = MakeImage(_root.transform, "BG", new Color(0.04f, 0.03f, 0.10f, 1f));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
            var biome = BiomeFor(_enemyName);
            #if UNITY_EDITOR
            // Prefer the BattleOfHeroes Top-Down ground (matches Maleficus
            // adventure look), falling back to Layer Lab demo backgrounds.
            string[] bgCandidates = {
                "Assets/BattleOfHeroes/Backgrounds/PNG/Top-Down Simple Dry_Ground 01.png",
                "Assets/BattleOfHeroes/Backgrounds/PNG/Top-Down Simple Dry_Ground 03.png",
                "Assets/BattleOfHeroes/Backgrounds/PNG/Top-Down Simple Dry_Ground 05.png",
                biome.bgPath,
                "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_03.png",
            };
            Sprite bgSp = null;
            foreach (var bgp in bgCandidates)
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(bgp) as UnityEditor.TextureImporter;
                if (imp != null && !Application.isPlaying &&
                    imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                    imp.SaveAndReimport();
                }
                bgSp = Sparq.Core.SpriteLoader.Load(bgp);
                if (bgSp != null) break;
            }
            if (bgSp != null)
            {
                bg.GetComponent<Image>().sprite = bgSp;
                bg.GetComponent<Image>().color = Color.white;       // BoH bg is full-color, don't tint
                bg.GetComponent<Image>().preserveAspect = false;
            }
            #endif

            // Animated forest backdrop — Fantasy World 2D trees swaying in the wind
            BuildForestBackdrop(_root.transform, biome);

            // Dark vignette over bg
            var bgVig = MakeImage(_root.transform, "BgVig", biome.vignette);
            var bvrt = bgVig.GetComponent<RectTransform>();
            bvrt.anchorMin = Vector2.zero; bvrt.anchorMax = Vector2.one;
            bvrt.offsetMin = Vector2.zero; bvrt.offsetMax = Vector2.zero;
            bgVig.GetComponent<Image>().raycastTarget = false;

            // Floating ambient fireflies above the vignette
            BuildFireflies(_root.transform, 14, biome.fireflyColor);

            // ── Header banner — fantasy flag ──
            var titleBanner = MakeImage(_root.transform, "TitleBanner", new Color(0.55f, 0.30f, 0.15f, 1f));
            var tbRT = titleBanner.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.5f, 1); tbRT.anchorMax = new Vector2(0.5f, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = new Vector2(0, -10);
            tbRT.sizeDelta = new Vector2(440, 88);
            #if UNITY_EDITOR
            const string FLAG_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/Label_Flag_01_Bg.png";
            var fimp = UnityEditor.AssetImporter.GetAtPath(FLAG_PATH) as UnityEditor.TextureImporter;
            if (fimp != null && !Application.isPlaying)
            {
                bool changed = false;
                if (fimp.textureType != UnityEditor.TextureImporterType.Sprite)
                { fimp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!fimp.alphaIsTransparency)
                { fimp.alphaIsTransparency = true; changed = true; }
                var s = new UnityEditor.TextureImporterSettings();
                fimp.ReadTextureSettings(s);
                if (s.spriteBorder == Vector4.zero)
                { s.spriteBorder = new Vector4(60, 30, 60, 30); fimp.SetTextureSettings(s); changed = true; }
                if (changed) fimp.SaveAndReimport();
            }
            var flagSp = Sparq.Core.SpriteLoader.Load(FLAG_PATH);
            if (flagSp != null)
            {
                var bImg = titleBanner.GetComponent<Image>();
                bImg.sprite = flagSp;
                bImg.type = (flagSp.border == Vector4.zero) ? Image.Type.Simple : Image.Type.Sliced;
                // Deep plum tint so the gold-yellow title pops with high contrast
                bImg.color = new Color(0.30f, 0.10f, 0.40f, 1f);
                bImg.raycastTarget = false;
            }
            #endif

            // Title text on the banner
            var bannerTitle = MakeText(titleBanner.transform, "Title", _enemyName.ToUpper(),
                32, FontStyles.Bold, new Color(1f, 0.95f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bannerTitle.alignment = TextAlignmentOptions.Center;
            bannerTitle.outlineWidth = 0.34f;
            bannerTitle.outlineColor = new Color(0.30f, 0.10f, 0.05f, 1f);
            bannerTitle.characterSpacing = 4f;

            // Karu HP bar (left)
            BuildHpBar(_root.transform, "PlayerHp", "Karu  Lv " + (Sparq.Core.SaveService.Data?.level ?? 1),
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, -110), new Vector2(420, 90),
                HP_GREEN, out _playerHpBar, out _playerHpText);

            // Enemy HP bar (right)
            BuildHpBar(_root.transform, "EnemyHp", _enemyName,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, -110), new Vector2(420, 90),
                HP_RED, out _enemyHpBar, out _enemyHpText);

            // Karu sprite (left, mid-bottom area) — use existing scene Karu's sprite
            var karuSrc = GameObject.Find("Karu");
            if (karuSrc != null)
            {
                var karuClone = new GameObject("PlayerSprite", typeof(RectTransform), typeof(Image));
                karuClone.transform.SetParent(_root.transform, false);
                var kRT = karuClone.GetComponent<RectTransform>();
                kRT.anchorMin = new Vector2(0, 0); kRT.anchorMax = new Vector2(0, 0);
                kRT.pivot = new Vector2(0, 0);
                kRT.anchoredPosition = new Vector2(80, 360);
                kRT.sizeDelta = new Vector2(440, 600);
                var kImg = karuClone.GetComponent<Image>();
                var karuSR = karuSrc.GetComponent<SpriteRenderer>();
                if (karuSR != null && karuSR.sprite != null)
                {
                    kImg.sprite = karuSR.sprite;
                    kImg.preserveAspect = true;
                }
                else kImg.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.6f);
                _playerFlash = karuClone.AddComponent<Sparq.UI.HitFlash>();
                _playerRT = kRT;
                _playerBase = kRT.anchoredPosition;
            }

            // ── Active pet sprite — fights alongside Karu ──
            BuildPetCompanion();

            // Enemy sprite (right)
            EnsureSprite(enemySpritePath);
            var enemyGO = new GameObject("EnemySprite", typeof(RectTransform), typeof(Image));
            enemyGO.transform.SetParent(_root.transform, false);
            var eRT = enemyGO.GetComponent<RectTransform>();
            eRT.anchorMin = new Vector2(1, 0); eRT.anchorMax = new Vector2(1, 0);
            eRT.pivot = new Vector2(1, 0);
            eRT.anchoredPosition = new Vector2(-80, 360);
            eRT.sizeDelta = new Vector2(440, 600);
            var eImg = enemyGO.GetComponent<Image>();
            var enemySprite = AssetDatabase_LoadAssetAtPath(enemySpritePath);
            if (enemySprite != null)
            {
                eImg.sprite = enemySprite;
                eImg.preserveAspect = true;
            }
            else eImg.color = new Color(HP_RED.r, HP_RED.g, HP_RED.b, 0.6f);
            _enemyFlash = enemyGO.AddComponent<Sparq.UI.HitFlash>();
            _enemyRT = eRT;
            _enemyBase = eRT.anchoredPosition;

            // Status text
            _statusText = MakeText(_root.transform, "Status", "Strike to open a combo — tap when the ring is small for PERFECT.",
                26, FontStyles.Bold, CREAM,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 280), new Vector2(900, 40));
            _statusText.alignment = TextAlignmentOptions.Center;

            // ── Action bar — 3 buttons across the bottom ──
            _strikeBtn = MakeBtn(_root.transform, "StrikeBtn", "STRIKE",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-280, 110), new Vector2(240, 130),
                Color.white, CREAM, 30);
            _strikeBtn.onClick.AddListener(OnStrike);
            ApplyButtonSkin(_strikeBtn, "Yellow");
            StyleButtonLabel(_strikeBtn, new Color(0.04f, 0.06f, 0.20f), new Color(1f, 0.95f, 0.7f, 1f), 0.22f);
            AddSkillIcon(_strikeBtn, "ItemIcon_Skill_Attack");

            _powerBtn = MakeBtn(_root.transform, "PowerBtn", "POWER\n<size=18>1 Focus</size>",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(240, 130),
                Color.white, CREAM, 28);
            _powerBtn.onClick.AddListener(OnPowerStrike);
            ApplyButtonSkin(_powerBtn, "Red");
            StyleButtonLabel(_powerBtn, new Color(0.04f, 0.06f, 0.20f), new Color(1f, 0.92f, 0.78f, 1f), 0.28f);
            AddSkillIcon(_powerBtn, "ItemIcon_Skill_Critical");

            _guardBtn = MakeBtn(_root.transform, "GuardBtn", "GUARD\n<size=18>+1 Focus</size>",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(280, 110), new Vector2(240, 130),
                Color.white, CREAM, 28);
            _guardBtn.onClick.AddListener(OnGuard);
            ApplyButtonSkin(_guardBtn, "Sky");
            StyleButtonLabel(_guardBtn, new Color(0.02f, 0.04f, 0.18f), new Color(1f, 1f, 1f, 1f), 0.25f);
            AddSkillIcon(_guardBtn, "ItemIcon_Skill_Defense");

            _attackBtn = _strikeBtn; // keep legacy var pointed at primary

            // (Back/Flee button removed — battle commits to the result)
            _fleeBtn = null;

            // ── Focus pips under Karu HP bar ──
            BuildFocusPips(_root.transform);

            _focus = 0;
            _guardActive = false;
            UpdateBars();
            UpdateFocusPips();
            UpdateActionButtons();
        }

        private static void BuildHpBar(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color fillColor, out Slider slider, out TMP_Text valText)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;

            // Portrait disc on the left (or right for enemy) — adds visual punch
            bool isEnemy = name == "EnemyHp";
            var disc = MakeImage(go.transform, "Disc", GOLD);
            disc.GetComponent<Image>().sprite = LoadCircleSpriteForBattle();
            var drt = disc.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(isEnemy ? 1 : 0, 0.5f);
            drt.anchorMax = new Vector2(isEnemy ? 1 : 0, 0.5f);
            drt.pivot = new Vector2(isEnemy ? 1 : 0, 0.5f);
            drt.anchoredPosition = new Vector2(isEnemy ? 0 : 0, 0);
            drt.sizeDelta = new Vector2(76, 76);
            // Inner colored disc (matches HP color)
            var innerDisc = MakeImage(disc.transform, "Inner", fillColor);
            innerDisc.GetComponent<Image>().sprite = LoadCircleSpriteForBattle();
            var idrt = innerDisc.GetComponent<RectTransform>();
            idrt.anchorMin = Vector2.zero; idrt.anchorMax = Vector2.one;
            idrt.offsetMin = new Vector2(6, 6); idrt.offsetMax = new Vector2(-6, -6);
            // First letter inside disc
            var letter = MakeText(disc.transform, "Letter",
                isEnemy && label.Length > 0 ? label.Substring(0, 1).ToUpper()
                                            : label.Substring(0, System.Math.Min(1, label.Length)).ToUpper(),
                36, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            letter.alignment = TextAlignmentOptions.Center;
            letter.outlineWidth = 0.30f;
            letter.outlineColor = new Color(1f, 0.95f, 0.7f);

            // Label (name + level) — bigger + outlined, offset to clear the portrait disc
            var lblTm = MakeText(go.transform, "Lbl", label,
                26, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lblTm.alignment = isEnemy ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
            lblTm.outlineWidth = 0.30f;
            lblTm.outlineColor = new Color(0.10f, 0.05f, 0.18f, 1f);
            var lblRT = lblTm.rectTransform;
            lblRT.anchorMin = new Vector2(0, 1); lblRT.anchorMax = new Vector2(1, 1);
            lblRT.pivot = new Vector2(0.5f, 1);
            lblRT.anchoredPosition = new Vector2(0, -8);
            lblRT.sizeDelta = new Vector2(isEnemy ? -100 : -100, 30);
            lblRT.offsetMin = new Vector2(isEnemy ? 12 : 90, lblRT.offsetMin.y);
            lblRT.offsetMax = new Vector2(isEnemy ? -90 : -12, lblRT.offsetMax.y);

            // Outer glow shadow under bar
            var glow = MakeImage(go.transform, "Glow", new Color(fillColor.r, fillColor.g, fillColor.b, 0.35f));
            glow.GetComponent<Image>().sprite = LoadCircleSpriteForBattle();
            var glRT = glow.GetComponent<RectTransform>();
            glRT.anchorMin = new Vector2(0, 0); glRT.anchorMax = new Vector2(1, 0);
            glRT.pivot = new Vector2(0.5f, 0);
            glRT.offsetMin = new Vector2(isEnemy ? -6 : 78, 4);
            glRT.offsetMax = new Vector2(isEnemy ? -78 : 6, 50);
            glow.GetComponent<Image>().raycastTarget = false;

            // Bar bg — thicker, with margin for the portrait
            var bg = MakeImage(go.transform, "Bg", new Color(0.08f, 0.05f, 0.18f, 0.95f));
            var bgImg = bg.GetComponent<Image>();
            #if UNITY_EDITOR
            const string SBAR_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/UI_Etc/StatusBar_Bg_Rectangle_01.png";
            var sbarImp = UnityEditor.AssetImporter.GetAtPath(SBAR_PATH) as UnityEditor.TextureImporter;
            if (sbarImp != null && sbarImp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
            {
                sbarImp.textureType = UnityEditor.TextureImporterType.Sprite;
                sbarImp.alphaIsTransparency = true;
                var settings = new UnityEditor.TextureImporterSettings();
                sbarImp.ReadTextureSettings(settings);
                if (settings.spriteBorder == Vector4.zero)
                {
                    settings.spriteBorder = new Vector4(20, 12, 20, 12);
                    sbarImp.SetTextureSettings(settings);
                }
                sbarImp.SaveAndReimport();
            }
            var sbarSp = Sparq.Core.SpriteLoader.Load(SBAR_PATH);
            if (sbarSp != null)
            {
                bgImg.sprite = sbarSp;
                bgImg.type = (sbarSp.border == Vector4.zero) ? Image.Type.Simple : Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            #endif
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.offsetMin = new Vector2(isEnemy ? 0 : 84, 8);
            brt.offsetMax = new Vector2(isEnemy ? -84 : 0, 50);

            // Slider
            slider = bg.AddComponent<Slider>();
            var fillArea = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillArea.transform.SetParent(bg.transform, false);
            var frt = fillArea.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3, 3); frt.offsetMax = new Vector2(-3, -3);
            var fillImg = fillArea.GetComponent<Image>();
            fillImg.color = fillColor;
            slider.fillRect = fillArea.GetComponent<RectTransform>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
            slider.interactable = false;

            // Value text — properly centered on the bar
            valText = MakeText(bg.transform, "Val", "100/100",
                20, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            valText.alignment = TextAlignmentOptions.Center;
            valText.outlineWidth = 0.32f;
            valText.outlineColor = new Color(0, 0, 0, 0.95f);
        }

        // ───────── Combat ─────────
        // Tier B combo: Strike → opens timing window → tap again to follow up.
        // Inner ring (≤0.5 scale) = Perfect (1.4× dmg + crit boost)
        // Outer ring (≤0.85)      = Good (1.0× dmg)
        // Miss / window expires   = combo ends, enemy counters

        private static void OnStrike()
        {
            if (_comboWindowOpen) { ResolveComboTap(); return; }
            DoStrike(damageMul: 1f, critBonus: 0f, label: "Strike");
            // Open combo window only if enemy still alive
            if (_enemyHP > 0) StartComboWindow();
            else AfterPlayerAction(immediate: true);
        }

        private static void OnPowerStrike()
        {
            if (_focus < 1) { Flash(_statusText, "Need 1 Focus!"); return; }
            _focus--;
            UpdateFocusPips();
            // Big slash arc VFX over enemy + thunder + lightning frames from BattleOfHeroes
            SpawnSlashArc();
            SpawnLightningSpellVFX();
            PlayClip(_thunderClip, 0.55f, 1.25f);
            DoStrike(damageMul: 1.7f, critBonus: 0.40f, label: "POWER STRIKE");
            AfterPlayerAction(immediate: false);
        }

        // BattleOfHeroes Lightning Spell — plays 11 frames over the enemy on power strike
        private static void SpawnLightningSpellVFX()
        {
            #if UNITY_EDITOR
            // Lazy-load the 11 lightning frames once
            if (_lightningFrames == null)
            {
                var list = new System.Collections.Generic.List<Sprite>();
                for (int i = 1; i <= 11; i++)
                {
                    string p = $"Assets/BattleOfHeroes/Animations/Lightning Spell/PNG/Lightning Spell_Frame_{i:00}.png";
                    var imp = UnityEditor.AssetImporter.GetAtPath(p) as UnityEditor.TextureImporter;
                    if (imp != null && !Application.isPlaying &&
                        imp.textureType != UnityEditor.TextureImporterType.Sprite)
                    {
                        imp.textureType = UnityEditor.TextureImporterType.Sprite;
                        imp.alphaIsTransparency = true;
                        imp.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                        imp.SaveAndReimport();
                    }
                    var fSp = Sparq.Core.SpriteLoader.Load(p);
                    if (fSp != null) list.Add(fSp);
                }
                _lightningFrames = list.ToArray();
            }
            if (_lightningFrames.Length == 0) return;

            // Spawn over the enemy position (top-center area of battle scene)
            var go = new GameObject("LightningVFX",
                typeof(RectTransform), typeof(UnityEngine.UI.Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -250);
            rt.sizeDelta = new Vector2(420, 420);
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(PlayVFXFrames(img, _lightningFrames, 0.05f));
            #endif
        }

        private static Sprite[] _lightningFrames;
        private static System.Collections.IEnumerator PlayVFXFrames(
            UnityEngine.UI.Image img, Sprite[] frames, float frameTime)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (img == null) yield break;
                img.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
            if (img != null) UnityEngine.Object.Destroy(img.gameObject);
        }

        private static void OnGuard()
        {
            _guardActive = true;
            _focus = Mathf.Min(FOCUS_MAX, _focus + 1);
            UpdateFocusPips();
            _statusText.text = "Guard up — next hit halved.";
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            SpawnShieldBubble();
            UpdateActionButtons();
            // Enemy still gets to attack after guard, but guard absorbs half
            EnsureRunner();
            _runner.StartCoroutine(EnemyTurn());
        }

        private static void DoStrike(float damageMul, float critBonus, string label)
        {
            PlayClip(_swordClip, 0.9f, damageMul > 1.2f ? 0.85f : 1.0f);

            float critChance = 0.15f + critBonus;
            bool crit = Random.value < critChance;
            int dmg = Mathf.RoundToInt((_playerDmg + Random.Range(-3, 4)) * damageMul);
            if (crit) dmg = Mathf.RoundToInt(dmg * 1.8f);

            _enemyHP = Mathf.Max(0, _enemyHP - dmg);
            UpdateBars();

            EnsureRunner();
            _runner.StartCoroutine(DelayedClip(0.08f, crit ? _thunderClip : _hitClip, crit ? 0.85f : 0.9f, 1f));

            // Visible sword swing crashing into the enemy
            SpawnSwordSwing(crit, power: damageMul > 1.2f);

            _statusText.text = crit ? $"CRITICAL {label}! -{dmg}" : $"{label} -{dmg}";
            FloatText(_root.transform, new Vector3(700, 600, 0),
                crit ? $"CRIT -{dmg}" : $"-{dmg}",
                crit ? GOLD : HP_RED);

            // Karu lunge — bigger for power strikes
            float lungeDist = damageMul > 1.2f ? 130f : 80f;
            float lungeDur  = damageMul > 1.2f ? 0.30f : 0.22f;
            if (_playerRT != null)
                _runner.StartCoroutine(LungeWithPunch(_playerRT, _playerBase, Vector2.right, lungeDist, lungeDur));

            // Pet pounces too
            if (_petRT != null) _runner.StartCoroutine(PetAttackBounce());

            if (_enemyFlash != null)
                _enemyFlash.Flash(crit ? Color.white : new Color(1f, 0.4f, 0.4f),
                                  crit ? 0.25f : 0.15f, crit ? 1.20f : 1.10f);
            if (_enemyRT != null)
                SparkBurst(_enemyRT.anchoredPosition + new Vector2(-100, 100),
                           crit ? GOLD : new Color(1f, 0.5f, 0.3f),
                           crit ? 18 : 10);
            Sparq.UI.ScreenShake.Shake(crit ? 22f : (damageMul > 1.2f ? 18f : 10f),
                                       crit ? 0.28f : 0.16f);

            // Earn 1 focus on each successful hit (capped)
            if (damageMul <= 1.0f) // basic strikes build focus, power strike already spent
            {
                _focus = Mathf.Min(FOCUS_MAX, _focus + 1);
                UpdateFocusPips();
            }

            if (_enemyHP <= 0) { Victory(); return; }
        }

        private static void AfterPlayerAction(bool immediate)
        {
            if (_enemyHP <= 0) return;
            EnsureRunner();
            _runner.StartCoroutine(EnemyTurn());
        }

        // ── Combo window (Tier B) ──
        private static void StartComboWindow()
        {
            CloseComboWindow();
            _comboStage = 1;
            _comboWindowOpen = true;
            UpdateActionButtons();
            EnsureRunner();
            _comboRoutine = _runner.StartCoroutine(ComboWindowCo(0.85f));
        }

        private static IEnumerator ComboWindowCo(float duration)
        {
            // Build the shrinking ring at the enemy
            if (_comboRing != null) Object.Destroy(_comboRing);
            _comboRing = new GameObject("ComboRing", typeof(RectTransform), typeof(Image));
            _comboRing.transform.SetParent(_root.transform, false);
            var rt = _comboRing.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            // anchor over enemy (right side)
            Vector2 enemyCenter = _enemyRT != null
                ? _enemyRT.anchoredPosition + new Vector2(-220, 320)
                : new Vector2(-300, 700);
            rt.anchoredPosition = enemyCenter;
            rt.sizeDelta = new Vector2(280, 280);
            var img = _comboRing.GetComponent<Image>();
            img.sprite = LoadRingSpriteForBattle();
            img.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f);
            img.raycastTarget = false;

            // Inner zone marker (the bullseye)
            var inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            inner.transform.SetParent(_comboRing.transform, false);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f); irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(110, 110);
            var iimg = inner.GetComponent<Image>();
            iimg.sprite = LoadRingSpriteForBattle();
            iimg.color = new Color(1f, 0.95f, 0.6f, 0.55f);
            iimg.raycastTarget = false;

            float t = 0f;
            while (t < duration && _comboWindowOpen)
            {
                t += Time.deltaTime;
                float k = t / duration; // 0→1
                _comboRingScale = Mathf.Lerp(1.4f, 0.18f, k);
                rt.localScale = Vector3.one * _comboRingScale;
                // pulse alpha
                var c = img.color; c.a = 0.55f + 0.35f * Mathf.Sin(t * 14f); img.color = c;
                yield return null;
            }

            // Window expired
            if (_comboWindowOpen) ResolveComboMiss();
        }

        private static void ResolveComboTap()
        {
            // Evaluate ring scale: lower = better
            float s = _comboRingScale;
            CloseComboWindow();

            if (s <= 0.55f) // Perfect zone
            {
                _statusText.text = "PERFECT!";
                DoStrike(damageMul: 1.4f, critBonus: 0.20f, label: "PERFECT");
                int multiplier = _comboStage + 1; // x2, x3
                SpawnComboBurst($"x{multiplier}  PERFECT!", GOLD);
                if (_comboStage < 2 && _enemyHP > 0)
                {
                    _comboStage++;
                    StartComboWindow();
                    return;
                }
            }
            else if (s <= 0.95f) // Good zone
            {
                _statusText.text = "Good combo!";
                DoStrike(damageMul: 1.0f, critBonus: 0.05f, label: "Combo");
                int multiplier = _comboStage + 1;
                SpawnComboBurst($"x{multiplier}  COMBO!", new Color(0.55f, 0.85f, 0.45f));
                if (_comboStage < 2 && _enemyHP > 0)
                {
                    _comboStage++;
                    StartComboWindow();
                    return;
                }
            }
            else
            {
                ResolveComboMiss();
                return;
            }

            // Combo finished naturally (max chain or enemy down)
            AfterPlayerAction(immediate: false);
        }

        private static void ResolveComboMiss()
        {
            CloseComboWindow();
            _statusText.text = "Whiff!";
            // Enemy gets a slightly faster counter as punishment
            EnsureRunner();
            _runner.StartCoroutine(EnemyTurn());
        }

        private static void CloseComboWindow()
        {
            _comboWindowOpen = false;
            if (_comboRing != null) { Object.Destroy(_comboRing); _comboRing = null; }
            if (_comboRoutine != null && _runner != null) { _runner.StopCoroutine(_comboRoutine); _comboRoutine = null; }
            UpdateActionButtons();
        }

        // ── Karu skill VFX (Tier D) ──
        private static IEnumerator LungeWithPunch(RectTransform rt, Vector2 baseP, Vector2 dir, float distance, float duration)
        {
            if (rt == null) yield break;
            float half = duration * 0.5f;
            float t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                rt.anchoredPosition = Vector2.Lerp(baseP, baseP + dir * distance, k);
                rt.localScale = Vector3.one * (1f + 0.12f * k);
                yield return null;
            }
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                rt.anchoredPosition = Vector2.Lerp(baseP + dir * distance, baseP, k);
                rt.localScale = Vector3.one * (1.12f - 0.12f * k);
                yield return null;
            }
            rt.anchoredPosition = baseP;
            rt.localScale = Vector3.one;
        }

        private static void SpawnSlashArc()
        {
            if (_root == null || _enemyRT == null) return;
            var go = new GameObject("Slash", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = _enemyRT.anchoredPosition + new Vector2(-220, 320);
            rt.sizeDelta = new Vector2(360, 80);
            rt.localRotation = Quaternion.Euler(0, 0, -28f);
            var img = go.GetComponent<Image>();
            img.sprite = LoadSlashSpriteForBattle();
            img.color = new Color(1f, 0.95f, 0.55f, 0.95f);
            img.raycastTarget = false;
            EnsureRunner();
            _runner.StartCoroutine(SlashLife(rt, img));
        }

        private static IEnumerator SlashLife(RectTransform rt, Image img)
        {
            float t = 0f, dur = 0.32f;
            Vector3 from = new Vector3(0.4f, 0.4f, 1f);
            Vector3 to   = new Vector3(1.5f, 1.1f, 1f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                rt.localScale = Vector3.Lerp(from, to, k);
                var c = img.color; c.a = 0.95f * (1f - k); img.color = c;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void SpawnShieldBubble()
        {
            if (_root == null || _playerRT == null) return;
            var go = new GameObject("Shield", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = _playerRT.anchoredPosition + new Vector2(220, 300);
            rt.sizeDelta = new Vector2(420, 420);
            var img = go.GetComponent<Image>();
            img.sprite = LoadCircleSpriteForBattle();
            img.color = new Color(0.45f, 0.75f, 1f, 0.55f);
            img.raycastTarget = false;
            EnsureRunner();
            _runner.StartCoroutine(ShieldLife(rt, img));
        }

        private static IEnumerator ShieldLife(RectTransform rt, Image img)
        {
            float t = 0f, dur = 0.7f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, k);
                var c = img.color; c.a = 0.55f * (1f - k); img.color = c;
                yield return null;
            }
            // Persist a faint ring while guard is active
            if (rt == null) yield break;
            while (_guardActive && rt != null)
            {
                rt.localScale = Vector3.one * (1.10f + 0.04f * Mathf.Sin(Time.time * 5f));
                var c = img.color; c.a = 0.18f + 0.08f * Mathf.Sin(Time.time * 5f); img.color = c;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        // ── Focus pips ──
        private static void BuildFocusPips(Transform parent)
        {
            _focusPips = new Image[FOCUS_MAX];
            for (int i = 0; i < FOCUS_MAX; i++)
            {
                var go = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(75 + i * 36, -210);
                rt.sizeDelta = new Vector2(36, 36);
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSpriteForBattle();
                img.color = new Color(0.72f, 0.32f, 0.95f, 1f); // bright purple
                img.raycastTarget = false;
                _focusPips[i] = img;
            }
        }

        private static void UpdateFocusPips()
        {
            if (_focusPips == null) return;
            for (int i = 0; i < _focusPips.Length; i++)
            {
                if (_focusPips[i] == null) continue;
                _focusPips[i].color = i < _focus
                    ? new Color(1f, 0.92f, 0.35f, 1f)        // bright gold when lit
                    : new Color(0.72f, 0.32f, 0.95f, 1f);    // bright purple when dim
                _focusPips[i].rectTransform.localScale =
                    Vector3.one * (i < _focus ? 1.25f : 0.95f);
            }
            UpdateActionButtons();
        }

        private static void UpdateActionButtons()
        {
            if (_powerBtn != null) _powerBtn.interactable = (_focus >= 1) && !_comboWindowOpen;
            if (_guardBtn != null) _guardBtn.interactable = !_comboWindowOpen;
            if (_strikeBtn != null) _strikeBtn.interactable = true; // always tappable
        }

        private static void Flash(TMP_Text t, string msg)
        {
            if (t != null) t.text = msg;
        }

        // ── Button text styling — readable colour + outline ──
        private static void StyleButtonLabel(Button btn, Color faceColor, Color outlineColor, float outlineWidth)
        {
            if (btn == null) return;
            var lbl = btn.transform.Find("Lbl");
            if (lbl == null) return;
            var tm = lbl.GetComponent<TMP_Text>();
            if (tm == null) return;
            tm.color = faceColor;
            tm.fontStyle = FontStyles.Bold;
            tm.outlineWidth = outlineWidth;
            tm.outlineColor = outlineColor;
        }

        // ── Skill icon overlay for action buttons ──
        private static void AddSkillIcon(Button btn, string iconBaseName)
        {
            #if UNITY_EDITOR
            string path = $"Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/{iconBaseName}.png";
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
            {
                imp.textureType = UnityEditor.TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp == null) return;

            var icon = new GameObject("SkillIcon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(btn.transform, false);
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(12, 0);
            rt.sizeDelta = new Vector2(70, 70);
            var img = icon.GetComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // Shift the label right so it doesn't overlap the icon
            var lbl = btn.transform.Find("Lbl");
            if (lbl != null)
            {
                var lrt = lbl.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    lrt.offsetMin = new Vector2(76, lrt.offsetMin.y);
                    lrt.offsetMax = new Vector2(-6, lrt.offsetMax.y);
                }
            }
            #endif
        }

        // ── Layer Lab button skin ──
        private static readonly System.Collections.Generic.HashSet<string> _btnSpritePrepared = new System.Collections.Generic.HashSet<string>();

        private static void ApplyButtonSkin(Button btn, string color)
        {
            #if UNITY_EDITOR
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            string path = $"Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_01_Mian_l_Bg_{color}.png";

            // CRITICAL: Never SaveAndReimport in play mode — domain reload kills coroutines.
            if (!Application.isPlaying && !_btnSpritePrepared.Contains(path))
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null)
                {
                    bool changed = false;
                    if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
                    { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                    if (!imp.alphaIsTransparency)
                    { imp.alphaIsTransparency = true; changed = true; }
                    var settings = new UnityEditor.TextureImporterSettings();
                    imp.ReadTextureSettings(settings);
                    if (settings.spriteBorder == Vector4.zero)
                    {
                        settings.spriteBorder = new Vector4(40, 40, 40, 40);
                        imp.SetTextureSettings(settings);
                        changed = true;
                    }
                    if (changed) imp.SaveAndReimport();
                }
                _btnSpritePrepared.Add(path);
            }

            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = (sp.border == Vector4.zero) ? Image.Type.Simple : Image.Type.Sliced;
                img.color = Color.white;
                img.preserveAspect = false;
            }
            #endif
        }

        // ── Sword swing VFX (visible blade striking the enemy) ──
        private static void SpawnSwordSwing(bool crit, bool power)
        {
            if (_root == null || _enemyRT == null) return;
            var go = new GameObject("SwordSwing", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            // Anchor over the enemy
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0.15f); // pivot near hilt
            rt.anchoredPosition = _enemyRT.anchoredPosition + new Vector2(-220, 200);
            rt.sizeDelta = new Vector2(power ? 320 : 240, power ? 460 : 340);
            var img = go.GetComponent<Image>();
            img.sprite = LoadSwordSprite();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = crit ? new Color(1f, 0.95f, 0.55f) : (power ? new Color(1f, 0.85f, 0.55f) : Color.white);
            EnsureRunner();
            _runner.StartCoroutine(SwordSwingLife(rt, img, power));
        }

        private static IEnumerator SwordSwingLife(RectTransform rt, Image img, bool power)
        {
            float t = 0f, dur = power ? 0.30f : 0.22f;
            // Big overhead arc — start raised, swing down across the enemy
            float rotFrom = -130f;
            float rotTo   = 35f;
            Vector2 startOffset = new Vector2(60, 120);   // raised left of impact
            Vector2 endOffset   = new Vector2(-40, -20);  // hilt drops past the body
            Vector2 anchor = rt.anchoredPosition;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float e = 1f - (1f - k) * (1f - k); // ease-out quad
                if (rt == null) yield break;
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(rotFrom, rotTo, e));
                rt.localScale    = Vector3.one * Mathf.Lerp(0.8f, 1.15f, k);
                rt.anchoredPosition = anchor + Vector2.Lerp(startOffset, endOffset, e);
                yield return null;
            }
            // Linger briefly at impact, then fade out
            yield return new WaitForSeconds(0.06f);
            t = 0f; dur = 0.14f;
            Color c = img != null ? img.color : Color.white;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (img != null) { c.a = 1f - k; img.color = c; }
                if (rt != null)  rt.localScale = Vector3.one * (1.15f + 0.15f * k);
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static Sprite _swordSprite;
        private static Sprite LoadSwordSprite()
        {
            if (_swordSprite != null) return _swordSprite;
            #if UNITY_EDITOR
            _swordSprite = LoadSpriteForBattle("Assets/FantasyIconPack/256/SwordT1.png");
            #endif
            return _swordSprite;
        }

        // ── Procedural ring + slash sprites ──
        private static Sprite _ringSprite, _slashSprite;
        private static Sprite LoadRingSpriteForBattle()
        {
            if (_ringSprite != null) return _ringSprite;
            const int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float rOut = s * 0.48f, rIn = s * 0.40f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, (d <= rOut && d >= rIn) ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _ringSprite;
        }

        private static Sprite LoadSlashSpriteForBattle()
        {
            if (_slashSprite != null) return _slashSprite;
            const int w = 128, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // soft horizontal blade with feathered edges
                float dx = (x - w * 0.5f) / (w * 0.5f);
                float dy = (y - h * 0.5f) / (h * 0.5f);
                float a = Mathf.Clamp01(1f - (dx * dx) * 0.8f - (dy * dy) * 1.4f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            _slashSprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            return _slashSprite;
        }

        private static IEnumerator DelayedClip(float delay, AudioClip clip, float volume, float pitch)
        {
            yield return new WaitForSeconds(delay);
            PlayClip(clip, volume, pitch);
        }

        // ───────── Pet companion ─────────
        private static RectTransform _petRT;
        private static Vector2 _petBase;
        private static Sparq.UI.HitFlash _petFlash;
        private static TMP_Text _petLevelTm;

        private static void BuildPetCompanion()
        {
            #if UNITY_EDITOR
            var pet = Sparq.Systems.PetService.Active();
            if (pet == null) return;
            var sp = Sparq.Systems.PetService.FindSpecies(pet.speciesId);
            if (sp == null) return;

            // Auto-import pet sprite as Sprite type if needed
            var imp = UnityEditor.AssetImporter.GetAtPath(sp.spritePath) as UnityEditor.TextureImporter;
            if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
            {
                imp.textureType = UnityEditor.TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var psp = Sparq.Core.SpriteLoader.Load(sp.spritePath);
            if (psp == null) return;

            // Pet sprite — to the right of Karu, slightly behind/lower
            var petGO = new GameObject("PetCompanion", typeof(RectTransform), typeof(Image));
            petGO.transform.SetParent(_root.transform, false);
            var rt = petGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(360, 320);
            rt.sizeDelta = new Vector2(230, 230);
            var img = petGO.GetComponent<Image>();
            img.sprite = psp;
            img.preserveAspect = true;
            img.raycastTarget = false;
            _petFlash = petGO.AddComponent<Sparq.UI.HitFlash>();
            _petRT = rt;
            _petBase = rt.anchoredPosition;

            // Tiny pet name + level chip floating above the pet
            var chip = new GameObject("PetChip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(petGO.transform, false);
            var crt = chip.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1); crt.anchorMax = new Vector2(0.5f, 1);
            crt.pivot = new Vector2(0.5f, 0);
            crt.anchoredPosition = new Vector2(0, -10);
            crt.sizeDelta = new Vector2(180, 40);
            chip.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.22f, 0.9f);

            _petLevelTm = MakeText(chip.transform, "PT",
                $"{sp.name}  LV {pet.level}",
                20, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _petLevelTm.alignment = TextAlignmentOptions.Center;
            _petLevelTm.outlineWidth = 0.20f;
            _petLevelTm.outlineColor = new Color(0, 0, 0, 0.85f);

            // Idle bounce — gentle up/down so the pet looks alive
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(PetIdleBounce());
            #endif
        }

        private static IEnumerator PetIdleBounce()
        {
            while (_petRT != null)
            {
                float t = Time.time;
                float dy = Mathf.Sin(t * 2.6f) * 8f;
                _petRT.anchoredPosition = _petBase + new Vector2(0, dy);
                yield return null;
            }
        }

        private static IEnumerator PetAttackBounce()
        {
            if (_petRT == null) yield break;
            float t = 0f, dur = 0.30f;
            Vector2 dir = new Vector2(50, 30); // forward + up
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float bounce = Mathf.Sin(k * Mathf.PI);
                _petRT.anchoredPosition = _petBase + dir * bounce;
                _petRT.localScale = Vector3.one * (1f + 0.18f * bounce);
                yield return null;
            }
            _petRT.anchoredPosition = _petBase;
            _petRT.localScale = Vector3.one;
        }

        // ───────── Lunge animation ─────────
        private static IEnumerator Lunge(RectTransform rt, Vector2 baseP, Vector2 dir, float distance, float duration)
        {
            if (rt == null) yield break;
            float half = duration * 0.5f;
            float t = 0f;
            // Forward
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                rt.anchoredPosition = Vector2.Lerp(baseP, baseP + dir * distance, k);
                yield return null;
            }
            // Back
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                rt.anchoredPosition = Vector2.Lerp(baseP + dir * distance, baseP, k);
                yield return null;
            }
            rt.anchoredPosition = baseP;
        }

        // ───────── Combo burst (big "x2 PERFECT!" with confetti) ─────────
        private static void SpawnComboBurst(string text, Color color)
        {
            if (_root == null) return;
            // The big text
            var go = new GameObject("ComboBurst", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 380);
            rt.sizeDelta = new Vector2(900, 140);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = 88;
            tm.fontStyle = FontStyles.Bold;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.outlineWidth = 0.40f;
            tm.outlineColor = new Color(0.15f, 0.05f, 0.10f, 1f);
            tm.raycastTarget = false;
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(ComboBurstLife(rt, tm));

            // Confetti — 16 small colored circles bursting out
            Color[] confetti = {
                GOLD, new Color(0.92f, 0.20f, 0.50f), new Color(0.55f, 0.85f, 0.45f),
                new Color(0.30f, 0.55f, 0.85f), new Color(1f, 0.55f, 0.30f),
            };
            for (int i = 0; i < 18; i++)
            {
                var c = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                c.transform.SetParent(_root.transform, false);
                var crt = c.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(0, 380);
                crt.sizeDelta = new Vector2(20, 20);
                var img = c.GetComponent<Image>();
                img.sprite = LoadCircleSpriteForBattle();
                img.color = confetti[i % confetti.Length];
                img.raycastTarget = false;
                float ang = i * (360f / 18f);
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                if (_runner != null) _runner.StartCoroutine(SparkLife(crt, img, dir, 380f, 0.7f));
            }
        }

        private static IEnumerator ComboBurstLife(RectTransform rt, TMP_Text tm)
        {
            float t = 0f, dur = 0.85f;
            Vector3 from = new Vector3(0.4f, 0.4f, 1f);
            Vector3 peak = new Vector3(1.25f, 1.25f, 1f);
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                // Pop in (first 25%), hold, fade out
                float scale = k < 0.25f ? Mathf.Lerp(from.x, peak.x, k / 0.25f)
                            : k < 0.65f ? peak.x
                            : Mathf.Lerp(peak.x, 0.95f, (k - 0.65f) / 0.35f);
                rt.localScale = Vector3.one * scale;
                if (tm != null)
                {
                    var c = tm.color;
                    c.a = k < 0.65f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.65f) / 0.35f);
                    tm.color = c;
                }
                yield return null;
            }
            if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
        }

        // ───────── Loot cascade (drops from above on victory) ─────────
        private static void SpawnLootCascade(string lootName, Color rarityColor)
        {
            if (_root == null) return;
            // Outer rarity ring (pulses)
            var ring = new GameObject("LootRing", typeof(RectTransform), typeof(Image));
            ring.transform.SetParent(_root.transform, false);
            var rrt = ring.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.5f, 0.5f); rrt.anchorMax = new Vector2(0.5f, 0.5f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.anchoredPosition = new Vector2(0, 1200); // start offscreen above
            rrt.sizeDelta = new Vector2(280, 280);
            var ringImg = ring.GetComponent<Image>();
            ringImg.sprite = LoadCircleSpriteForBattle();
            ringImg.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.55f);
            ringImg.raycastTarget = false;

            // Inner card with the loot name
            var card = new GameObject("LootCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(ring.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(220, 220);
            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = LoadCircleSpriteForBattle();
            cardImg.color = new Color(rarityColor.r * 0.4f, rarityColor.g * 0.4f, rarityColor.b * 0.4f, 0.95f);
            cardImg.raycastTarget = false;

            // Loot label
            var lbl = new GameObject("Lbl", typeof(RectTransform));
            lbl.transform.SetParent(card.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(-30, -30); lrt.offsetMax = new Vector2(30, 30);
            var lblTm = lbl.AddComponent<TextMeshProUGUI>();
            lblTm.text = $"NEW LOOT!\n<size=42><color=#FFE9A8>{lootName}</color></size>";
            lblTm.richText = true;
            lblTm.fontSize = 24;
            lblTm.fontStyle = FontStyles.Bold;
            lblTm.color = Color.white;
            lblTm.alignment = TextAlignmentOptions.Center;
            lblTm.font = TMP_Settings.defaultFontAsset;
            lblTm.outlineWidth = 0.30f;
            lblTm.outlineColor = new Color(0.10f, 0.05f, 0.10f, 1f);
            lblTm.raycastTarget = false;

            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(LootCascadeLife(rrt, ringImg, rarityColor));
        }

        private static IEnumerator LootCascadeLife(RectTransform rt, Image ringImg, Color rarityColor)
        {
            // Phase 1: cascade from above to center (0.55s, ease-out)
            float t = 0f, dur = 0.55f;
            Vector2 from = new Vector2(0, 1200);
            Vector2 to   = new Vector2(0, 200);
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float ease = 1f - (1f - k) * (1f - k); // ease-out quad
                rt.anchoredPosition = Vector2.Lerp(from, to, ease);
                yield return null;
            }
            // Drop a particle trail of colored sparks from the cascade path
            for (int i = 0; i < 16; i++)
            {
                float ang = i * (360f / 16f);
                Vector2 dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                var go = new GameObject("LootSpark", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root.transform, false);
                var grt = go.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
                grt.pivot = new Vector2(0.5f, 0.5f);
                grt.anchoredPosition = new Vector2(0, 200);
                grt.sizeDelta = new Vector2(20, 20);
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSpriteForBattle();
                img.color = rarityColor;
                img.raycastTarget = false;
                if (_runner != null) _runner.StartCoroutine(SparkLife(grt, img, dir, 320f, 0.7f));
            }
            // Phase 2: pulse the ring for 0.9s
            t = 0f;
            float pulseDur = 0.9f;
            while (t < pulseDur && rt != null)
            {
                t += Time.deltaTime;
                float k = t / pulseDur;
                float pulse = 1f + Mathf.Sin(k * Mathf.PI * 4f) * 0.06f;
                rt.localScale = Vector3.one * pulse;
                yield return null;
            }
            // Phase 3: fade out
            t = 0f; dur = 0.4f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (ringImg != null) { var c = ringImg.color; c.a = (1f - k) * 0.55f; ringImg.color = c; }
                rt.localScale = Vector3.one * (1f + k * 0.2f);
                yield return null;
            }
            if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
        }

        // ───────── Spark burst at impact ─────────
        private static void SparkBurst(Vector2 atScreenPos, Color color, int count = 10)
        {
            if (_root == null) return;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = atScreenPos;
                rt.sizeDelta = new Vector2(14, 14);
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSpriteForBattle();
                img.color = color;
                img.raycastTarget = false;

                float angle = i * (360f / count);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad),
                                          Mathf.Sin(angle * Mathf.Deg2Rad));
                EnsureRunner();
                _runner.StartCoroutine(SparkLife(rt, img, dir, 200f, 0.5f));
            }
        }

        private static IEnumerator SparkLife(RectTransform rt, Image img, Vector2 dir, float distance, float duration)
        {
            Vector2 start = rt.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                rt.anchoredPosition = start + dir * distance * k;
                rt.localScale = Vector3.one * (1f - k * 0.7f);
                var c = img.color; c.a = 1f - k; img.color = c;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static Sprite _battleCircle;
        private static Sprite LoadCircleSpriteForBattle()
        {
            if (_battleCircle != null) return _battleCircle;
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.46f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _battleCircle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _battleCircle;
        }

        private static IEnumerator EnemyTurn()
        {
            SetActionButtons(false);
            yield return new WaitForSeconds(0.28f);

            // Enemy battle cry (random grunt) + attack swing
            PlayClip(RandomFrom(_gruntClips), 0.75f, 0.92f);
            yield return new WaitForSeconds(0.10f);
            PlayClip(RandomFrom(_swingClips), 0.85f, 0.9f);

            int damage = _enemyDmg + Random.Range(-2, 3);
            // Guard halves incoming damage and consumes
            if (_guardActive)
            {
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * 0.5f));
                _guardActive = false;
                FloatText(_root.transform, new Vector3(-700, 700, 0), "BLOCK!", new Color(0.5f, 0.85f, 1f));
            }
            _playerHP = Mathf.Max(0, _playerHP - damage);
            UpdateBars();

            yield return new WaitForSeconds(0.10f);
            // Karu pain on hit
            PlayClip(_painClip, 0.7f, 1.05f);

            _statusText.text = $"{_enemyName} strikes for {damage}!";
            FloatText(_root.transform, new Vector3(-700, 600, 0), $"-{damage}", HP_RED);

            // Enemy lunges left, Karu flashes + sparks burst at player
            if (_enemyRT != null) _runner.StartCoroutine(Lunge(_enemyRT, _enemyBase, Vector2.left, 80f, 0.22f));
            if (_playerFlash != null)
                _playerFlash.Flash(new Color(1f, 0.3f, 0.3f), 0.18f, 1.08f);
            if (_playerRT != null)
                SparkBurst(_playerRT.anchoredPosition + new Vector2(100, 100),
                           new Color(1f, 0.4f, 0.3f), 8);
            Sparq.UI.ScreenShake.Shake(8f, 0.14f);

            yield return new WaitForSeconds(0.18f);

            if (_playerHP <= 0) { Defeat(); yield break; }
            SetActionButtons(true);
            UpdateActionButtons();
            _statusText.text = "Strike, charge Power, or Guard.";
        }

        private static void SetActionButtons(bool on)
        {
            if (_strikeBtn != null) _strikeBtn.interactable = on;
            if (_powerBtn  != null) _powerBtn.interactable  = on && _focus >= 1;
            if (_guardBtn  != null) _guardBtn.interactable  = on;
        }

        private static void Victory()
        {
            // Loot drop
            int playerLvl = 1;
            try { playerLvl = Sparq.Core.SaveService.Data?.level ?? 1; } catch {}
            var loot = EquipmentService.RollLoot(playerLvl);
            EquipmentService.Grant(loot.id);

            // Report stage completion if launched from map
            if (CurrentStageIdx > 0)
            {
                float hpPct = (float)_playerHP / _playerMaxHP;
                StageService.RecordVictory(CurrentStageIdx, hpPct);
                CurrentStageIdx = 0; // reset
                StageOverrideName = null;
            }

            // Pet food drop — 60% chance after every victory
            string foodDropId = null;
            try
            {
                if (Random.value < 0.60f) foodDropId = Sparq.Systems.PetService.RollFoodDrop();
            } catch {}
            string foodLine = "";
            if (!string.IsNullOrEmpty(foodDropId))
            {
                var f = Sparq.Systems.PetService.FindFood(foodDropId);
                if (f != null)
                {
                    foodLine = $"\n🍓 {f.name} for the pet!";
                    FloatText(_root.transform, new Vector3(-200, 280, 0),
                        $"<color=#FF8E5C>+1 {f.name}</color>", new Color(1f, 0.55f, 0.30f));
                }
            }

            _statusText.text = $"VICTORY!  +{_xpReward} XP   +{_goldReward} g\nLooted: {loot.name}{foodLine}";

            // Cascade the loot card from above with a colored ring + particle trail
            SpawnLootCascade(loot.name, EquipmentService.RarityColor(loot.rarity));

            _attackBtn.interactable = false;

            // Apply rewards
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null)
                {
                    data.sparqCoins += _goldReward;
                    Progression.GrantXp(data, _xpReward);   // single canonical curve
                    Sparq.Core.SaveService.Save();
                }
            } catch {}

            // Victory: thunder + coin chime
            PlayClip(_thunderClip, 1f, 1.1f);
            EnsureRunner();
            _runner.StartCoroutine(DelayedClip(0.5f, _hitClip, 0.6f, 1.4f));
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}

            // Spawn a Continue button
            var btn = MakeBtn(_root.transform, "Continue", "Continue",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(360, 100),
                GOLD, DEEP_NAVY, 32);
            btn.onClick.AddListener(() => Object.Destroy(_root));
            HideActionBar();
        }

        private static void HideActionBar()
        {
            if (_strikeBtn != null) _strikeBtn.gameObject.SetActive(false);
            if (_powerBtn  != null) _powerBtn.gameObject.SetActive(false);
            if (_guardBtn  != null) _guardBtn.gameObject.SetActive(false);
            if (_fleeBtn   != null) _fleeBtn.gameObject.SetActive(false);
        }

        private static void Defeat()
        {
            _statusText.text = "DEFEAT… Train and try again.";
            _attackBtn.interactable = false;
            // Real death explosion
            PlayClip(_explosionClip, 1f, 0.9f);

            var btn = MakeBtn(_root.transform, "Retreat", "Retreat",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(360, 100),
                new Color(0.5f, 0.5f, 0.55f), CREAM, 30);
            btn.onClick.AddListener(() => Object.Destroy(_root));
            HideActionBar();
        }

        // ───────── helpers ─────────
        private static void UpdateBars()
        {
            _playerHpBar.value = (float)_playerHP / _playerMaxHP;
            _enemyHpBar.value = (float)_enemyHP / _enemyMaxHP;
            _playerHpText.text = $"{_playerHP}/{_playerMaxHP}";
            _enemyHpText.text  = $"{_enemyHP}/{_enemyMaxHP}";
        }

        private static void FloatText(Transform parent, Vector3 worldOffset, string text, Color color)
        {
            try { Sparq.UI.XPFloater.Spawn(parent, parent.position + worldOffset, text, color); } catch {}
        }

        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = GameObject.Find("BattleRunner");
            if (go == null) { go = new GameObject("BattleRunner"); Object.DontDestroyOnLoad(go); }
            _runner = go.AddComponent<RunnerStub>();
        }

        private class RunnerStub : MonoBehaviour {}

        private static Sprite AssetDatabase_LoadAssetAtPath(string path)
        {
            #if UNITY_EDITOR
            return Sparq.Core.SpriteLoader.Load(path);
            #else
            return Resources.Load<Sprite>(System.IO.Path.GetFileNameWithoutExtension(path));
            #endif
        }

        private static void EnsureSprite(string path)
        {
            #if UNITY_EDITOR
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
            { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != UnityEditor.SpriteImportMode.Single)
            { imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
            #endif
        }

        // ───────── Animated forest backdrop ─────────
        private static void BuildForestBackdrop(Transform parent, Biome biome)
        {
            #if UNITY_EDITOR
            // Ground strip — biome-specific tile
            var groundSp = LoadSpriteForBattle(biome.groundTile);
            if (groundSp != null)
            {
                var ground = new GameObject("Ground", typeof(RectTransform), typeof(Image));
                ground.transform.SetParent(parent, false);
                var grt = ground.GetComponent<RectTransform>();
                grt.anchorMin = new Vector2(0, 0); grt.anchorMax = new Vector2(1, 0);
                grt.pivot = new Vector2(0.5f, 0);
                grt.anchoredPosition = new Vector2(0, 0);
                grt.sizeDelta = new Vector2(0, 360);
                var gImg = ground.GetComponent<Image>();
                gImg.sprite = groundSp;
                gImg.type = Image.Type.Tiled;
                gImg.color = biome.groundTint;
                gImg.raycastTarget = false;
            }

            string[] treeAssets = biome.treeAssets;
            // (x anchor 0..1, y px from bottom, size, scale tint, sway amp, sway speed, phase)
            var slots = new (float xN, float yPx, float w, float h, float tint, float amp, float spd, float ph)[] {
                (0.05f, 280, 280, 360, 0.70f, 2.5f, 0.55f, 0.0f),
                (0.22f, 320, 220, 300, 0.62f, 3.2f, 0.45f, 0.7f),
                (0.42f, 340, 200, 280, 0.55f, 2.0f, 0.62f, 1.4f),
                (0.62f, 330, 220, 300, 0.60f, 3.0f, 0.50f, 2.1f),
                (0.82f, 310, 240, 320, 0.65f, 2.4f, 0.58f, 2.8f),
                (0.95f, 290, 280, 360, 0.72f, 2.8f, 0.52f, 3.5f),
            };
            for (int i = 0; i < slots.Length; i++)
            {
                var sp = LoadSpriteForBattle(treeAssets[i % treeAssets.Length]);
                if (sp == null) continue;
                var s = slots[i];
                var tree = new GameObject($"Tree_{i}", typeof(RectTransform), typeof(Image));
                tree.transform.SetParent(parent, false);
                var trt = tree.GetComponent<RectTransform>();
                trt.anchorMin = new Vector2(s.xN, 0); trt.anchorMax = new Vector2(s.xN, 0);
                trt.pivot = new Vector2(0.5f, 0); // pivot at base so sway rotates from trunk
                trt.anchoredPosition = new Vector2(0, s.yPx);
                trt.sizeDelta = new Vector2(s.w, s.h);
                var img = tree.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.color = new Color(biome.treeTint.r * s.tint, biome.treeTint.g * s.tint, biome.treeTint.b * s.tint, 1f);
                img.raycastTarget = false;

                var sway = tree.AddComponent<Sparq.UI.GentleSway>();
                sway.amplitude = s.amp;
                sway.speed = s.spd;
                sway.phase = s.ph;
            }
            #endif
        }

        // ───────── Floating ambient fireflies ─────────
        private static void BuildFireflies(Transform parent, int count, Color tint)
        {
            EnsureRunner();
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"Firefly_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Random.Range(8f, 14f), Random.Range(8f, 14f));
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSpriteForBattle();
                img.color = tint;
                img.raycastTarget = false;
                _runner.StartCoroutine(FireflyLife(rt, img, Random.Range(0f, 6.28f)));
            }
        }

        private static IEnumerator FireflyLife(RectTransform rt, Image img, float seed)
        {
            float baseX = Random.Range(80f, 1000f);
            float baseY = Random.Range(420f, 1500f);
            float driftX = Random.Range(40f, 90f);
            float driftY = Random.Range(30f, 70f);
            float spdX = Random.Range(0.25f, 0.55f);
            float spdY = Random.Range(0.35f, 0.7f);
            float pulseSpd = Random.Range(1.2f, 2.2f);
            Color c = img.color;
            while (rt != null)
            {
                float t = Time.time + seed;
                rt.anchoredPosition = new Vector2(
                    baseX + Mathf.Sin(t * spdX) * driftX,
                    baseY + Mathf.Cos(t * spdY) * driftY);
                float a = 0.55f + 0.35f * (Mathf.Sin(t * pulseSpd) * 0.5f + 0.5f);
                c.a = a; img.color = c;
                yield return null;
            }
        }

        private static Sprite LoadSpriteForBattle(string path)
        {
            #if UNITY_EDITOR
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null)
            {
                bool changed = false;
                if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency)
                { imp.alphaIsTransparency = true; changed = true; }
                if (changed) imp.SaveAndReimport();
            }
            return Sparq.Core.SpriteLoader.Load(path);
            #else
            return null;
            #endif
        }

        private static GameObject MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static Button MakeBtn(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color bg, Color fg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;

            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = fontSize;
            tm.fontStyle = FontStyles.Bold;
            tm.color = fg;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
