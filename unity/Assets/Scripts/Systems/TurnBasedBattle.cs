using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.Systems
{
    /// <summary>
    /// Active-Time-Battle. Class-aware skills, projectile trails, hit-stop,
    /// limit breaks, parallax backgrounds, juicy numbers.
    ///
    /// ENTRY:  TurnBasedBattle.Start(enemyTitle)
    ///
    /// Visual stack (back→front):
    ///   1. Parallax: l1-sky (back, slow drift) + l2-ground (mid) + l3-decorations (front bob)
    ///      Source: Assets/FantasyMaps/_PNG/01 or /02 (chosen per biome)
    ///   2. Vignette
    ///   3. Combatants on platforms
    ///   4. HP bars + status icons
    ///   5. Floating numbers / projectile trails / status flashes
    ///   6. Full-screen ult flash
    ///   7. UI bar + action menu
    /// </summary>
    public static class TurnBasedBattle
    {
        // ───────── Palette ─────────
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color HP_GREEN   = new Color(0.45f, 0.85f, 0.45f);
        private static readonly Color HP_AMBER   = new Color(1.00f, 0.78f, 0.30f);
        private static readonly Color HP_RED     = new Color(0.85f, 0.35f, 0.40f);
        private static readonly Color INCOMING_Y = new Color(1.00f, 0.92f, 0.45f, 0.85f);
        private static readonly Color CARD_BG    = new Color(0.08f, 0.06f, 0.16f, 0.94f);
        private static readonly Color CD_RED     = new Color(0.55f, 0.20f, 0.20f);

        // ───────── Skill model ─────────
        public enum Status { None, Stun, Defend, Evade, Poison, Slow }

        public class Skill
        {
            public string id, name, desc;
            public int    cooldown;
            public float  dmgMult;
            public Status applyStatus;
            public int    statusDur;
            public bool   selfTarget;
            public float  critBonus;
            public int    healAmount;
            public string vfx;          // slash | smash | arrow | bolt | fire | shadow | shield | smoke
            public Color  flashColor;
            public bool   ult;          // ult-tier → full-screen flash + bigger shake
        }

        // Per-class skill kits (slot 0 = basic, slots 1-3 = cooldowns)
        private static readonly Dictionary<string, Skill[]> KITS = new Dictionary<string, Skill[]>
        {
            ["knight"] = new[]
            {
                new Skill { id="slash", name="Slash", desc="Basic strike.",
                    dmgMult=1.0f, vfx="slash", flashColor=new Color(1f, 0.95f, 0.65f) },
                new Skill { id="bash", name="Shield Bash", desc="Heavy hit. Stuns 1 turn.",
                    cooldown=3, dmgMult=1.1f, applyStatus=Status.Stun, statusDur=1,
                    vfx="smash", flashColor=new Color(1f, 0.85f, 0.45f) },
                new Skill { id="iron_will", name="Iron Will", desc="Defend 2 turns (½ damage).",
                    cooldown=4, applyStatus=Status.Defend, statusDur=2, selfTarget=true,
                    vfx="shield", flashColor=new Color(0.55f, 0.85f, 1f) },
                new Skill { id="cleave", name="Cleave", desc="Brutal swing. ULT.",
                    cooldown=3, dmgMult=1.7f, vfx="slash", ult=true,
                    flashColor=new Color(1f, 0.55f, 0.30f) },
            },
            ["archer"] = new[]
            {
                new Skill { id="quick_shot", name="Quick Shot", desc="Fast arrow.",
                    dmgMult=1.0f, vfx="arrow", flashColor=new Color(0.85f, 1f, 0.65f) },
                new Skill { id="volley", name="Volley", desc="Rain of arrows.",
                    cooldown=2, dmgMult=1.5f, vfx="arrow", flashColor=new Color(0.55f, 0.95f, 0.55f) },
                new Skill { id="aimed_shot", name="Aimed Shot", desc="Massive crit. ULT.",
                    cooldown=4, dmgMult=2.4f, critBonus=0.50f, vfx="arrow", ult=true,
                    flashColor=new Color(1f, 0.92f, 0.45f) },
                new Skill { id="trap", name="Snare Trap", desc="Stun 1 turn.",
                    cooldown=3, dmgMult=0.5f, applyStatus=Status.Stun, statusDur=1,
                    vfx="smoke", flashColor=new Color(0.65f, 0.85f, 0.45f) },
            },
            ["mage"] = new[]
            {
                new Skill { id="bolt", name="Bolt", desc="Arcane shock.",
                    dmgMult=1.0f, vfx="bolt", flashColor=new Color(0.65f, 0.55f, 1f) },
                new Skill { id="time_slow", name="Time Slow", desc="Enemy skips next turn.",
                    cooldown=4, dmgMult=0.4f, applyStatus=Status.Slow, statusDur=1,
                    vfx="bolt", flashColor=new Color(0.85f, 0.65f, 1f) },
                new Skill { id="meteor", name="Meteor", desc="Catastrophic burst. ULT.",
                    cooldown=5, dmgMult=3.0f, vfx="fire", ult=true,
                    flashColor=new Color(1f, 0.45f, 0.20f) },
                new Skill { id="ward", name="Time Ward", desc="Defend 2 turns.",
                    cooldown=4, applyStatus=Status.Defend, statusDur=2, selfTarget=true,
                    vfx="shield", flashColor=new Color(0.65f, 0.55f, 1f) },
            },
            ["assassin"] = new[]
            {
                new Skill { id="strike", name="Strike", desc="Quick blade.",
                    dmgMult=1.0f, vfx="slash", flashColor=new Color(0.95f, 0.55f, 0.55f) },
                new Skill { id="smoke_bomb", name="Smoke Bomb", desc="50% dodge for 2 turns.",
                    cooldown=4, applyStatus=Status.Evade, statusDur=2, selfTarget=true,
                    vfx="smoke", flashColor=new Color(0.55f, 0.55f, 0.65f) },
                new Skill { id="backstab", name="Backstab", desc="Massive crit. ULT.",
                    cooldown=4, dmgMult=2.7f, critBonus=0.60f, vfx="shadow", ult=true,
                    flashColor=new Color(0.40f, 0.20f, 0.55f) },
                new Skill { id="poison", name="Toxin Blade", desc="Poisons 3 turns.",
                    cooldown=3, dmgMult=0.8f, applyStatus=Status.Poison, statusDur=3,
                    vfx="slash", flashColor=new Color(0.55f, 0.85f, 0.30f) },
            },
        };

        // Class-tinted HP bar fill
        private static readonly Dictionary<string, Color> CLASS_TINT = new Dictionary<string, Color>
        {
            ["knight"]   = new Color(0.55f, 0.65f, 0.95f),
            ["paladin"]  = new Color(1.00f, 0.78f, 0.30f),
            ["archer"]   = new Color(0.45f, 0.85f, 0.55f),
            ["mage"]     = new Color(0.65f, 0.55f, 0.95f),
            ["assassin"] = new Color(0.85f, 0.40f, 0.50f),
        };

        private static Skill[] KitFor(string cls)
        {
            if (!string.IsNullOrEmpty(cls) && KITS.TryGetValue(cls, out var k)) return k;
            return KITS["knight"];
        }
        private static Color HpTintFor(string cls)
        {
            if (!string.IsNullOrEmpty(cls) && CLASS_TINT.TryGetValue(cls, out var c)) return c;
            return HP_GREEN;
        }

        // ───────── Combatant ─────────
        private class Combatant
        {
            public string name;
            public int    hp, maxHp;
            public float  hpDisplayed;     // animated value the bar lerps toward
            public int    hpIncoming;      // damage about to land (preview)
            public int    atk, def, speed;
            public bool   isPlayer;
            public Skill[] kit;
            public Color  hpFill;

            // Status state
            public int stunTurns, defendTurns, evadeTurns, poisonTurns, slowTurns;
            public int poisonDmgPerTurn;

            // Cooldowns
            public Dictionary<string,int> cd = new Dictionary<string,int>();

            // UI hooks
            public Image      portrait;
            public Slider     hpBar;
            public Image      hpFillImg;
            public Image      hpPreview;       // yellow incoming-dmg band
            public Image      shieldOverlay;
            public TMP_Text   hpTxt;
            public RectTransform rect;
            public Vector2    homePos;
            public Transform  fxLayer;
            public Transform  statusRow;
        }

        // ───────── Enemy roster + biome ─────────
        private static readonly (string title, string sprite, int hp, int dmg, int xp, int gold, string biome)[] ENEMIES = new[]
        {
            ("Forest Goblin", "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brawler/Brigading-Brawler.png",       60, 8, 30, 25, "forest"),
            ("Shadow Wolf",   "Assets/2D Fantasy Monster Sprite Pack/Monsters/Hellhound/Darkness-Hellhound.png",   100,12, 50, 50, "moonlit"),
            ("Mind Phantom",  "Assets/2D Fantasy Monster Sprite Pack/Monsters/Spectre/Nightmare-Spectre.png",       50, 6, 25, 20, "haunted"),
            ("Stone Brute",   "Assets/2D Fantasy Monster Sprite Pack/Monsters/Brute/Shadow-Brute.png",              80,10, 35, 30, "rocky"),
        };

        // Biome → which FantasyMaps painted scene + tint
        private struct Biome
        {
            public string mapNum;        // "01" or "02"
            public Color  skyTint, groundTint, decoTint, vignette;
            public string particleKind;  // "fireflies" | "embers" | "mist" | "snow"
            public Color  particleColor;
        }
        private static Biome BiomeFor(string biome)
        {
            switch (biome)
            {
                case "forest": return new Biome {
                    mapNum="01",
                    skyTint=Color.white, groundTint=Color.white, decoTint=Color.white,
                    vignette=new Color(0,0,0,0.30f),
                    particleKind="fireflies", particleColor=new Color(1f, 0.92f, 0.55f, 0.85f) };
                case "moonlit": return new Biome {
                    mapNum="01",
                    skyTint=new Color(0.45f, 0.40f, 0.65f), groundTint=new Color(0.55f, 0.50f, 0.70f),
                    decoTint=new Color(0.50f, 0.50f, 0.65f),
                    vignette=new Color(0.05f, 0.0f, 0.15f, 0.55f),
                    particleKind="fireflies", particleColor=new Color(0.65f, 0.75f, 1f, 0.85f) };
                case "haunted": return new Biome {
                    mapNum="02",
                    skyTint=new Color(0.70f, 0.55f, 0.85f), groundTint=new Color(0.55f, 0.45f, 0.70f),
                    decoTint=new Color(0.65f, 0.50f, 0.75f),
                    vignette=new Color(0.05f, 0.0f, 0.18f, 0.55f),
                    particleKind="mist", particleColor=new Color(0.85f, 0.55f, 1f, 0.65f) };
                case "rocky": return new Biome {
                    mapNum="02",
                    skyTint=new Color(0.95f, 0.78f, 0.55f), groundTint=new Color(0.85f, 0.72f, 0.55f),
                    decoTint=new Color(0.85f, 0.72f, 0.55f),
                    vignette=new Color(0.10f, 0.05f, 0.0f, 0.35f),
                    particleKind="embers", particleColor=new Color(1f, 0.55f, 0.20f, 0.85f) };
            }
            return new Biome { mapNum="01", skyTint=Color.white, groundTint=Color.white, decoTint=Color.white,
                vignette=new Color(0,0,0,0.30f),
                particleKind="fireflies", particleColor=new Color(1f, 0.92f, 0.55f, 0.85f) };
        }

        // ───────── State ─────────
        private static GameObject _root;
        private static MonoBehaviour _runner;
        private static Combatant _player, _enemy;
        private static int _xpReward, _goldReward;
        private static int _playerTurnCount;
        private static bool _awaitingInput;
        private static int _pickedSkillIdx = -1;
        private static TMP_Text _statusBanner, _comboTxt;
        private static GameObject _actionMenu, _skillMenu;
        private static List<Button> _skillButtons = new List<Button>();
        private static int _comboCount;
        private static float _limitBreak;        // 0..100
        private static Slider _lbBar;
        private static Image  _lbFill, _lbGlow;
        private static Button _lbBtn;
        private static Image  _fullScreenFlash;
        private static Image  _vignette;
        private static RectTransform _skyLayer, _groundLayer, _decoLayer;
        private static Vector2 _skyHome, _decoHome;

        // ── SFX (Leohpaz RPG Essentials) ──
        private const string LEO = "Assets/Leohpaz/RPG_Essentials_Free/";
        private const string SFX_SLASH    = LEO + "10_Battle_SFX/22_Slash_04.wav";
        private const string SFX_FLESH    = LEO + "10_Battle_SFX/15_Impact_flesh_02.wav";
        private const string SFX_FLESH2   = LEO + "10_Battle_SFX/77_flesh_02.wav";
        private const string SFX_BLOCK    = LEO + "10_Battle_SFX/39_Block_03.wav";
        private const string SFX_MISS     = LEO + "10_Battle_SFX/35_Miss_Evade_02.wav";
        private const string SFX_FLEE     = LEO + "10_Battle_SFX/51_Flee_02.wav";
        private const string SFX_ENCOUNTER= LEO + "10_Battle_SFX/55_Encounter_02.wav";
        private const string SFX_DEATH    = LEO + "10_Battle_SFX/69_Enemy_death_01.wav";
        private const string SFX_FIRE     = LEO + "8_Atk_Magic_SFX/04_Fire_explosion_04_medium.wav";
        private const string SFX_ICE      = LEO + "8_Atk_Magic_SFX/13_Ice_explosion_01.wav";
        private const string SFX_THUNDER  = LEO + "8_Atk_Magic_SFX/18_Thunder_02.wav";
        private const string SFX_WIND     = LEO + "8_Atk_Magic_SFX/25_Wind_01.wav";
        private const string SFX_EARTH    = LEO + "8_Atk_Magic_SFX/30_Earth_02.wav";
        private const string SFX_POISON   = LEO + "8_Atk_Magic_SFX/46_Poison_01.wav";
        private const string SFX_CHARGE   = LEO + "8_Atk_Magic_SFX/45_Charge_05.wav";
        private const string SFX_HEAL     = LEO + "8_Buffs_Heals_SFX/02_Heal_02.wav";
        private const string SFX_ATKBUFF  = LEO + "8_Buffs_Heals_SFX/16_Atk_buff_04.wav";
        private const string SFX_DEFBUFF  = LEO + "8_Buffs_Heals_SFX/17_Def_buff_01.wav";
        private const string SFX_DEBUFF   = LEO + "8_Buffs_Heals_SFX/21_Debuff_01.wav";
        private const string SFX_CONFIRM  = LEO + "10_UI_Menu_SFX/013_Confirm_03.wav";
        private const string SFX_DENIED   = LEO + "10_UI_Menu_SFX/033_Denied_03.wav";

        // ── BGM (Action RPG Music) ──
        private const string BGM_DIR = "Assets/Action RPG Music 1.6/";
        private static readonly string[] BGM_BATTLE = {
            BGM_DIR + "BGM07battle1.wav",
            BGM_DIR + "BGM07battle2.wav",
            BGM_DIR + "BGM07battle3.wav",
            BGM_DIR + "BGM07battle4.wav",
        };
        private static readonly string[] BGM_BOSS = {
            BGM_DIR + "BGM08boss1.wav",
            BGM_DIR + "BGM08boss2.wav",
        };
        private const string MS_VICTORY = BGM_DIR + "MS01triumph1NL.wav";
        private const string MS_DEFEAT  = BGM_DIR + "MS02gameover1NL.wav";

        private static AudioSource _audio;     // one-shot SFX
        private static AudioSource _music;     // looping BGM

        // ═══════════════════════════════════════════════════════════════════
        // ENTRY
        // ═══════════════════════════════════════════════════════════════════
        public static void Start(string enemyTitle)
        {
            if (_root != null) Hide();

            int pickIdx = -1;
            for (int i = 0; i < ENEMIES.Length; i++)
                if (string.Equals(ENEMIES[i].title, enemyTitle, System.StringComparison.OrdinalIgnoreCase))
                { pickIdx = i; break; }
            if (pickIdx < 0) pickIdx = Random.Range(0, ENEMIES.Length);
            var e = ENEMIES[pickIdx];

            int gearAtk = 0, gearDef = 0, gearHp = 0;
            try { (gearAtk, gearDef, gearHp) = EquipmentService.TotalStats(); } catch {}
            int level = Sparq.Core.SaveService.Data?.level ?? 1;
            string heroClass = Sparq.Core.SaveService.Data?.heroClass ?? "knight";

            _player = new Combatant
            {
                name = "You", isPlayer = true,
                maxHp = 80 + level * 8 + gearHp,
                atk  = 10 + level * 2 + gearAtk,
                def  = 2 + level + gearDef,
                speed = 10 + Random.Range(0, 4),
                kit = KitFor(heroClass),
                hpFill = HpTintFor(heroClass),
            };
            _player.hp = _player.maxHp; _player.hpDisplayed = _player.maxHp;

            _enemy = new Combatant
            {
                name = e.title, isPlayer = false,
                maxHp = e.hp + level * 5,
                atk  = e.dmg + level,
                def  = level / 2,
                speed = 8 + Random.Range(0, 5),
                kit = null, hpFill = HP_RED,
            };
            _enemy.hp = _enemy.maxHp; _enemy.hpDisplayed = _enemy.maxHp;

            _xpReward = e.xp + level * 4;
            _goldReward = e.gold + level * 3;
            _playerTurnCount = 0;
            _comboCount = 0;
            _limitBreak = 0;

            BuildUI(e.sprite, BiomeFor(e.biome));

            if (_runner == null)
            {
                var go = new GameObject("TurnBasedBattle.Runner");
                _runner = go.AddComponent<RunnerMB>();
                _audio = go.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
                _music = go.AddComponent<AudioSource>();
                _music.playOnAwake = false;
                _music.spatialBlend = 0f;
                _music.loop = true;
                _music.volume = 0.45f;
            }

            // Start battle BGM — random battle track (boss tracks reserved for future boss flag)
            #if UNITY_EDITOR
            string bgmPath = BGM_BATTLE[Random.Range(0, BGM_BATTLE.Length)];
            Sparq.UI.HomeBgm.ConfigureForStreaming(bgmPath);   // streaming + vorbis = smooth playback
            var bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(bgmPath);
            if (bgmClip != null && _music != null)
            {
                _music.clip = bgmClip;
                _music.Play();
            }
            #endif

            _runner.StartCoroutine(BattleLoop());
            _runner.StartCoroutine(LerpHpBars());
            _runner.StartCoroutine(ParallaxDrift());
            _runner.StartCoroutine(SpawnAmbientParticles(BiomeFor(e.biome)));
            _runner.StartCoroutine(InputHotkeys());
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            if (_music != null) _music.Stop();
            if (_runner != null) { Object.Destroy(_runner.gameObject); _runner = null; _audio = null; _music = null; }
        }

        // ═══════════════════════════════════════════════════════════════════
        // BATTLE LOOP
        // ═══════════════════════════════════════════════════════════════════
        private static IEnumerator BattleLoop()
        {
            // Boss intro
            PlayAudio(SFX_ENCOUNTER, 0.85f);
            SetBanner($"<color=#FFD466>{_enemy.name}</color> appeared!");
            FullScreenFlash(new Color(1f, 0.45f, 0.30f, 0.55f), 0.45f);
            ShakeOnce(_root.GetComponent<RectTransform>(), 8f, 0.3f);
            yield return new WaitForSeconds(1.0f);

            var ordered = new List<Combatant>();
            if (_player.speed >= _enemy.speed) { ordered.Add(_player); ordered.Add(_enemy); }
            else { ordered.Add(_enemy); ordered.Add(_player); }

            int idx = 0;
            while (_player.hp > 0 && _enemy.hp > 0)
            {
                var actor = ordered[idx % ordered.Count];

                if (TickStatusStart(actor))
                {
                    yield return new WaitForSeconds(0.5f);
                    if (actor.hp <= 0) break;
                }

                if (actor.slowTurns > 0)
                {
                    actor.slowTurns--;
                    SetBanner($"{actor.name} is <color=#A88BFF>slowed</color> — turn skipped.");
                    yield return new WaitForSeconds(0.7f);
                    idx++;
                    continue;
                }

                if (actor.stunTurns > 0)
                {
                    actor.stunTurns--;
                    SetBanner($"{actor.name} is <color=#FFCC55>stunned</color> — can't act!");
                    yield return new WaitForSeconds(0.8f);
                    idx++;
                    DecrementCooldowns(actor);
                    continue;
                }

                if (actor.isPlayer) yield return PlayerTurn();
                else yield return EnemyTurn();

                if (_player.hp <= 0 || _enemy.hp <= 0) break;

                DecrementCooldowns(actor);
                RefreshSkillButtons();
                idx++;

                if (actor.isPlayer)
                {
                    _playerTurnCount++;
                    if (_playerTurnCount % 3 == 0) yield return PetSupport();
                }
            }

            yield return new WaitForSeconds(0.4f);
            if (_player.hp <= 0) yield return Defeat();
            else yield return Victory();
        }

        // ───────── Player turn ─────────
        private static IEnumerator PlayerTurn()
        {
            SetBanner("Your turn — pick an action.");
            ShowActionMenu(true);
            ActorPulse(_player);
            _awaitingInput = true;
            _pickedSkillIdx = -1;

            while (_awaitingInput) yield return null;

            ShowActionMenu(false);
            ShowSkillMenu(false);

            if (_pickedSkillIdx == -10)
            {
                // Limit Break: signature ult, ignore CD
                _limitBreak = 0;
                RefreshLimitBreak();
                int ultIdx = FindUltIndex(_player.kit);
                if (ultIdx >= 0) yield return ResolveSkill(_player, _enemy, _player.kit[ultIdx]);
                yield break;
            }
            if (_pickedSkillIdx < 0) yield break;

            var skill = _player.kit[_pickedSkillIdx];
            if (skill.cooldown > 0) _player.cd[skill.id] = skill.cooldown;
            yield return ResolveSkill(_player, _enemy, skill);
        }

        private static int FindUltIndex(Skill[] kit)
        {
            for (int i = 0; i < kit.Length; i++) if (kit[i].ult) return i;
            return -1;
        }

        // ───────── Enemy turn ─────────
        private static IEnumerator EnemyTurn()
        {
            SetBanner($"<color=#FF8888>{_enemy.name}</color> attacks!");
            ActorPulse(_enemy);
            yield return new WaitForSeconds(0.45f);

            var basic = new Skill {
                id="enemy_atk", name="Attack", dmgMult=1.0f,
                vfx="slash", flashColor=new Color(1f, 0.45f, 0.45f)
            };
            yield return ResolveSkill(_enemy, _player, basic);
        }

        // ───────── Resolve a skill (the meaty bit) ─────────
        private static IEnumerator ResolveSkill(Combatant attacker, Combatant target, Skill skill)
        {
            // Self-buff
            if (skill.selfTarget && skill.applyStatus != Status.None)
            {
                ApplyStatus(attacker, skill.applyStatus, skill.statusDur);
                FlashPortrait(attacker, skill.flashColor);
                FloatNumber(attacker, skill.name.ToUpper(), skill.flashColor, 1.4f);
                SetBanner($"{attacker.name} casts <color=#FFD466>{skill.name}</color>!");
                // Pick the right buff sound for the status type
                string buffSfx = skill.applyStatus == Status.Defend ? SFX_DEFBUFF
                               : skill.applyStatus == Status.Evade  ? SFX_WIND
                               : SFX_ATKBUFF;
                PlayAudio(buffSfx, 0.7f);
                yield return new WaitForSeconds(0.7f);
                yield break;
            }

            if (skill.healAmount > 0)
            {
                int healed = Mathf.Min(skill.healAmount, attacker.maxHp - attacker.hp);
                attacker.hp += healed;
                FloatNumber(attacker, $"+{healed}", new Color(0.55f, 1f, 0.55f), 1.4f);
                yield return new WaitForSeconds(0.6f);
                yield break;
            }

            // Pre-roll outcome so we can show incoming preview band
            bool willEvade = target.evadeTurns > 0 && Random.value < 0.50f;
            float critChance = 0.10f + skill.critBonus;
            bool crit = !willEvade && Random.value < critChance;
            int dmg = 0;
            if (skill.dmgMult > 0 && !willEvade)
            {
                int raw = Mathf.RoundToInt(attacker.atk * skill.dmgMult);
                dmg = Mathf.Max(1, raw - target.def);
                if (target.defendTurns > 0) dmg = Mathf.Max(1, dmg / 2);
                if (crit) dmg = Mathf.RoundToInt(dmg * 1.6f);
            }

            // Show incoming-damage preview band
            if (dmg > 0)
            {
                target.hpIncoming = dmg;
                RefreshHpPreview(target);
                yield return new WaitForSeconds(0.20f);
            }

            // Spell projectile (caster → target)
            yield return SpawnProjectile(attacker, target, skill);

            if (willEvade)
            {
                FloatNumber(target, "MISS", new Color(0.85f, 0.85f, 0.85f), 1.5f);
                ShakeOnce(target.rect, 4f, 0.18f);
                PlayAudio(SFX_MISS, 0.7f);
                target.hpIncoming = 0;
                RefreshHpPreview(target);
                _comboCount = 0; RefreshCombo();
                yield return new WaitForSeconds(0.5f);
                yield break;
            }

            if (skill.dmgMult > 0)
            {
                target.hp = Mathf.Max(0, target.hp - dmg);
                target.hpIncoming = 0;
                RefreshHpPreview(target);

                FlashPortrait(target, skill.flashColor);
                ShakeOnce(target.rect, crit ? 16f : 8f, crit ? 0.34f : 0.20f);

                // Hit-stop: brief pause to make hits feel weighty
                yield return new WaitForSeconds(crit ? 0.13f : 0.07f);

                // ULT-tier full-screen flash
                if (skill.ult)
                {
                    FullScreenFlash(skill.flashColor, 0.40f);
                    ShakeOnce(_root.GetComponent<RectTransform>(), 12f, 0.32f);
                    PlayAudio(SFX_CHARGE, 0.85f);
                }

                // Damage popup — arc, scale by dmg, color by type
                float scale = Mathf.Clamp(0.9f + dmg / 50f, 1.0f, 2.4f);
                Color col = crit ? GOLD : new Color(1f, 0.55f, 0.55f);
                FloatArcNumber(target, crit ? $"<b>{dmg}!</b>" : $"{dmg}", col, scale * (crit ? 1.3f : 1f), crit);

                // Sound
                PlayWeaponAudio(skill.vfx, crit);

                // Combo
                if (attacker.isPlayer)
                {
                    _comboCount++;
                    if (_comboCount > 1) ShowComboBurst();
                    RefreshCombo();
                }

                // Limit Break: enemy hits fill bar
                if (target.isPlayer)
                {
                    float fill = Mathf.Min(28f, dmg * 28f / Mathf.Max(1, target.maxHp));
                    _limitBreak = Mathf.Min(100f, _limitBreak + fill);
                    RefreshLimitBreak();
                }

                SetBanner($"{attacker.name} → <color=#FFD466>{skill.name}</color>" + (crit ? "  ★ CRIT" : ""));
                yield return new WaitForSeconds(crit ? 0.55f : 0.40f);
            }

            // Apply status to non-self target
            if (!skill.selfTarget && skill.applyStatus != Status.None && target.hp > 0)
            {
                if (skill.applyStatus == Status.Poison)
                {
                    target.poisonTurns = Mathf.Max(target.poisonTurns, skill.statusDur);
                    target.poisonDmgPerTurn = Mathf.Max(target.poisonDmgPerTurn, Mathf.RoundToInt(attacker.atk * 0.3f));
                }
                else ApplyStatus(target, skill.applyStatus, skill.statusDur);
                yield return new WaitForSeconds(0.20f);
            }
        }

        // ───────── Projectile / spell trail ─────────
        private static IEnumerator SpawnProjectile(Combatant from, Combatant to, Skill skill)
        {
            // Symbol per VFX kind
            string symbol = skill.vfx switch
            {
                "slash"  => "✦",
                "smash"  => "✺",
                "arrow"  => "➤",
                "bolt"   => "⚡",
                "fire"   => "●",
                "shadow" => "◆",
                "shield" => "🛡",
                "smoke"  => "☁",
                _        => "✦",
            };

            var go = new GameObject("Proj", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 120);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = symbol;
            tm.fontSize = skill.ult ? 110 : 78;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = skill.flashColor;
            tm.outlineWidth = 0.30f;
            tm.outlineColor = Color.black;
            tm.raycastTarget = false;

            // Animate from → to in canvas space
            Vector3 a = from.rect.position;
            Vector3 b = to.rect.position;

            float dur = skill.ult ? 0.42f : 0.30f;
            float t = 0;
            while (t < dur)
            {
                float p = t / dur;
                Vector3 pos = Vector3.Lerp(a, b, p);
                // Add a slight parabolic arc for ranged-feel
                float arcY = (skill.vfx == "arrow" || skill.vfx == "fire" || skill.vfx == "smash")
                    ? Mathf.Sin(p * Mathf.PI) * 80f : 0f;
                rt.position = pos + new Vector3(0, arcY, 0);
                tm.color = new Color(skill.flashColor.r, skill.flashColor.g, skill.flashColor.b,
                                      Mathf.Lerp(0.7f, 1.0f, Mathf.Sin(p * Mathf.PI)));
                rt.localScale = Vector3.one * Mathf.Lerp(0.7f, skill.ult ? 1.4f : 1.0f, Mathf.Sin(p * Mathf.PI));
                t += Time.deltaTime;
                yield return null;
            }
            Object.Destroy(go);

            // Burst at target — quick scale-up symbol that fades
            if (skill.dmgMult > 0)
            {
                var burst = new GameObject("Burst", typeof(RectTransform));
                burst.transform.SetParent(to.fxLayer, false);
                var brt = burst.GetComponent<RectTransform>();
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(200, 200);
                var btm = burst.AddComponent<TextMeshProUGUI>();
                btm.text = "✦";
                btm.fontSize = skill.ult ? 240 : 160;
                btm.fontStyle = FontStyles.Bold;
                btm.alignment = TextAlignmentOptions.Center;
                btm.color = skill.flashColor;
                btm.outlineWidth = 0.30f; btm.outlineColor = Color.black;
                btm.raycastTarget = false;
                _runner.StartCoroutine(BurstAnim(brt, btm));
            }
        }

        private static IEnumerator BurstAnim(RectTransform rt, TMP_Text tm)
        {
            float dur = 0.30f, t = 0;
            Color baseC = tm.color;
            while (t < dur)
            {
                float p = t / dur;
                rt.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.6f, p);
                tm.color = new Color(baseC.r, baseC.g, baseC.b, 1f - p);
                t += Time.deltaTime;
                yield return null;
            }
            Object.Destroy(rt.gameObject);
        }

        // ───────── Pet support (heal) ─────────
        private static IEnumerator PetSupport()
        {
            int heal = 12 + (Sparq.Core.SaveService.Data?.level ?? 1);
            int actually = Mathf.Min(heal, _player.maxHp - _player.hp);
            if (actually <= 0) yield break;
            _player.hp += actually;
            FloatNumber(_player, $"+{actually}", new Color(0.55f, 1f, 0.55f), 1.5f);
            SetBanner("Your pet pulses with healing light.");
            PlayAudio(SFX_HEAL, 0.85f);
            FullScreenFlash(new Color(0.55f, 1f, 0.55f, 0.30f), 0.35f);
            yield return new WaitForSeconds(0.7f);
        }

        // ───────── Status helpers ─────────
        private static void ApplyStatus(Combatant c, Status s, int dur)
        {
            switch (s)
            {
                case Status.Stun:   c.stunTurns   = Mathf.Max(c.stunTurns,   dur); break;
                case Status.Defend: c.defendTurns = Mathf.Max(c.defendTurns, dur); break;
                case Status.Evade:  c.evadeTurns  = Mathf.Max(c.evadeTurns,  dur); break;
                case Status.Slow:   c.slowTurns   = Mathf.Max(c.slowTurns,   dur); break;
            }
            RefreshStatusIcons(c);
            RefreshShield(c);
        }

        private static bool TickStatusStart(Combatant c)
        {
            bool any = false;
            if (c.poisonTurns > 0)
            {
                int dmg = c.poisonDmgPerTurn;
                c.hp = Mathf.Max(0, c.hp - dmg);
                c.poisonTurns--;
                FloatArcNumber(c, $"{dmg}", new Color(0.55f, 0.85f, 0.30f), 1.1f, false);
                any = true;
            }
            if (c.defendTurns > 0) c.defendTurns--;
            if (c.evadeTurns  > 0) c.evadeTurns--;
            RefreshStatusIcons(c);
            RefreshShield(c);
            return any;
        }

        private static void DecrementCooldowns(Combatant c)
        {
            if (c.cd == null || c.cd.Count == 0) return;
            var keys = new List<string>(c.cd.Keys);
            foreach (var k in keys)
            {
                c.cd[k] = c.cd[k] - 1;
                if (c.cd[k] <= 0) c.cd.Remove(k);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // UI BUILD
        // ═══════════════════════════════════════════════════════════════════
        private static void BuildUI(string enemySpritePath, Biome biome)
        {
            _root = new GameObject("TurnBasedBattleRoot",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 16000;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // ── Parallax background ──
            BuildParallaxBackground(biome);

            // Vignette (above bg, below combatants)
            var vig = MakeImage(_root.transform, "Vignette", biome.vignette);
            var vrt = vig.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            vig.raycastTarget = false;
            _vignette = vig;

            // ── Enemy slot ──
            _enemy.rect = MakeCombatantSlot(_root.transform, "EnemySlot", enemySpritePath,
                new Vector2(0.5f, 0.78f), new Vector2(360, 360), out _enemy.portrait, out _enemy.fxLayer, out _enemy.statusRow);
            BuildHpBar(_root.transform, _enemy, "EnemyHP",
                new Vector2(0.5f, 0.62f), new Vector2(580, 36));

            // ── Player slot ──
            _player.rect = MakeCombatantSlot(_root.transform, "PlayerSlot", null,
                new Vector2(0.30f, 0.36f), new Vector2(320, 320), out _player.portrait, out _player.fxLayer, out _player.statusRow);
            var heroSp = ResolveHeroSprite();
            if (heroSp != null) _player.portrait.sprite = heroSp;
            BuildHpBar(_root.transform, _player, "PlayerHP",
                new Vector2(0.30f, 0.20f), new Vector2(440, 30));

            // Player platform shadow (visual grounding)
            BuildPlatform(_root.transform, _player.rect.anchorMin, new Color(0,0,0,0.35f));
            BuildPlatform(_root.transform, _enemy.rect.anchorMin,  new Color(0,0,0,0.35f));

            // Banner
            _statusBanner = MakeText(_root.transform, "Banner", "",
                new Vector2(0.5f, 0.94f), new Vector2(900, 60),
                34, FontStyles.Bold, CREAM);
            _statusBanner.alignment = TextAlignmentOptions.Center;
            _statusBanner.outlineWidth = 0.18f; _statusBanner.outlineColor = Color.black;
            _statusBanner.richText = true;
            _statusBanner.raycastTarget = false;

            // Combo counter (top-left)
            _comboTxt = MakeText(_root.transform, "Combo", "",
                new Vector2(0.06f, 0.86f), new Vector2(220, 60),
                42, FontStyles.Bold, GOLD);
            _comboTxt.alignment = TextAlignmentOptions.Left;
            _comboTxt.outlineWidth = 0.30f; _comboTxt.outlineColor = Color.black;
            _comboTxt.raycastTarget = false;

            // Limit Break bar (bottom-left)
            BuildLimitBreakBar();

            // Action menu (bottom-right)
            _actionMenu = BuildActionMenu();
            _skillMenu = BuildSkillMenu();
            _skillMenu.SetActive(false);

            // Full-screen flash overlay (always present, alpha animated)
            _fullScreenFlash = MakeImage(_root.transform, "FSFlash", new Color(1,1,1,0));
            var fsr = _fullScreenFlash.GetComponent<RectTransform>();
            fsr.anchorMin = Vector2.zero; fsr.anchorMax = Vector2.one;
            fsr.offsetMin = Vector2.zero; fsr.offsetMax = Vector2.zero;
            _fullScreenFlash.raycastTarget = false;
            _fullScreenFlash.transform.SetAsLastSibling();

            RefreshHP(_player); RefreshHP(_enemy);
            RefreshStatusIcons(_player); RefreshStatusIcons(_enemy);
            RefreshLimitBreak();
            RefreshCombo();
        }

        private static void BuildParallaxBackground(Biome biome)
        {
            // Layer 1 — sky (back, slow drift)
            string skyPath = $"Assets/FantasyMaps/_PNG/{biome.mapNum}/layers/l1-sky.png";
            var sky = MakeBgLayer(_root.transform, "BG_Sky", skyPath, biome.skyTint);
            _skyLayer = sky.GetComponent<RectTransform>();
            _skyHome = _skyLayer.anchoredPosition;

            // Layer 2 — ground (mid, static)
            string grPath = $"Assets/FantasyMaps/_PNG/{biome.mapNum}/layers/l2-ground.png";
            var gr = MakeBgLayer(_root.transform, "BG_Ground", grPath, biome.groundTint);
            _groundLayer = gr.GetComponent<RectTransform>();

            // Layer 3 — decorations (front, gentle bob)
            string decoPath = $"Assets/FantasyMaps/_PNG/{biome.mapNum}/layers/l3-decoartions.png"; // (typo in source)
            var deco = MakeBgLayer(_root.transform, "BG_Deco", decoPath, biome.decoTint);
            _decoLayer = deco.GetComponent<RectTransform>();
            _decoHome = _decoLayer.anchoredPosition;

            // Fallback solid color in case layer images fail to load
            // (Image.sprite stays null → renders as a solid color tinted overlay)
        }

        private static Image MakeBgLayer(Transform parent, string name, string spritePath, Color tint)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-100, -100); rt.offsetMax = new Vector2(100, 100); // slight overscan for parallax drift
            var img = go.GetComponent<Image>();
            img.color = tint;
            img.preserveAspect = false;
            img.raycastTarget = false;
            #if UNITY_EDITOR
            var sp = Sparq.Core.SpriteLoader.Load(spritePath);
            if (sp != null) img.sprite = sp;
            #endif
            return img;
        }

        private static IEnumerator ParallaxDrift()
        {
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

        private static IEnumerator SpawnAmbientParticles(Biome biome)
        {
            // Spawn slowly. Parented to a particle layer behind the combatants.
            var layer = new GameObject("ParticleLayer", typeof(RectTransform));
            layer.transform.SetParent(_root.transform, false);
            var lrt = layer.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            // Behind the combatants but in front of background
            layer.transform.SetSiblingIndex(4);

            float interval = biome.particleKind == "embers" ? 0.10f
                           : biome.particleKind == "mist"   ? 0.20f
                           : biome.particleKind == "snow"   ? 0.15f
                           : 0.18f;
            while (_root != null)
            {
                yield return new WaitForSeconds(interval);
                if (_root == null) yield break;
                SpawnParticle(layer.transform, biome);
            }
        }

        private static void SpawnParticle(Transform parent, Biome biome)
        {
            var go = new GameObject("p", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(Random.value, biome.particleKind == "snow" ? 1.05f : 0.05f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(14, 14);
            var img = go.AddComponent<Image>();
            img.color = biome.particleColor;
            img.raycastTarget = false;
            img.sprite = MakeCircleSprite();
            _runner.StartCoroutine(ParticleAnim(rt, img, biome));
        }

        private static IEnumerator ParticleAnim(RectTransform rt, Image img, Biome biome)
        {
            float life = biome.particleKind == "mist" ? 4.5f : 3.5f;
            float t = 0;
            float driftX = Random.Range(-0.5f, 0.5f);
            float driftY = biome.particleKind == "snow" ? -1f : Random.Range(0.4f, 1.0f);
            Color baseC = img.color;
            while (t < life && rt != null)
            {
                float p = t / life;
                rt.anchorMin = rt.anchorMax = rt.anchorMin + new Vector2(driftX * Time.deltaTime * 0.05f, driftY * Time.deltaTime * 0.05f);
                img.color = new Color(baseC.r, baseC.g, baseC.b, baseC.a * (1f - Mathf.Pow(p, 2)));
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) Object.Destroy(rt.gameObject);
        }

        private static void BuildPlatform(Transform parent, Vector2 anchor, Color color)
        {
            var go = new GameObject("Platform", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(anchor.x, anchor.y - 0.10f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280, 36);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            img.sprite = MakeEllipseSprite();
        }

        private static RectTransform MakeCombatantSlot(Transform parent, string name, string spritePath,
            Vector2 anchorPivot, Vector2 size, out Image portrait, out Transform fxLayer, out Transform statusRow)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorPivot; rt.anchorMax = anchorPivot; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            var imgGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            imgGO.transform.SetParent(go.transform, false);
            var irt = imgGO.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
            portrait = imgGO.GetComponent<Image>();
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(spritePath))
            {
                var sp = Sparq.Core.SpriteLoader.Load(spritePath);
                if (sp != null) portrait.sprite = sp;
            }
            #endif

            var fxGO = new GameObject("FX", typeof(RectTransform));
            fxGO.transform.SetParent(go.transform, false);
            var frt = fxGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
            fxLayer = fxGO.transform;

            var stGO = new GameObject("Status", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stGO.transform.SetParent(go.transform, false);
            var srt = stGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 0);
            srt.offsetMin = new Vector2(8, 8);  srt.offsetMax = new Vector2(-8, 48);
            var hlg = stGO.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            statusRow = stGO.transform;

            return rt;
        }

        // ───────── HP bar with class tint, lerp animation, incoming preview, shield overlay ─────────
        private static void BuildHpBar(Transform parent, Combatant c, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;

            // Backplate (dark)
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.12f, 0.95f);
            bg.GetComponent<Image>().raycastTarget = false;

            // Yellow incoming preview (sits between bg and fill)
            var preview = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            preview.transform.SetParent(go.transform, false);
            var prt = preview.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0.15f); prt.anchorMax = new Vector2(1, 0.85f);
            prt.offsetMin = new Vector2(4, 0); prt.offsetMax = new Vector2(-4, 0);
            c.hpPreview = preview.GetComponent<Image>();
            c.hpPreview.color = INCOMING_Y;
            c.hpPreview.raycastTarget = false;
            c.hpPreview.type = Image.Type.Filled;
            c.hpPreview.fillMethod = Image.FillMethod.Horizontal;
            c.hpPreview.fillAmount = 0;

            // The actual HP fill via a Slider for smooth value control
            var slGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            slGO.transform.SetParent(go.transform, false);
            var slRT = slGO.GetComponent<RectTransform>();
            slRT.anchorMin = Vector2.zero; slRT.anchorMax = Vector2.one;
            slRT.offsetMin = Vector2.zero; slRT.offsetMax = Vector2.zero;
            var slider = slGO.GetComponent<Slider>();
            slider.minValue = 0; slider.maxValue = c.maxHp; slider.value = c.maxHp;
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;

            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(slGO.transform, false);
            var faRT = fa.GetComponent<RectTransform>();
            faRT.anchorMin = new Vector2(0, 0.15f); faRT.anchorMax = new Vector2(1, 0.85f);
            faRT.offsetMin = new Vector2(4, 0); faRT.offsetMax = new Vector2(-4, 0);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fa.transform, false);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            c.hpFillImg = fill.GetComponent<Image>();
            c.hpFillImg.color = c.hpFill;
            c.hpFillImg.raycastTarget = false;
            slider.fillRect = fRT;
            c.hpBar = slider;

            // Label text on top
            c.hpTxt = MakeText(go.transform, "Txt", $"{c.name}  {c.maxHp}/{c.maxHp}",
                new Vector2(0.5f, 0.5f), size, 22, FontStyles.Bold, CREAM);
            c.hpTxt.alignment = TextAlignmentOptions.Center;
            c.hpTxt.outlineWidth = 0.20f; c.hpTxt.outlineColor = Color.black;
            c.hpTxt.raycastTarget = false;

            // Shield overlay (icon visible when defending)
            var sh = new GameObject("Shield", typeof(RectTransform), typeof(Image));
            sh.transform.SetParent(go.transform, false);
            var shRT = sh.GetComponent<RectTransform>();
            shRT.anchorMin = new Vector2(0, 0.5f); shRT.anchorMax = new Vector2(0, 0.5f);
            shRT.pivot = new Vector2(0.5f, 0.5f);
            shRT.anchoredPosition = new Vector2(-22, 0);
            shRT.sizeDelta = new Vector2(40, 40);
            c.shieldOverlay = sh.GetComponent<Image>();
            c.shieldOverlay.color = new Color(0.55f, 0.85f, 1f, 1f);
            c.shieldOverlay.sprite = MakeCircleSprite();
            c.shieldOverlay.raycastTarget = false;
            c.shieldOverlay.gameObject.SetActive(false);
        }

        private static IEnumerator LerpHpBars()
        {
            while (_root != null)
            {
                LerpOne(_player);
                LerpOne(_enemy);
                yield return null;
            }
        }
        private static void LerpOne(Combatant c)
        {
            if (c == null || c.hpBar == null) return;
            float target = c.hp;
            c.hpDisplayed = Mathf.MoveTowards(c.hpDisplayed, target, Mathf.Max(15, c.maxHp) * Time.deltaTime * 1.4f);
            c.hpBar.value = c.hpDisplayed;
            float ratio = c.maxHp > 0 ? c.hp / (float)c.maxHp : 0;
            if (c.hpFillImg != null)
            {
                Color baseTint = c.hpFill;
                Color cur = ratio < 0.30f ? HP_RED
                          : ratio < 0.60f ? Color.Lerp(baseTint, HP_AMBER, 0.65f)
                          : baseTint;
                c.hpFillImg.color = cur;
            }
            if (c.hpTxt != null) c.hpTxt.text = $"{c.name}  {Mathf.CeilToInt(c.hpDisplayed)}/{c.maxHp}";
        }

        private static void RefreshHP(Combatant c)
        {
            if (c.hpBar != null) { c.hpBar.maxValue = c.maxHp; }
        }

        private static void RefreshHpPreview(Combatant c)
        {
            if (c.hpPreview == null) return;
            float startRatio = c.maxHp > 0 ? Mathf.Max(0, c.hp - c.hpIncoming) / (float)c.maxHp : 0;
            float endRatio   = c.maxHp > 0 ? c.hp / (float)c.maxHp : 0;
            // The preview sits as a band on the bar covering [startRatio, endRatio]
            // We fake it by setting fillAmount = endRatio and origin from startRatio.
            // Simpler: just set fillAmount; visually it's a yellow band overlapping the green.
            c.hpPreview.fillAmount = c.hpIncoming > 0 ? endRatio : 0;
            // Show only the right portion of the bar (from startRatio onward) by anchoring? Skip — fillAmount alone is good enough.
        }

        private static void RefreshShield(Combatant c)
        {
            if (c.shieldOverlay == null) return;
            c.shieldOverlay.gameObject.SetActive(c.defendTurns > 0);
        }

        private static void RefreshStatusIcons(Combatant c)
        {
            if (c.statusRow == null) return;
            for (int i = c.statusRow.childCount - 1; i >= 0; i--) Object.Destroy(c.statusRow.GetChild(i).gameObject);
            void AddIcon(string label, Color col)
            {
                var go = new GameObject($"S_{label}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(c.statusRow, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(36, 36);
                var img = go.GetComponent<Image>();
                img.color = col;
                img.sprite = MakeCircleSprite();
                img.raycastTarget = false;
                var tm = MakeText(go.transform, "T", label,
                    Vector2.zero, Vector2.zero, 18, FontStyles.Bold, Color.white);
                tm.alignment = TextAlignmentOptions.Center;
                var trt = tm.rectTransform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                tm.outlineWidth = 0.20f; tm.outlineColor = Color.black;
                tm.raycastTarget = false;
            }
            if (c.stunTurns   > 0) AddIcon($"⚡{c.stunTurns}",   new Color(1f, 0.85f, 0.30f));
            if (c.defendTurns > 0) AddIcon($"🛡{c.defendTurns}", new Color(0.55f, 0.85f, 1f));
            if (c.evadeTurns  > 0) AddIcon($"💨{c.evadeTurns}",  new Color(0.65f, 0.65f, 0.75f));
            if (c.poisonTurns > 0) AddIcon($"☠{c.poisonTurns}", new Color(0.55f, 0.85f, 0.30f));
            if (c.slowTurns   > 0) AddIcon($"⏳{c.slowTurns}",   new Color(0.65f, 0.55f, 0.95f));
        }

        private static void SetBanner(string txt) { if (_statusBanner != null) _statusBanner.text = txt; }

        // ───────── Action menu (icons + colors) ─────────
        private static GameObject BuildActionMenu()
        {
            var menu = new GameObject("ActionMenu", typeof(RectTransform), typeof(Image));
            menu.transform.SetParent(_root.transform, false);
            var rt = menu.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-30, 30);
            rt.sizeDelta = new Vector2(380, 360);
            menu.GetComponent<Image>().color = CARD_BG;

            var v = menu.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 16, 16);
            v.spacing = 10;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            AddIconButton(menu.transform, "⚔",  "ATTACK", " (1)", new Color(0.85f, 0.55f, 0.30f), () => ChooseSkill(0));
            AddIconButton(menu.transform, "✦",  "SKILLS", " (2)", new Color(0.55f, 0.65f, 0.95f), () => ShowSkillMenu(true));
            AddIconButton(menu.transform, "⚗",  "ITEM",   " (3)", new Color(0.45f, 0.85f, 0.55f), () => UseItem());
            AddIconButton(menu.transform, "➤",  "FLEE",   " (4)", new Color(0.55f, 0.55f, 0.65f), () => Flee());
            return menu;
        }

        private static GameObject BuildSkillMenu()
        {
            var menu = new GameObject("SkillMenu", typeof(RectTransform), typeof(Image));
            menu.transform.SetParent(_root.transform, false);
            var rt = menu.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-30, 30);
            rt.sizeDelta = new Vector2(440, 420);
            menu.GetComponent<Image>().color = CARD_BG;

            var v = menu.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(14, 14, 14, 14);
            v.spacing = 8;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            _skillButtons.Clear();
            for (int i = 1; i < _player.kit.Length; i++)
            {
                int idx = i;
                var sk = _player.kit[idx];
                _skillButtons.Add(AddSkillButton(menu.transform, sk, () => ChooseSkill(idx)));
            }
            AddIconButton(menu.transform, "←", "BACK", "", new Color(0.40f, 0.30f, 0.50f), () => ShowSkillMenu(false));
            return menu;
        }

        private static Button AddIconButton(Transform parent, string icon, string label, string suffix, Color color, System.Action onClick)
        {
            var go = new GameObject($"Btn_{label}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 70;
            go.GetComponent<Image>().color = color;
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var tm = MakeText(go.transform, "Lbl", $"<size=110%>{icon}</size>  {label}<size=70%>{suffix}</size>",
                new Vector2(0.5f, 0.5f), Vector2.zero, 26, FontStyles.Bold, Color.white);
            tm.alignment = TextAlignmentOptions.Center;
            tm.richText = true;
            var trt = tm.rectTransform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            tm.outlineWidth = 0.20f; tm.outlineColor = Color.black;
            tm.raycastTarget = false;
            return btn;
        }

        private static Button AddSkillButton(Transform parent, Skill sk, System.Action onClick)
        {
            var go = new GameObject($"Sk_{sk.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 78;
            go.GetComponent<Image>().color = sk.flashColor * 0.6f + new Color(0,0,0,1);
            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            var inner = new GameObject("Inner", typeof(RectTransform), typeof(VerticalLayoutGroup));
            inner.transform.SetParent(go.transform, false);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(14, 4); irt.offsetMax = new Vector2(-14, -4);
            var v = inner.GetComponent<VerticalLayoutGroup>();
            v.spacing = 0; v.childAlignment = TextAnchor.MiddleLeft;

            string icon = sk.vfx switch { "slash"=>"✦", "smash"=>"✺", "arrow"=>"➤", "bolt"=>"⚡",
                "fire"=>"●", "shadow"=>"◆", "shield"=>"🛡", "smoke"=>"☁", _=>"✦" };
            var nameTxt = MakeText(inner.transform, "Name", $"<size=130%>{icon}</size>  {sk.name}",
                Vector2.zero, Vector2.zero, 22, FontStyles.Bold, Color.white);
            nameTxt.alignment = TextAlignmentOptions.Left;
            nameTxt.richText = true;
            nameTxt.outlineWidth = 0.18f; nameTxt.outlineColor = Color.black;
            nameTxt.raycastTarget = false;

            var descTxt = MakeText(inner.transform, "Desc", sk.desc,
                Vector2.zero, Vector2.zero, 16, FontStyles.Italic, new Color(1,1,1,0.85f));
            descTxt.alignment = TextAlignmentOptions.Left;
            descTxt.raycastTarget = false;
            return btn;
        }

        private static void RefreshSkillButtons()
        {
            if (_skillButtons == null) return;
            for (int i = 1; i < _player.kit.Length && i - 1 < _skillButtons.Count; i++)
            {
                var sk = _player.kit[i];
                var btn = _skillButtons[i - 1];
                if (btn == null) continue;
                int cd = _player.cd.TryGetValue(sk.id, out int v) ? v : 0;
                bool ready = cd <= 0;
                btn.interactable = ready;
                var img = btn.GetComponent<Image>();
                img.color = ready ? sk.flashColor * 0.6f + new Color(0,0,0,1) : CD_RED;
                var nameTxt = btn.transform.Find("Inner/Name")?.GetComponent<TMP_Text>();
                if (nameTxt != null)
                {
                    string icon = sk.vfx switch { "slash"=>"✦", "smash"=>"✺", "arrow"=>"➤", "bolt"=>"⚡",
                        "fire"=>"●", "shadow"=>"◆", "shield"=>"🛡", "smoke"=>"☁", _=>"✦" };
                    nameTxt.text = ready ? $"<size=130%>{icon}</size>  {sk.name}"
                                         : $"<size=130%>{icon}</size>  {sk.name}  <size=70%><color=#FFCCCC>CD {cd}</color></size>";
                }
            }
        }

        private static void ShowActionMenu(bool show) { if (_actionMenu != null) _actionMenu.SetActive(show); }
        private static void ShowSkillMenu(bool show)
        {
            if (_skillMenu != null) _skillMenu.SetActive(show);
            if (_actionMenu != null) _actionMenu.SetActive(!show);
            if (show) RefreshSkillButtons();
        }

        private static void ChooseSkill(int idx)
        {
            if (idx > 0)
            {
                var sk = _player.kit[idx];
                if (_player.cd.ContainsKey(sk.id) && _player.cd[sk.id] > 0)
                { SetBanner($"<color=#FFAAAA>{sk.name} on cooldown</color>"); PlayAudio(SFX_DENIED, 0.6f); return; }
            }
            PlayAudio(SFX_CONFIRM, 0.55f);
            _pickedSkillIdx = idx;
            _awaitingInput = false;
        }

        private static void UseItem()
        {
            int heal = 20;
            int actually = Mathf.Min(heal, _player.maxHp - _player.hp);
            if (actually <= 0) { SetBanner("Already at full HP."); return; }
            _player.hp += actually;
            FloatNumber(_player, $"+{actually}", new Color(0.55f, 1f, 0.55f), 1.4f);
            SetBanner("You sip a potion.");
            _pickedSkillIdx = -2;
            _awaitingInput = false;
        }

        private static void Flee()
        {
            SetBanner("You fled!");
            PlayAudio(SFX_FLEE, 0.85f);
            if (_music != null) _music.Stop();
            _pickedSkillIdx = -3;
            _awaitingInput = false;
            _runner.StartCoroutine(FleeAndClose());
        }
        private static IEnumerator FleeAndClose() { yield return new WaitForSeconds(1.0f); Hide(); }

        // ───────── Limit Break bar ─────────
        private static void BuildLimitBreakBar()
        {
            var go = new GameObject("LimitBreak", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(30, 30);
            rt.sizeDelta = new Vector2(420, 70);
            go.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.12f, 0.95f);
            _lbBtn = go.GetComponent<Button>();
            _lbBtn.onClick.AddListener(OnLimitBreakClicked);
            _lbBtn.interactable = false;

            // Glow ring (visible when full)
            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(go.transform, false);
            var grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = Vector2.zero; grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(-8, -8); grt.offsetMax = new Vector2(8, 8);
            _lbGlow = glow.GetComponent<Image>();
            _lbGlow.color = new Color(1f, 0.55f, 0.20f, 0f);
            _lbGlow.raycastTarget = false;

            var slGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            slGO.transform.SetParent(go.transform, false);
            var slRT = slGO.GetComponent<RectTransform>();
            slRT.anchorMin = Vector2.zero; slRT.anchorMax = Vector2.one;
            slRT.offsetMin = new Vector2(8, 8); slRT.offsetMax = new Vector2(-8, -8);
            _lbBar = slGO.GetComponent<Slider>();
            _lbBar.minValue = 0; _lbBar.maxValue = 100; _lbBar.value = 0;
            _lbBar.transition = Selectable.Transition.None;
            _lbBar.interactable = false;

            var fa = new GameObject("FillArea", typeof(RectTransform));
            fa.transform.SetParent(slGO.transform, false);
            var faRT = fa.GetComponent<RectTransform>();
            faRT.anchorMin = Vector2.zero; faRT.anchorMax = Vector2.one;
            faRT.offsetMin = Vector2.zero; faRT.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fa.transform, false);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = Vector2.zero; fRT.offsetMax = Vector2.zero;
            _lbFill = fill.GetComponent<Image>();
            _lbFill.color = new Color(0.85f, 0.30f, 0.20f);
            _lbFill.raycastTarget = false;
            _lbBar.fillRect = fRT;

            var lbl = MakeText(go.transform, "Lbl", "LIMIT BREAK",
                new Vector2(0.5f, 0.5f), Vector2.zero, 22, FontStyles.Bold, CREAM);
            lbl.alignment = TextAlignmentOptions.Center;
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lbl.outlineWidth = 0.30f; lbl.outlineColor = Color.black;
            lbl.raycastTarget = false;
        }

        private static void RefreshLimitBreak()
        {
            if (_lbBar == null) return;
            _lbBar.value = _limitBreak;
            bool full = _limitBreak >= 100f;
            if (_lbBtn != null) _lbBtn.interactable = full;
            if (_lbFill != null)
                _lbFill.color = full ? new Color(1f, 0.55f, 0.20f) : new Color(0.65f, 0.30f, 0.20f);
            if (_lbGlow != null)
                _lbGlow.color = new Color(1f, 0.55f, 0.20f, full ? 0.45f : 0f);
        }

        private static void OnLimitBreakClicked()
        {
            if (_limitBreak < 100f || !_awaitingInput) return;
            _pickedSkillIdx = -10;
            _awaitingInput = false;
        }

        // ───────── Combo counter ─────────
        private static void RefreshCombo()
        {
            if (_comboTxt == null) return;
            _comboTxt.text = _comboCount > 1 ? $"<size=70%>COMBO</size>  ×{_comboCount}" : "";
        }
        private static void ShowComboBurst()
        {
            if (_comboTxt == null) return;
            _runner.StartCoroutine(ComboPulseAnim());
        }
        private static IEnumerator ComboPulseAnim()
        {
            var rt = _comboTxt.rectTransform;
            float t = 0;
            while (t < 0.25f)
            {
                float p = t / 0.25f;
                rt.localScale = Vector3.one * Mathf.Lerp(1.4f, 1.0f, p);
                t += Time.deltaTime; yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ═══════════════════════════════════════════════════════════════════
        // VFX
        // ═══════════════════════════════════════════════════════════════════
        private static void FlashPortrait(Combatant c, Color tint)
        {
            if (c.portrait == null) return;
            _runner.StartCoroutine(FlashRoutine(c.portrait, tint));
        }
        private static IEnumerator FlashRoutine(Image img, Color tint)
        {
            var orig = img.color;
            img.color = tint;
            yield return new WaitForSeconds(0.10f);
            img.color = orig;
        }

        private static void ShakeOnce(RectTransform rt, float magnitude, float duration)
        {
            if (rt == null) return;
            _runner.StartCoroutine(ShakeRoutine(rt, magnitude, duration));
        }
        private static IEnumerator ShakeRoutine(RectTransform rt, float mag, float dur)
        {
            Vector2 home = rt.anchoredPosition;
            float t = 0;
            while (t < dur && rt != null)
            {
                rt.anchoredPosition = home + new Vector2(Random.Range(-mag, mag), Random.Range(-mag, mag));
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = home;
        }

        private static void ActorPulse(Combatant c)
        {
            if (c.rect == null) return;
            _runner.StartCoroutine(PulseScale(c.rect, 1.08f, 0.18f));
        }
        private static IEnumerator PulseScale(RectTransform rt, float peak, float dur)
        {
            float t = 0;
            while (t < dur && rt != null)
            {
                float p = t / dur;
                float s = 1f + (peak - 1f) * Mathf.Sin(p * Mathf.PI);
                rt.localScale = new Vector3(s, s, 1f);
                t += Time.deltaTime;
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private static void FullScreenFlash(Color c, float dur)
        {
            if (_fullScreenFlash == null) return;
            _runner.StartCoroutine(FlashFS(c, dur));
        }
        private static IEnumerator FlashFS(Color c, float dur)
        {
            float t = 0;
            while (t < dur && _fullScreenFlash != null)
            {
                float p = t / dur;
                float a = c.a * (1f - p);
                _fullScreenFlash.color = new Color(c.r, c.g, c.b, a);
                t += Time.deltaTime;
                yield return null;
            }
            if (_fullScreenFlash != null) _fullScreenFlash.color = new Color(c.r, c.g, c.b, 0);
        }

        private static void FloatNumber(Combatant c, string text, Color color, float scale)
        {
            FloatArcNumber(c, text, color, scale, false);
        }

        private static void FloatArcNumber(Combatant c, string text, Color color, float scale, bool crit)
        {
            if (c == null || c.fxLayer == null) return;
            var go = new GameObject("Float", typeof(RectTransform));
            go.transform.SetParent(c.fxLayer, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.7f); rt.anchorMax = new Vector2(0.5f, 0.7f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(260, 90);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = Mathf.RoundToInt(58 * scale);
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = color;
            tm.outlineWidth = 0.30f;
            tm.outlineColor = Color.black;
            tm.raycastTarget = false;
            tm.richText = true;
            _runner.StartCoroutine(FloatNumberAnim(rt, tm, crit));
        }

        private static IEnumerator FloatNumberAnim(RectTransform rt, TMP_Text tm, bool crit)
        {
            float dur = crit ? 1.3f : 0.95f;
            float t = 0;
            Vector2 home = rt.anchoredPosition;
            float driftX = Random.Range(-30f, 30f);
            Color baseColor = tm.color;
            Vector3 baseScale = rt.localScale;
            while (t < dur)
            {
                float p = t / dur;
                float arcY = 130f * Mathf.Sin(p * Mathf.PI * 0.5f);
                rt.anchoredPosition = home + new Vector2(driftX * p, arcY);
                rt.localScale = baseScale * Mathf.Lerp(1.4f, 1.0f, p);
                tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, p < 0.7f ? 1f : 1f - (p - 0.7f) / 0.3f);
                t += Time.deltaTime;
                yield return null;
            }
            Object.Destroy(rt.gameObject);
        }

        // ───────── Hotkeys 1-4 ─────────
        private static IEnumerator InputHotkeys()
        {
            while (_root != null)
            {
                if (_awaitingInput && _actionMenu != null && _actionMenu.activeSelf)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseSkill(0);
                    else if (Input.GetKeyDown(KeyCode.Alpha2)) ShowSkillMenu(true);
                    else if (Input.GetKeyDown(KeyCode.Alpha3)) UseItem();
                    else if (Input.GetKeyDown(KeyCode.Alpha4)) Flee();
                }
                else if (_awaitingInput && _skillMenu != null && _skillMenu.activeSelf)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1) && _player.kit.Length > 1) ChooseSkill(1);
                    else if (Input.GetKeyDown(KeyCode.Alpha2) && _player.kit.Length > 2) ChooseSkill(2);
                    else if (Input.GetKeyDown(KeyCode.Alpha3) && _player.kit.Length > 3) ChooseSkill(3);
                    else if (Input.GetKeyDown(KeyCode.Escape)) ShowSkillMenu(false);
                }
                yield return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // WIN / LOSE
        // ═══════════════════════════════════════════════════════════════════
        private static IEnumerator Victory()
        {
            SetBanner($"<color=#FFD466>VICTORY!</color>  +{_xpReward} XP  +{_goldReward} gold");
            // Stop battle BGM so the triumph sting is clean
            if (_music != null) _music.Stop();
            PlayAudio(SFX_DEATH, 0.7f);
            yield return new WaitForSeconds(0.20f);
            PlayAudio(MS_VICTORY, 0.85f);
            FullScreenFlash(new Color(1f, 0.85f, 0.30f, 0.50f), 0.6f);
            var d = Sparq.Core.SaveService.Data;
            if (d != null)
            {
                d.sparqCoins += _goldReward;
                // Route XP through the canonical curve so leveling stays in sync
                // with every other source (battle/loot/AFK/quests).
                Progression.GrantXp(d, _xpReward);
                Sparq.Core.SaveService.Save();
            }
            yield return new WaitForSeconds(2.4f);
            Hide();
        }

        private static IEnumerator Defeat()
        {
            SetBanner("<color=#FF6677>DEFEAT…</color>");
            if (_music != null) _music.Stop();
            PlayAudio(MS_DEFEAT, 0.85f);
            FullScreenFlash(new Color(0.55f, 0.10f, 0.10f, 0.55f), 0.7f);
            yield return new WaitForSeconds(2.0f);
            Hide();
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════
        private static Sprite ResolveHeroSprite()
        {
            var hero = HeroClassResolver.Resolve();
            if (hero != null && !string.IsNullOrEmpty(hero.idleBase))
                return Sparq.Core.SpriteLoader.Load(hero.idleBase + "000.png");
            return null;
        }

        private static void PlayAudio(string assetPath, float volume)
        {
            if (_audio == null) return;
            #if UNITY_EDITOR
            var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip != null) _audio.PlayOneShot(clip, volume);
            #endif
        }

        private static void PlayWeaponAudio(string vfxKind, bool crit)
        {
            // Pick swing/cast sound based on VFX kind, then layer a flesh impact
            string castPath = vfxKind switch {
                "slash"  => SFX_SLASH,
                "smash"  => SFX_EARTH,
                "arrow"  => SFX_WIND,         // bowstring → wind whoosh
                "bolt"   => SFX_THUNDER,
                "fire"   => SFX_FIRE,
                "shadow" => SFX_POISON,        // dark/tox vibe
                "shield" => SFX_BLOCK,
                "smoke"  => SFX_WIND,
                _        => SFX_SLASH,
            };
            PlayAudio(castPath, crit ? 0.95f : 0.75f);

            // Layer an impact-flesh hit half a beat later for melee/physical feel
            if (vfxKind == "slash" || vfxKind == "smash" || vfxKind == "shadow" || vfxKind == "arrow")
            {
                string fleshPath = Random.value < 0.5f ? SFX_FLESH : SFX_FLESH2;
                _runner.StartCoroutine(DelayedAudio(fleshPath, 0.06f, crit ? 0.85f : 0.65f));
            }

            if (crit) try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Crit); } catch {}
        }

        private static IEnumerator DelayedAudio(string path, float delay, float volume)
        {
            yield return new WaitForSeconds(delay);
            PlayAudio(path, volume);
        }

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
            if (size != Vector2.zero) { rt.anchorMin = anchor; rt.anchorMax = anchor; rt.sizeDelta = size; rt.pivot = new Vector2(0.5f, 0.5f); }
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.fontStyle = style;
            tm.color = color;
            return tm;
        }

        // Procedural circle sprite (cached)
        private static Sprite _circleSp, _ellipseSp;
        private static Sprite MakeCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
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
            _circleSp = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
            return _circleSp;
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

        private class RunnerMB : MonoBehaviour { }
    }
}
