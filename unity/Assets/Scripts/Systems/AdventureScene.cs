using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.Systems
{
    /// <summary>
    /// Top-down auto-action adventure scene — Maleficus / Top Heroes style.
    ///
    /// Core loop:
    ///  • Hero stands on a tile field, plays idle animation
    ///  • Enemies spawn in waves on the path
    ///  • Hero auto-attacks the nearest enemy in range (animated frames)
    ///  • Damage numbers float up from each hit
    ///  • Defeated enemies leave loot drops the player taps to collect
    ///  • Pet companion follows the hero and joins attacks
    ///
    /// Phase 1 (this build):
    ///  - Static tile background
    ///  - Animated hero (FantasyKnight idle/attack frames)
    ///  - 3 enemies that approach + take hits
    ///  - Damage floaters
    ///  - Victory popup with loot
    ///
    /// Phase 2 (future):
    ///  - Joystick movement, multi-wave waves, camera scroll
    /// </summary>
    public static class AdventureScene
    {
        // ─────────── Asset paths ───────────
        private const string BG_PATH =
            "Assets/BattleOfHeroes/Backgrounds/PNG/Top-Down Simple Dry_Ground 01.png";
        // Hero — use BoH Spartan Knight (proven to load) instead of FantasyKnight
        private const string HERO_IDLE_BASE =
            "Assets/BattleOfHeroes/Characters/Frontier Defender Spartan Knight/PNG/PNG Sequences/Idle/Idle_";
        private const string HERO_ATTACK_BASE =
            "Assets/BattleOfHeroes/Characters/Frontier Defender Spartan Knight/PNG/PNG Sequences/Attack/Attack_";
        // Enemies — use monster pack sprites (known to work) for guaranteed visibility
        private const string ENEMY_PATH_GOBLIN =
            "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brawler/Brigading-Brawler.png";
        private const string ENEMY_PATH_GNOLL =
            "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brute/Pyro-Brute.png";
        private const string ENEMY_PATH_THUG =
            "Assets/2D Fantasy Monster Sprite Pack/Monsters/Demon/Gliding-Demon.png";

        // ─────────── State ───────────
        private static GameObject _root;
        private static RectTransform _heroRT;
        private static Image _heroImg;
        private static Sprite[] _heroIdleFrames;
        private static Sprite[] _heroAttackFrames;
        private static MonoBehaviour _runner;
        private static readonly List<Enemy> _enemies = new();
        private static int _heroHP = 100, _heroMaxHP = 100;
        private static int _coinsThisRun = 0;

        private class Enemy
        {
            public RectTransform rt;
            public Image img;
            public int hp, maxHp;
            public string name;
        }

        // ─────────── Public API ───────────
        private static string _stageName;
        public static void Show(string stageName = "Forest Patrol")
        {
            if (_root != null) Hide();
            _coinsThisRun = 0;
            _heroHP = _heroMaxHP;
            _enemies.Clear();
            _stageName = stageName ?? "Forest Patrol";
            HideHomeHud(true);   // hide top/bottom nav, currency pills, Karu/Wisp HUD

            // Root canvas — top-level overlay
            _root = new GameObject("AdventureRoot",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 14000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            BuildBackground();
            BuildHUD(stageName);
            BuildHero();
            BuildPet();
            SpawnEnemyWave();

            EnsureRunner();
            if (_runner != null)
            {
                _runner.StartCoroutine(HeroIdleLoop());
                _runner.StartCoroutine(CombatLoop());
                _runner.StartCoroutine(PetAttackLoop());   // pet shoots Wind Ball / Lightning Arrow
            }
        }

        public static void Hide()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            _enemies.Clear();
            HideHomeHud(false);   // restore home HUD when leaving the adventure
        }

        // Toggle the home-screen HUD elements (top nav buttons, bottom nav,
        // currency pills, Karu/Wisp profile) so the battle is full-screen focus.
        private static readonly string[] HUD_NAMES = {
            "HomeNavButtons",         // MAP/SHOP/BAG/PETS/WORLD pills
            "TopHud",                 // currency pills
            "BottomNav",              // HOME/QUESTS/JOURNAL/etc tabs
            "ProfileBar",             // Karu/Wisp Lv2 HUD top-right
            "PlayerProfile",          // alt name
            "CurrencyBar",            // alt name
        };
        private static readonly List<GameObject> _hiddenForBattle = new();
        private static void HideHomeHud(bool hide)
        {
            if (hide)
            {
                _hiddenForBattle.Clear();
                foreach (var name in HUD_NAMES)
                {
                    var go = GameObject.Find(name);
                    if (go != null && go.activeSelf)
                    {
                        _hiddenForBattle.Add(go);
                        go.SetActive(false);
                    }
                }
            }
            else
            {
                foreach (var go in _hiddenForBattle)
                {
                    if (go != null) go.SetActive(true);
                }
                _hiddenForBattle.Clear();
            }
        }

        // ─────────── Background ───────────
        private static void BuildBackground()
        {
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_root.transform, false);
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = bg.GetComponent<Image>();
            img.raycastTarget = false;
            #if UNITY_EDITOR
            var sp = LoadSprite(BG_PATH);
            if (sp != null) { img.sprite = sp; img.preserveAspect = false; }
            else img.color = new Color(0.35f, 0.55f, 0.30f);   // grass green fallback
            #else
            img.color = new Color(0.35f, 0.55f, 0.30f);
            #endif
        }

        // ─────────── HUD: HP bar, coin counter, BACK ───────────
        private static TMP_Text _hpText, _coinText;
        private static Image _hpFill;
        private static void BuildHUD(string stageName)
        {
            // Top bar — semi-transparent strip with HP, stage name, coins
            var bar = new GameObject("HUD", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(_root.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.sizeDelta = new Vector2(0, 130);
            bar.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            bar.GetComponent<Image>().raycastTarget = false;

            // Stage name on the LEFT
            MakeText(bar.transform, "Stage", $"⚔ {stageName}",
                28, FontStyles.Bold, new Color(1f, 0.95f, 0.55f),
                new Vector2(0, 0), new Vector2(0.4f, 1), new Vector2(28, 0), Vector2.zero)
                .alignment = TextAlignmentOptions.MidlineLeft;

            // HP bar in CENTER
            var hpBg = new GameObject("HpBg", typeof(RectTransform), typeof(Image));
            hpBg.transform.SetParent(bar.transform, false);
            var hbg = hpBg.GetComponent<RectTransform>();
            hbg.anchorMin = new Vector2(0.4f, 0.3f); hbg.anchorMax = new Vector2(0.7f, 0.7f);
            hbg.offsetMin = Vector2.zero; hbg.offsetMax = Vector2.zero;
            hpBg.GetComponent<Image>().color = new Color(0.20f, 0.05f, 0.10f);
            hpBg.GetComponent<Image>().raycastTarget = false;

            var hpFill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
            hpFill.transform.SetParent(hpBg.transform, false);
            var hfRT = hpFill.GetComponent<RectTransform>();
            hfRT.anchorMin = Vector2.zero; hfRT.anchorMax = Vector2.one;
            hfRT.offsetMin = new Vector2(2, 2); hfRT.offsetMax = new Vector2(-2, -2);
            _hpFill = hpFill.GetComponent<Image>();
            _hpFill.color = new Color(0.45f, 0.85f, 0.40f);
            _hpFill.raycastTarget = false;
            _hpFill.type = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;

            _hpText = MakeText(hpBg.transform, "HpTxt", $"{_heroHP}/{_heroMaxHP}",
                22, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _hpText.alignment = TextAlignmentOptions.Center;
            _hpText.outlineWidth = 0.20f;
            _hpText.outlineColor = new Color(0, 0, 0, 0.85f);

            // Coin counter on the RIGHT
            _coinText = MakeText(bar.transform, "Coins", $"✦ {_coinsThisRun}",
                28, FontStyles.Bold, new Color(1f, 0.85f, 0.30f),
                new Vector2(0.7f, 0), new Vector2(0.95f, 1), Vector2.zero, Vector2.zero);
            _coinText.alignment = TextAlignmentOptions.MidlineRight;

            // BACK button (X) top-right
            var back = MakeBtn(_root.transform, "Back", "✕",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -30), new Vector2(70, 70),
                new Color(0.45f, 0.20f, 0.55f), Color.white, 36);
            back.onClick.AddListener(Hide);
        }

        // ─────────── Hero ───────────
        private static void BuildHero()
        {
            var hero = new GameObject("Hero", typeof(RectTransform), typeof(Image));
            hero.transform.SetParent(_root.transform, false);
            var rt = hero.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.3f); rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(220, 220);   // chibi-scale, like reference
            _heroRT = rt;
            _heroImg = hero.GetComponent<Image>();
            _heroImg.preserveAspect = true;
            _heroImg.raycastTarget = false;

            // ── Class-driven hero loadout — sprite changes based on weapon ──
            var heroClass = HeroClassResolver.Resolve();
            _heroIdleFrames   = LoadFramesAt(heroClass.idleBase,   heroClass.idleCount);
            _heroAttackFrames = LoadFramesAt(heroClass.attackBase, heroClass.attackCount);

            // Fallback to BoH Spartan Knight if the class pack didn't load
            bool idleLoaded = _heroIdleFrames != null && _heroIdleFrames.Length > 0 && _heroIdleFrames[0] != null;
            if (!idleLoaded)
            {
                _heroIdleFrames   = LoadFramesAt(HERO_IDLE_BASE, 20);
                _heroAttackFrames = LoadFramesAt(HERO_ATTACK_BASE, 20);
                idleLoaded = _heroIdleFrames != null && _heroIdleFrames.Length > 0 && _heroIdleFrames[0] != null;
            }
            if (idleLoaded)
            {
                _heroImg.sprite = _heroIdleFrames[0];
                Debug.Log($"[AdventureScene] Hero class: {heroClass.className}");
            }
            else
            {
                _heroImg.color = new Color(0.85f, 0.55f, 0.30f);
            }
        }

        // ─────────── Pet (placeholder spot to hero's right) ───────────
        private static void BuildPet()
        {
            var pet = new GameObject("Pet", typeof(RectTransform), typeof(Image));
            pet.transform.SetParent(_root.transform, false);
            var rt = pet.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.3f); rt.anchorMax = new Vector2(0.5f, 0.3f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(140, -30);
            rt.sizeDelta = new Vector2(130, 130);   // chibi scale
            var img = pet.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            #if UNITY_EDITOR
            // Resolve the active pet's species sprite via PetService catalog
            string petPath = null;
            try
            {
                var active = Sparq.Systems.PetService.Active();
                if (active != null)
                {
                    foreach (var sp2 in Sparq.Systems.PetService.CATALOG)
                    {
                        if (sp2.id == active.speciesId)
                        {
                            petPath = sp2.spritePath;
                            break;
                        }
                    }
                }
            } catch {}
            // Fallback to a known monster pack creature so we never crash
            if (string.IsNullOrEmpty(petPath))
                petPath = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Cloud/Happy-Cloud.png";
            var sp = LoadSprite(petPath);
            if (sp != null) img.sprite = sp;
            #endif

            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(PetIdleBob(rt));
        }

        // ─────────── Enemy spawning ───────────
        private static readonly string[] ENEMY_NAMES = { "Goblin", "Gnoll", "Thug" };
        private static readonly string[] ENEMY_PATHS = { ENEMY_PATH_GOBLIN, ENEMY_PATH_GNOLL, ENEMY_PATH_THUG };
        private static bool IsBossStage(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLower();
            return n.Contains("boss") || n.Contains("lord") || n.Contains("spectre")
                || n.Contains("zeus") || n.Contains("pharaoh") || n.Contains("minotaur");
        }

        private static void SpawnEnemyWave()
        {
            // ── BOSS path: stage names like "Spectre Lord" spawn one big boss ──
            if (IsBossStage(_stageName))
            {
                SpawnMythologyBoss();
                return;
            }
            // 6 enemies scattered across the upper-half of the field —
            // chibi-scale, like the Maleficus reference screenshots.
            // Two rows of 3, with slight horizontal jitter for a natural look.
            Vector2[] slots = {
                new Vector2(-280,  90), new Vector2(0,  130), new Vector2(280,  90),
                new Vector2(-200,-120), new Vector2(220,-110), new Vector2(0,  -50),
            };
            for (int i = 0; i < slots.Length; i++)
            {
                int idx = Random.Range(0, ENEMY_NAMES.Length);
                var go = new GameObject($"Enemy_{i}_{ENEMY_NAMES[idx]}",
                    typeof(RectTransform), typeof(Image));
                go.transform.SetParent(_root.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.65f); rt.anchorMax = new Vector2(0.5f, 0.65f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = slots[i] + new Vector2(Random.Range(-20f, 20f), Random.Range(-15f, 15f));
                rt.sizeDelta = new Vector2(160, 160);   // chibi-scale enemies
                var img = go.GetComponent<Image>();
                img.preserveAspect = true;
                img.raycastTarget = false;
                #if UNITY_EDITOR
                var sp = LoadSprite(ENEMY_PATHS[idx]);
                if (sp != null) img.sprite = sp;
                else
                {
                    // Visible fallback: red disc with name
                    img.color = new Color(0.85f, 0.30f, 0.30f);
                    var lbl = MakeText(go.transform, "L", ENEMY_NAMES[idx],
                        28, FontStyles.Bold, Color.white,
                        Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    lbl.alignment = TextAlignmentOptions.Center;
                }
                #endif

                _enemies.Add(new Enemy {
                    rt = rt, img = img,
                    hp = 22, maxHp = 22,    // weaker — there are 6 of them
                    name = ENEMY_NAMES[idx],
                });
                // Idle bob for enemy
                EnsureRunner();
                if (_runner != null) _runner.StartCoroutine(EnemyIdleBob(rt));
            }
        }

        // ─────────── Combat loop — hero auto-attacks nearest enemy ───────────
        private static IEnumerator CombatLoop()
        {
            yield return new WaitForSeconds(1.0f);   // brief intro pause
            while (_root != null && _heroHP > 0)
            {
                // Find target
                Enemy target = null;
                float closestSq = float.MaxValue;
                foreach (var e in _enemies)
                {
                    if (e == null || e.rt == null || e.hp <= 0) continue;
                    float d = (e.rt.anchoredPosition - _heroRT.anchoredPosition).sqrMagnitude;
                    if (d < closestSq) { closestSq = d; target = e; }
                }
                if (target == null)
                {
                    // No enemies left — victory
                    ShowVictory();
                    yield break;
                }
                // Attack — sword swing + random elemental VFX + damage number
                yield return _runner.StartCoroutine(PlayAttackFrames());
                SpawnSlashVFX(target.rt.anchoredPosition);

                // 15% chance for a CRITICAL HIT — double damage + big lightning spell
                bool crit = Random.value < 0.15f;
                int dmg = Random.Range(8, 16);
                if (crit)
                {
                    dmg *= 2;
                    SpawnCritVFX(target.rt.anchoredPosition);
                }
                target.hp -= dmg;
                SpawnDamageNumber(target.rt.anchoredPosition + new Vector2(0, 120),
                                  crit ? dmg : dmg, crit);
                if (target.hp <= 0)
                {
                    SpawnExplosionVFX(target.rt.anchoredPosition);
                    DefeatEnemy(target);
                }
                else if (!crit)
                {
                    // Random impact VFX — sometimes smoke, sometimes lightning, sometimes wind
                    SpawnRandomImpactVFX(target.rt.anchoredPosition);
                }
                yield return new WaitForSeconds(0.55f);
            }
        }

        private static bool _heroAttacking = false;
        private static IEnumerator PlayAttackFrames()
        {
            if (_heroAttackFrames == null || _heroAttackFrames.Length == 0) yield break;
            _heroAttacking = true;
            for (int i = 0; i < _heroAttackFrames.Length; i++)
            {
                if (_heroImg == null) { _heroAttacking = false; yield break; }
                if (_heroAttackFrames[i] != null) _heroImg.sprite = _heroAttackFrames[i];
                yield return new WaitForSeconds(0.035f);
            }
            _heroAttacking = false;
        }

        private static IEnumerator HeroIdleLoop()
        {
            int idx = 0;
            while (_root != null)
            {
                // Skip the entire frame swap during attack — no race
                if (!_heroAttacking &&
                    _heroIdleFrames != null && _heroIdleFrames.Length > 0 && _heroImg != null
                    && _heroIdleFrames[idx] != null)
                {
                    _heroImg.sprite = _heroIdleFrames[idx];
                    idx = (idx + 1) % _heroIdleFrames.Length;
                }
                yield return new WaitForSeconds(0.07f);
            }
        }

        private static IEnumerator EnemyIdleBob(RectTransform rt)
        {
            float t = 0f;
            Vector2 baseP = rt.anchoredPosition;
            while (rt != null && _root != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = baseP + new Vector2(0, Mathf.Sin(t * 2f) * 8f);
                yield return null;
            }
        }

        private static IEnumerator PetIdleBob(RectTransform rt)
        {
            float t = 0f;
            Vector2 baseP = rt.anchoredPosition;
            while (rt != null && _root != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = baseP + new Vector2(0, Mathf.Sin(t * 1.6f) * 10f);
                yield return null;
            }
        }

        // ─────────── Damage floater ───────────
        // toHero=true is now repurposed: true means CRITICAL hit (bigger, gold "CRIT!")
        private static void SpawnDamageNumber(Vector2 anchoredPos, int dmg, bool isCrit)
        {
            var go = new GameObject("Dmg", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.7f); rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(220, 70);
            var tm = go.GetComponent<TextMeshProUGUI>();
            tm.text = isCrit ? $"CRIT!  -{dmg}" : $"-{dmg}";
            tm.fontSize = isCrit ? 56 : 38;
            tm.fontStyle = FontStyles.Bold;
            tm.color = isCrit ? new Color(1f, 0.85f, 0.30f) : new Color(1f, 0.95f, 0.55f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.outlineWidth = 0.45f;
            tm.outlineColor = new Color(0.10f, 0.05f, 0.05f);
            tm.raycastTarget = false;
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(FloatDamage(rt, tm));
        }

        private static IEnumerator FloatDamage(RectTransform rt, TMP_Text tm)
        {
            float t = 0f, life = 0.9f;
            Vector2 start = rt.anchoredPosition;
            while (t < life && rt != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = start + new Vector2(0, 80f * (t / life));
                if (tm != null) { var c = tm.color; c.a = 1f - (t / life); tm.color = c; }
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void DefeatEnemy(Enemy e)
        {
            if (e == null || e.rt == null) return;
            // Coin gain + floater
            int reward = Random.Range(8, 22);
            _coinsThisRun += reward;
            if (_coinText != null) _coinText.text = $"✦ {_coinsThisRun}";
            SpawnDamageNumber(e.rt.anchoredPosition + new Vector2(0, 100), reward, false);
            // Fade and destroy
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(FadeAndDestroy(e.img, e.rt));
            e.rt = null; e.img = null;
        }

        private static IEnumerator FadeAndDestroy(Image img, RectTransform rt)
        {
            float t = 0f;
            while (t < 0.4f && img != null)
            {
                t += Time.deltaTime;
                var c = img.color; c.a = 1f - (t / 0.4f); img.color = c;
                if (rt != null) rt.localScale = Vector3.one * (1f + t * 0.4f);
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void ShowVictory()
        {
            // Animated VICTORY banner — plays the AnimatedTextGame sequence
            var banner = new GameObject("VictoryBanner",
                typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(_root.transform, false);
            var rt = banner.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 60);
            rt.sizeDelta = new Vector2(900, 520);
            var bImg = banner.GetComponent<Image>();
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;

            // Frames 00..33 — play the animation
            var frames = LoadFramesAt("Assets/AnimatedTextGame/PNG/Victory/Victory_", 34);
            // Some packs use 2-digit zero-pad (00..09 vs 10..33).  Our LoadFramesAt
            // uses {i:000} (3-digit) which doesn't match — use a custom loop here.
            #if UNITY_EDITOR
            var list = new List<Sprite>();
            for (int i = 0; i < 34; i++)
            {
                var sp = LoadSprite($"Assets/AnimatedTextGame/PNG/Victory/Victory_{i:00}.png");
                if (sp != null) list.Add(sp);
            }
            frames = list.ToArray();
            #endif

            if (frames != null && frames.Length > 0 && frames[0] != null)
                bImg.sprite = frames[0];
            else
            {
                // Fallback to plain text banner
                bImg.color = new Color(0.20f, 0.10f, 0.30f, 0.95f);
                MakeText(banner.transform, "Title", "✦  VICTORY  ✦",
                    64, FontStyles.Bold, new Color(1f, 0.85f, 0.30f),
                    new Vector2(0, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero)
                    .alignment = TextAlignmentOptions.Center;
            }

            // Reward text below the banner
            var reward = MakeText(_root.transform, "Reward", $"+{_coinsThisRun} coins",
                52, FontStyles.Bold, new Color(1f, 0.85f, 0.30f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -260), new Vector2(600, 80));
            reward.alignment = TextAlignmentOptions.Center;
            reward.outlineWidth = 0.40f;
            reward.outlineColor = new Color(0.20f, 0.04f, 0.10f);

            // Animate the banner frames
            EnsureRunner();
            if (_runner != null && frames != null && frames.Length > 1)
                _runner.StartCoroutine(PlayBannerFrames(bImg, frames));

            // Award coins
            try { Sparq.Core.SaveService.Data.sparqCoins += _coinsThisRun; Sparq.Core.SaveService.Save(); } catch {}

            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(AutoCloseAfter(2.5f));
        }

        private static IEnumerator AutoCloseAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Hide();
        }

        // ── Mythology boss — single big enemy with shield aura intro ──
        private static void SpawnMythologyBoss()
        {
            // Pick a boss based on stage name, default Minotaur
            string bossName = "Minotaur";
            string n = (_stageName ?? "").ToLower();
            if (n.Contains("zeus") || n.Contains("storm") || n.Contains("spectre")) bossName = "Zeus";
            else if (n.Contains("pharaoh") || n.Contains("ancient") || n.Contains("dark"))   bossName = "Pharaoh";

            var go = new GameObject($"Boss_{bossName}",
                typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.65f); rt.anchorMax = new Vector2(0.5f, 0.65f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(380, 380);   // 2× normal enemy size
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;

            #if UNITY_EDITOR
            string idle0 = $"Assets/MythologyBosses/{bossName}/PNG/PNG Sequences/Front - Idle/Front - Idle_000.png";
            var sp = LoadSprite(idle0);
            // Some bosses only have Back-Idle
            if (sp == null)
                sp = LoadSprite($"Assets/MythologyBosses/{bossName}/PNG/PNG Sequences/Back - Idle/Back - Idle_000.png");
            if (sp != null) img.sprite = sp;
            else { img.color = new Color(0.6f, 0.2f, 0.2f); }
            #endif

            _enemies.Add(new Enemy {
                rt = rt, img = img,
                hp = 110, maxHp = 110,    // beefier than grunts
                name = bossName,
            });
            // Bobbing
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(EnemyIdleBob(rt));

            // ── BOSS INTRO — Earth Shield aura flares around the boss ──
            SpawnBossShieldAura(rt.anchoredPosition);
        }

        // Boss intro — magical shield aura plays once when boss appears
        private static Sprite[] _bossAuraFrames;
        private static void SpawnBossShieldAura(Vector2 anchoredPos)
        {
            if (_bossAuraFrames == null)
            {
                #if UNITY_EDITOR
                var list = new List<Sprite>();
                for (int i = 1; i <= 18; i++)
                {
                    var sp = LoadSprite($"Assets/MagicShieldFX/Earth Shield/PNG/Earth Shield_Frame_{i:00}.png");
                    if (sp == null) sp = LoadSprite($"Assets/MagicShieldFX/Fire Shield/PNG/Fire Shield_Frame_{i:00}.png");
                    if (sp != null) list.Add(sp);
                }
                _bossAuraFrames = list.ToArray();
                #endif
            }
            SpawnVFXAt(anchoredPos, _bossAuraFrames, new Vector2(560, 560), 0.05f);
        }

        // ── Pet specials — pet attacks every 1.5s with Wind Ball or Lightning Arrow ──
        private static Sprite[] _petWindBallFrames;
        private static Sprite[] _petLightningArrowFrames;
        public static IEnumerator PetAttackLoop()
        {
            yield return new WaitForSeconds(2.0f);   // wait for hero to land first attack
            while (_root != null && _heroHP > 0)
            {
                // Find any alive enemy
                Enemy target = null;
                foreach (var e in _enemies)
                    if (e != null && e.rt != null && e.hp > 0) { target = e; break; }
                if (target == null) yield break;

                // Pick a random pet special
                if (Random.value < 0.5f)
                {
                    if (_petWindBallFrames == null)
                    {
                        #if UNITY_EDITOR
                        var list = new List<Sprite>();
                        for (int i = 1; i <= 14; i++)
                        {
                            var sp = LoadSprite($"Assets/WindLightningFX/Wind Ball/PNG/Wind Ball_Frame_{i:00}.png");
                            if (sp != null) list.Add(sp);
                        }
                        _petWindBallFrames = list.ToArray();
                        #endif
                    }
                    SpawnVFXAt(target.rt.anchoredPosition, _petWindBallFrames, new Vector2(220, 220), 0.04f);
                }
                else
                {
                    if (_petLightningArrowFrames == null)
                    {
                        #if UNITY_EDITOR
                        var list = new List<Sprite>();
                        for (int i = 1; i <= 14; i++)
                        {
                            var sp = LoadSprite($"Assets/WindLightningFX/Lightning Arrow/PNG/Lightning Arrow_Frame_{i:00}.png");
                            if (sp != null) list.Add(sp);
                        }
                        _petLightningArrowFrames = list.ToArray();
                        #endif
                    }
                    SpawnVFXAt(target.rt.anchoredPosition, _petLightningArrowFrames, new Vector2(240, 240), 0.04f);
                }

                int dmg = Random.Range(4, 9);
                target.hp -= dmg;
                SpawnDamageNumber(target.rt.anchoredPosition + new Vector2(-40, 80), dmg, false);
                if (target.hp <= 0)
                {
                    SpawnExplosionVFX(target.rt.anchoredPosition);
                    DefeatEnemy(target);
                }
                yield return new WaitForSeconds(1.6f);
            }
        }

        // ── Critical hit — 15% chance for 2× damage + big Lightning Spell ──
        private static Sprite[] _critLightningFrames;
        private static void SpawnCritVFX(Vector2 anchoredPos)
        {
            if (_critLightningFrames == null)
            {
                #if UNITY_EDITOR
                var list = new List<Sprite>();
                for (int i = 1; i <= 14; i++)
                {
                    var sp = LoadSprite($"Assets/WindLightningFX/Lightning Spell/PNG/Lightning Spell_Frame_{i:00}.png");
                    if (sp == null) sp = LoadSprite($"Assets/BattleOfHeroes/Animations/Lightning Spell/PNG/Lightning Spell_Frame_{i:00}.png");
                    if (sp != null) list.Add(sp);
                }
                _critLightningFrames = list.ToArray();
                #endif
            }
            SpawnVFXAt(anchoredPos, _critLightningFrames, new Vector2(440, 440), 0.04f);
        }

        // ── Combat VFX: slash, smoke, explosion ──
        private static Sprite[] _slashFrames;
        private static Sprite[] _smokeFrames;
        private static Sprite[] _explosionFrames;

        private static void SpawnSlashVFX(Vector2 anchoredPos)
        {
            if (_slashFrames == null)
            {
                #if UNITY_EDITOR
                var list = new List<Sprite>();
                for (int i = 0; i < 4; i++)
                {
                    var sp = LoadSprite(
                        $"Assets/BattleOfHeroes/Characters/Armored Ogre/PNG/Vector Parts/Slash FX 0{i}.png");
                    if (sp != null) list.Add(sp);
                }
                _slashFrames = list.ToArray();
                #endif
            }
            SpawnVFXAt(anchoredPos, _slashFrames, new Vector2(280, 280), 0.05f);
        }

        private static void SpawnSmokeVFX(Vector2 anchoredPos)
        {
            if (_smokeFrames == null)
            {
                #if UNITY_EDITOR
                var list = new List<Sprite>();
                for (int i = 1; i <= 12; i++)
                {
                    var sp = LoadSprite(
                        $"Assets/BattleOfHeroes/Animations/Smoke/PNG/Smoke_Frame_{i:00}.png");
                    if (sp != null) list.Add(sp);
                }
                _smokeFrames = list.ToArray();
                #endif
            }
            SpawnVFXAt(anchoredPos, _smokeFrames, new Vector2(220, 220), 0.04f);
        }

        // Random elemental impact VFX — picks from 4 different effects on each
        // hit so attacks feel varied (smoke / lightning / wind / poison etc).
        private static Sprite[][] _impactVfxBank;
        private static readonly (string folder, string framePrefix, int count)[] IMPACT_VFX = {
            ("Assets/BattleOfHeroes/Animations/Smoke/PNG/",                  "Smoke_Frame_",            12),
            ("Assets/CartoonSmokeFX/Smoke Spell/PNG/",                       "Smoke Spell_Frame_",      10),
            ("Assets/WindLightningFX/Lightning Strike/PNG/",                 "Lightning Strike_Frame_", 12),
            ("Assets/CartoonSmokeFX/Poisonous Smoke/PNG/",                   "Poisonous Smoke_Frame_",  12),
        };
        private static void SpawnRandomImpactVFX(Vector2 anchoredPos)
        {
            // Lazy-load all impact banks
            if (_impactVfxBank == null)
            {
                #if UNITY_EDITOR
                _impactVfxBank = new Sprite[IMPACT_VFX.Length][];
                for (int b = 0; b < IMPACT_VFX.Length; b++)
                {
                    var list = new List<Sprite>();
                    var (folder, framePrefix, count) = IMPACT_VFX[b];
                    for (int i = 1; i <= count; i++)
                    {
                        var sp = LoadSprite($"{folder}{framePrefix}{i:00}.png");
                        if (sp != null) list.Add(sp);
                    }
                    _impactVfxBank[b] = list.ToArray();
                }
                #endif
            }
            // Pick a random bank that actually has frames
            if (_impactVfxBank == null) { SpawnSmokeVFX(anchoredPos); return; }
            int tries = 0;
            while (tries < 4)
            {
                int idx = Random.Range(0, _impactVfxBank.Length);
                var bank = _impactVfxBank[idx];
                if (bank != null && bank.Length > 0)
                {
                    SpawnVFXAt(anchoredPos, bank, new Vector2(240, 240), 0.04f);
                    return;
                }
                tries++;
            }
            SpawnSmokeVFX(anchoredPos);
        }

        private static void SpawnExplosionVFX(Vector2 anchoredPos)
        {
            if (_explosionFrames == null)
            {
                #if UNITY_EDITOR
                var list = new List<Sprite>();
                for (int i = 1; i <= 14; i++)
                {
                    var sp = LoadSprite(
                        $"Assets/BattleOfHeroes/Animations/Fire Explosion/PNG/Fire Explosion_Frame_{i:00}.png");
                    if (sp != null) list.Add(sp);
                }
                _explosionFrames = list.ToArray();
                #endif
            }
            SpawnVFXAt(anchoredPos, _explosionFrames, new Vector2(380, 380), 0.045f);
        }

        // Generic frame-sequence VFX spawner — drops a temporary Image at the
        // given world-space position, plays through the frames, then destroys.
        private static void SpawnVFXAt(Vector2 anchoredPos, Sprite[] frames, Vector2 size, float frameTime)
        {
            if (frames == null || frames.Length == 0 || _root == null) return;
            var go = new GameObject("VFX", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.7f); rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.sprite = frames[0];
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(PlayVFX(img, frames, frameTime));
        }

        private static IEnumerator PlayVFX(Image img, Sprite[] frames, float frameTime)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (img == null) yield break;
                if (frames[i] != null) img.sprite = frames[i];
                yield return new WaitForSeconds(frameTime);
            }
            if (img != null) Object.Destroy(img.gameObject);
        }

        // Play a sprite-frame sequence on an Image at ~24fps (typical animation rate)
        private static IEnumerator PlayBannerFrames(Image img, Sprite[] frames)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (img == null || img.gameObject == null) yield break;
                if (frames[i] != null) img.sprite = frames[i];
                yield return new WaitForSeconds(0.04f);
            }
            // Hold on the last frame (don't loop)
        }

        // ─────────── Helpers ───────────
        private static Sprite[] LoadFrames(string folderPrefix, string namePrefix, int count)
        {
            #if UNITY_EDITOR
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++)
                arr[i] = LoadSprite($"{folderPrefix}{namePrefix}{i:000}.png");
            return arr;
            #else
            return null;
            #endif
        }

        // Variant: path already includes the full prefix up to the frame number
        private static Sprite[] LoadFramesAt(string fullPrefix, int count)
        {
            #if UNITY_EDITOR
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++)
                arr[i] = LoadSprite($"{fullPrefix}{i:000}.png");
            return arr;
            #else
            return null;
            #endif
        }

        private static Sprite LoadSprite(string path)
        {
            #if UNITY_EDITOR
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && !Application.isPlaying)
            {
                bool changed = false;
                if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency) { imp.alphaIsTransparency = true; changed = true; }
                if (imp.spriteImportMode != UnityEditor.SpriteImportMode.Single)
                { imp.spriteImportMode = UnityEditor.SpriteImportMode.Single; changed = true; }
                if (changed) imp.SaveAndReimport();
            }
            return Sparq.Core.SpriteLoader.Load(path);
            #else
            return null;
            #endif
        }

        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = GameObject.Find("AdventureSceneRunner");
            if (go == null) { go = new GameObject("AdventureSceneRunner"); Object.DontDestroyOnLoad(go); }
            _runner = go.AddComponent<RunnerStub>();
        }
        private class RunnerStub : MonoBehaviour {}

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
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
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;
            MakeText(go.transform, "Lbl", label, fontSize, FontStyles.Bold, fg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }
    }
}
