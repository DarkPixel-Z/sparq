using UnityEngine;
using UnityEngine.EventSystems;

namespace Sparq.UI
{
    /// <summary>
    /// Renders the active pet (Karu or Mochi) on screen with a gentle idle bob.
    /// Tap to earn bonus pet-XP.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class PetDisplay : MonoBehaviour, IPointerClickHandler
    {
        [Header("Sprites")]
        [SerializeField] private Sprite karuSprite;
        [SerializeField] private Sprite mochiSprite;

        [Header("Bob Animation")]
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed     = 2.5f;

        [Header("Tap Feedback")]
        [SerializeField] private float tapScaleBoost = 0.18f;
        [SerializeField] private float tapRecoverSpeed = 8f;

        private SpriteRenderer _sr;
        private Vector3        _basePos;
        private Vector3        _baseScale;
        private float          _tapScale = 0f;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _basePos = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void Start()
        {
            ApplyActivePet();
            Debug.Log($"[PetDisplay] Running on {name}. Tap me!");

            // Runtime safety — if a BoxCollider2D is present but tiny, fix it
            var col = GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            if (col.size.x < 0.1f || col.size.y < 0.1f)
            {
                col.size = new Vector2(1f, 1f);
                col.offset = Vector2.zero;
                Debug.Log("[PetDisplay] Auto-fixed tiny collider.");
            }
        }

        // Multiple ways to detect a tap — belt and suspenders
        private void OnMouseDown() => OnTap();                      // Works in editor with 2D collider
        public void OnPointerClick(PointerEventData e) => OnTap();  // Works with EventSystem + Physics2DRaycaster

        private void Update()
        {
            // Bob up and down
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.localPosition = _basePos + new Vector3(0f, yOffset, 0f);

            // Recover from tap scale
            _tapScale = Mathf.MoveTowards(_tapScale, 0f, Time.deltaTime * tapRecoverSpeed);
            transform.localScale = _baseScale * (1f + _tapScale);

            // Sanity test: press SPACE to simulate a tap
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Debug.Log("[PetDisplay] SPACE pressed — testing OnTap.");
                OnTap();
            }
        }

        public void OnTap()
        {
            Debug.Log("[PetDisplay] TAP!");
            var data = Sparq.Core.SaveService.Data;
            if (data == null)
            {
                Debug.LogWarning("[PetDisplay] No save data — is GameManager in the scene?");
                return;
            }

            data.petTaps++;
            _tapScale = tapScaleBoost;

            // Slight pitch variation keeps repeated taps from sounding robotic
            Sparq.Audio.SoundManager.Play(
                Sparq.Audio.SoundManager.Sfx.Tap,
                pitch: Random.Range(0.92f, 1.08f),
                volumeScale: 0.6f);

            // Every 5 taps award a bonus XP point — routed through Progression
            // so the canonical level curve stays in sync with every other source.
            if (data.petTaps % 5 == 0)
            {
                Sparq.Systems.Progression.GrantXp(data, 1);
                Debug.Log($"[Pet] +1 XP (taps: {data.petTaps})");
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
            }

            Sparq.Core.SaveService.ScheduleSave();

            // Trigger Feel feedbacks if an MMF_Player is attached
            TryPlayFeelFeedback();
        }

        // Generic Feel trigger — works whether or not Feel is imported.
        // Uses reflection so this script compiles even without Feel.
        private void TryPlayFeelFeedback()
        {
            var players = GetComponents<MonoBehaviour>();
            foreach (var mb in players)
            {
                if (mb == null) continue;
                var t = mb.GetType();
                if (t.Name != "MMF_Player") continue;
                var method = t.GetMethod("PlayFeedbacks", new System.Type[0]);
                if (method != null)
                {
                    method.Invoke(mb, null);
                    return;
                }
            }
        }

        /// <summary>Swap sprite based on activePet in save data.</summary>
        public void ApplyActivePet()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;
            _sr.sprite = data.activePet == "mochi" && mochiSprite != null
                ? mochiSprite
                : karuSprite;
        }
    }
}
