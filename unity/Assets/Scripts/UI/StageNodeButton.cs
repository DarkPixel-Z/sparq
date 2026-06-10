using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// A single stage node on the world map.
    /// Remembers its rival index; on tap, locks/unlocks based on player level,
    /// sets the current rival, and closes the map.
    /// </summary>
    public class StageNodeButton : MonoBehaviour
    {
        public int rivalIndex;

        private RectTransform _rt;
        private float _bobT;
        private Vector3 _basePos;

        private void Start()
        {
            _rt = GetComponent<RectTransform>();
            _basePos = _rt != null ? _rt.localPosition : Vector3.zero;
            _bobT = Random.value * 5f;

            // Self-wire onClick — editor-set lambdas don't persist through prefab save
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnTap);
            }
            // Visual state: Current vs Unlocked vs Locked
            RefreshVisuals();
        }

        private void Update()
        {
            if (_rt == null) return;
            _bobT += Time.deltaTime;
            // Idle bobbing — each enemy bobs at slightly different speed for organic feel
            float bob = Mathf.Sin(_bobT * 1.4f) * 6f;
            _rt.localPosition = _basePos + new Vector3(0, bob, 0);
        }

        private void RefreshVisuals()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            var r = Sparq.Systems.RivalRoster.ROSTER[rivalIndex];
            bool locked  = data.level < r.minLevel;
            bool current = data.currentRivalIndex == rivalIndex;

            var img = GetComponent<Image>();
            if (img == null) return;

            if (locked)
            {
                img.color = new Color(0.3f, 0.3f, 0.35f, 0.7f);
            }
            else if (current)
            {
                // pulse gold
                StartCoroutine(Pulse());
            }
        }

        private System.Collections.IEnumerator Pulse()
        {
            var img = GetComponent<Image>();
            var baseColor = new Color(1f, 0.85f, 0.2f);
            while (this != null && gameObject.activeInHierarchy)
            {
                float t = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
                img.color = Color.Lerp(baseColor, Color.white, t);
                yield return null;
            }
        }

        public void OnTap()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            var r = Sparq.Systems.RivalRoster.ROSTER[rivalIndex];
            if (data.level < r.minLevel)
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Hit, volumeScale: 0.25f);
                Debug.Log($"[Map] {r.name} is LOCKED — need level {r.minLevel}");
                return;
            }

            // Open stage-detail confirmation popup instead of silent dismiss
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
            StageDetailPopup.Show(rivalIndex);
        }
    }
}
