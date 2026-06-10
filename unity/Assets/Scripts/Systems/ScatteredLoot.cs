using System.Collections;
using UnityEngine;

namespace Sparq.Systems
{
    /// <summary>
    /// Scatters tappable goodies across the home scene — chests, butterflies,
    /// glowing runes, easter-egg orbs. Tap to collect → reward (coins/XP/secret).
    /// Respawns after random delay so the world always has something to explore.
    /// </summary>
    public class ScatteredLoot : MonoBehaviour
    {
        public static ScatteredLoot Instance { get; private set; }

        public enum LootKind { Chest, Butterfly, Rune, EasterEgg, GoldenLeaf }

        [System.Serializable]
        public struct LootSpawn
        {
            public LootKind kind;
            public Vector2  position;
            public float    spawnDelay;
        }

        // Easter egg quotes that appear when you tap one
        private static readonly string[] EASTER_EGGS = {
            "🌟 You hear Karu humming...",
            "🍀 A four-leaf clover! Lucky day.",
            "📜 Una whispers: 'Focus is a muscle.'",
            "✨ A shooting star streaks past.",
            "🐛 A glowworm lights your path.",
            "🌙 The forest remembers you.",
            "🎵 A faint melody drifts on the wind.",
            "💜 Volt is watching you...",
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Spawn initial loot scattered across the home scene
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            // Initial wave
            yield return new WaitForSeconds(1.5f);
            for (int i = 0; i < 4; i++)
            {
                SpawnRandom();
                yield return new WaitForSeconds(0.4f);
            }

            // Respawn loop — every 30-90s
            while (true)
            {
                float delay = Random.Range(30f, 90f);
                yield return new WaitForSeconds(delay);
                if (CountActive() < 6) SpawnRandom();
            }
        }

        public void SpawnRandom()
        {
            // Random position in the home scene foreground area (where forest is)
            Vector3 pos = new Vector3(
                Random.Range(-4.5f, 4.5f),
                Random.Range(-3.0f, -1.0f),
                0f);

            LootKind kind = (LootKind)Random.Range(0, System.Enum.GetValues(typeof(LootKind)).Length);
            Spawn(kind, pos);
        }

        public void Spawn(LootKind kind, Vector3 position)
        {
            var go = new GameObject($"Loot_{kind}");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 8;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            var pickup = go.AddComponent<LootPickup>();
            pickup.kind = kind;

            // Visual setup per kind
            switch (kind)
            {
                case LootKind.Chest:
                    sr.color = new Color(1f, 0.85f, 0.3f);
                    go.transform.localScale = Vector3.one * 0.25f;
                    pickup.label = "💰 CHEST";
                    pickup.coinReward = Random.Range(20, 80);
                    pickup.xpReward = Random.Range(2, 8);
                    break;
                case LootKind.Butterfly:
                    sr.color = new Color(0.9f, 0.5f, 1f);
                    go.transform.localScale = Vector3.one * 0.18f;
                    pickup.label = "🦋 +1 LUCK";
                    pickup.coinReward = 5;
                    pickup.xpReward = 1;
                    pickup.flutter = true;
                    break;
                case LootKind.Rune:
                    sr.color = new Color(0.4f, 1f, 0.8f);
                    go.transform.localScale = Vector3.one * 0.22f;
                    pickup.label = "✨ RUNE";
                    pickup.coinReward = 50;
                    pickup.xpReward = 5;
                    pickup.glow = true;
                    break;
                case LootKind.EasterEgg:
                    sr.color = new Color(1f, 0.85f, 1f);
                    go.transform.localScale = Vector3.one * 0.2f;
                    pickup.label = "🌟 SECRET";
                    pickup.coinReward = 10;
                    pickup.xpReward = 0;
                    pickup.glow = true;
                    pickup.easterEggMessage = EASTER_EGGS[Random.Range(0, EASTER_EGGS.Length)];
                    break;
                case LootKind.GoldenLeaf:
                    sr.color = new Color(1f, 0.95f, 0.4f);
                    go.transform.localScale = Vector3.one * 0.16f;
                    pickup.label = "🍂 GOLDEN LEAF";
                    pickup.coinReward = 30;
                    pickup.xpReward = 3;
                    pickup.flutter = true;
                    break;
            }

            // Make a simple square sprite procedurally — replaced with art later
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int x = 0; x < 64; x++)
            for (int y = 0; y < 64; y++)
            {
                float dx = x - 32f, dy = y - 32f;
                float dist = Mathf.Sqrt(dx*dx + dy*dy);
                float a = Mathf.Clamp01(1f - (dist / 28f));
                a = Mathf.Pow(a, 1.3f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);

            // Pulsing glow component to draw attention
            go.AddComponent<LootIdleAnim>();
        }

        private int CountActive()
        {
            return GameObject.FindGameObjectsWithTag("Untagged").Length; // rough fallback
        }
    }

    // ── Pickup behavior ──────────────────────────────────────────────────────
    public class LootPickup : MonoBehaviour
    {
        public ScatteredLoot.LootKind kind;
        public string label;
        public int coinReward;
        public int xpReward;
        public bool flutter;
        public bool glow;
        public string easterEggMessage;

        private bool _claimed;

        private void OnMouseDown() { Claim(); }

        public void Claim()
        {
            if (_claimed) return;
            _claimed = true;

            var data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.sparqCoins += coinReward;
                Progression.GrantXp(data, xpReward);   // single canonical curve
                Sparq.Core.SaveService.ScheduleSave();
            }

            // Sound
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin,
                pitch: Random.Range(0.95f, 1.15f));

            // Floater message
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                string text = string.IsNullOrEmpty(easterEggMessage)
                    ? $"{label}\n+{coinReward} 💰  +{xpReward} XP"
                    : easterEggMessage;
                Sparq.UI.XPFloater.Spawn(canvas.transform, transform.position,
                    text, new Color(1f, 0.95f, 0.4f));
            }

            Destroy(gameObject);
        }
    }

    // ── Idle bobbing/glow ────────────────────────────────────────────────────
    public class LootIdleAnim : MonoBehaviour
    {
        private Vector3 _basePos;
        private float _t;
        private SpriteRenderer _sr;
        private Color _baseColor;

        private void Awake()
        {
            _basePos = transform.position;
            _t = Random.value * 5f;
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _baseColor = _sr.color;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            transform.position = _basePos + new Vector3(0, Mathf.Sin(_t * 1.8f) * 0.08f, 0);
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 1.2f) * 8f);
            if (_sr != null)
            {
                float pulse = (Mathf.Sin(_t * 3f) + 1f) * 0.5f;
                _sr.color = Color.Lerp(_baseColor, Color.white, pulse * 0.4f);
            }
        }
    }
}
