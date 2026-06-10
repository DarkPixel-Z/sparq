using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sparq.Systems
{
    /// <summary>
    /// Drives the tap-battle loop on Karu (Bear).
    ///   Tap Karu → bear flips to attack face, lunges forward, slash flashes on Volt's card,
    ///              Volt's fitchXP rises (pushing him toward defeat), SFX fires.
    /// Lives on the Karu GameObject.
    /// Implements both IPointerClickHandler AND OnMouseDown for max compatibility.
    /// </summary>
    public class TapBattle : MonoBehaviour, IPointerClickHandler
    {
        [Header("Damage")]
        [Tooltip("How much fitchXP each tap deals (drains Volt's HP gap).")]
        public int damagePerTap = 1;

        [Tooltip("Bonus pet-XP every N taps.")]
        public int xpEveryNTaps = 5;

        [Header("Animation")]
        public float lungeDistance = 0.25f;
        public float attackFaceDuration = 0.3f;

        [Header("Bear face sprites (auto-found)")]
        [SerializeField] private SpriteRenderer faceRenderer;
        [SerializeField] private Sprite faceIdle;
        [SerializeField] private Sprite faceAttack;
        [SerializeField] private Sprite faceHappy;

        private Vector3 _basePos;
        private bool   _attacking;

        private void Awake()
        {
            _basePos = transform.position;
            AutoBindFace();
        }

        private void AutoBindFace()
        {
            // Find face SpriteRenderer
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr != null && sr.gameObject.name.ToLower() == "face")
                {
                    faceRenderer = sr;
                    if (sr.sprite != null) faceIdle = sr.sprite;
                    break;
                }
            }
            // Find attack/happy sprites from Bear sheet
            #if UNITY_EDITOR
            var subs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(
                "Assets/2D Animal Character Pack/Sprites/Characters/Bears/Bear.png");
            foreach (var o in subs)
            {
                if (o is Sprite sp)
                {
                    if (sp.name.Equals("Face-attack", System.StringComparison.OrdinalIgnoreCase)) faceAttack = sp;
                    else if (sp.name.Equals("Face-happy",  System.StringComparison.OrdinalIgnoreCase)) faceHappy = sp;
                    else if (sp.name.Equals("Face-idle",   System.StringComparison.OrdinalIgnoreCase) && faceIdle == null) faceIdle = sp;
                }
            }
            #endif
        }

        // Mouse fallback — fires in editor with Game view focused even without EventSystem
        private void OnMouseDown() => DoAttack();

        public void OnPointerClick(PointerEventData eventData) => DoAttack();

        public void DoAttack()
        {
            if (_attacking) return;
            _attacking = true;

            var data = Sparq.Core.SaveService.Data;
            if (data == null) { _attacking = false; return; }

            // 1. Damage Volt — close the XP gap by reducing fitchXP
            data.fitchXP = Mathf.Max(0, data.fitchXP - damagePerTap);

            // 2. Karu's pet taps + bonus XP — routed through Progression so the
            // shared level curve stays in sync (raw += inflated currentXP past
            // xpToNextLevel and caused multi-level cascades on the next quest).
            data.petTaps++;
            if (xpEveryNTaps > 0 && data.petTaps % xpEveryNTaps == 0)
            {
                Progression.GrantXp(data, 1);
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
            }

            // 3. Audio — every 7th tap crits
            bool crit = (data.petTaps % 7 == 0);
            Sparq.Audio.SoundManager.Play(
                crit ? Sparq.Audio.SoundManager.Sfx.Crit : Sparq.Audio.SoundManager.Sfx.Hit,
                pitch: Random.Range(0.92f, 1.08f),
                volumeScale: crit ? 1f : 0.75f);
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Whoosh, volumeScale: 0.3f);

            // 3b. Cinematic — camera shake + micro hit-pause (bigger on crits)
            Sparq.Cinematic.CombatCinematics.Shake(crit ? 0.25f : 0.08f, crit ? 0.25f : 0.1f);
            Sparq.Cinematic.CombatCinematics.HitPause(crit ? 0.1f : 0.04f);
            if (crit)
            {
                Sparq.Cinematic.CombatCinematics.Flash(new Color(1f, 0.9f, 0.6f), 0.15f);
                damagePerTap = 3; // burst damage
            }
            else damagePerTap = 1;

            Sparq.Core.SaveService.ScheduleSave();

            // 4. Check if rival just died → advance to next monster
            if (RivalManager.Instance != null) RivalManager.Instance.CheckDefeat();

            // 5. Visual: face flip + lunge + slash on Volt
            StartCoroutine(AttackVisuals());
        }

        private IEnumerator AttackVisuals()
        {
            // -- Face: idle → attack
            if (faceRenderer != null && faceAttack != null)
                faceRenderer.sprite = faceAttack;

            // -- Lunge toward right (where Volt's card is)
            float lungeT = 0f;
            float lungeDur = 0.18f;
            while (lungeT < lungeDur)
            {
                lungeT += Time.deltaTime;
                float k = lungeT / lungeDur;
                float curve = (k < 0.5f) ? Mathf.Lerp(0f, lungeDistance, k * 2f)
                                          : Mathf.Lerp(lungeDistance, 0f, (k - 0.5f) * 2f);
                transform.position = _basePos + new Vector3(curve, 0, 0);
                yield return null;
            }
            transform.position = _basePos;

            // -- Slash VFX flash on Volt card
            SpawnSlashOnVolt();

            // Hold attack face briefly
            yield return new WaitForSeconds(attackFaceDuration);

            // Revert face
            if (faceRenderer != null && faceIdle != null)
                faceRenderer.sprite = faceIdle;

            _attacking = false;
        }

        private void SpawnSlashOnVolt()
        {
            var portrait = GameObject.Find("VoltPortrait");
            if (portrait == null) return;

            var canvas = portrait.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Build a quick procedural slash: white diagonal line that scales + fades over 0.3s
            var slashGO = new GameObject("Slash", typeof(RectTransform), typeof(Image));
            slashGO.transform.SetParent(portrait.transform, false);
            var rt = slashGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(180, 14);
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f) - 15f);
            var img = slashGO.GetComponent<Image>();
            img.color = new Color(1f, 0.95f, 0.5f, 1f);
            img.raycastTarget = false;

            // Animate via tiny coroutine driver
            var driver = slashGO.AddComponent<SlashDriver>();
            driver.duration = 0.28f;
        }

        private class SlashDriver : MonoBehaviour
        {
            public float duration = 0.3f;
            private float _t;
            private Image _img;
            private RectTransform _rt;

            private void Awake()
            {
                _img = GetComponent<Image>();
                _rt = GetComponent<RectTransform>();
            }

            private void Update()
            {
                _t += Time.deltaTime;
                float k = Mathf.Clamp01(_t / duration);
                if (_img != null)
                {
                    var c = _img.color; c.a = 1f - k; _img.color = c;
                }
                if (_rt != null)
                {
                    float scale = 1f + k * 1.2f;
                    _rt.localScale = new Vector3(scale, 1f - k * 0.5f, 1f);
                }
                if (k >= 1f) Destroy(gameObject);
            }
        }
    }
}
