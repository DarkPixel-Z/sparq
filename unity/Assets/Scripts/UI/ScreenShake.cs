using System.Collections;
using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Lightweight screen shake — runs an offset on the target RectTransform.
    /// Used by BattleScene to add impact to hits/crits.
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        public static ScreenShake Instance;

        private RectTransform _rt;
        private Vector2       _basePos;
        private Coroutine     _running;

        private void Awake()
        {
            Instance = this;
            _rt = GetComponent<RectTransform>();
            _basePos = _rt.anchoredPosition;
        }

        public static void Shake(float amplitude = 14f, float duration = 0.18f)
        {
            if (Instance == null) return;
            Instance.StartShake(amplitude, duration);
        }

        public void StartShake(float amplitude, float duration)
        {
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(ShakeCo(amplitude, duration));
        }

        private IEnumerator ShakeCo(float amplitude, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float falloff = 1f - (t / duration);
                Vector2 off = new Vector2(
                    (Random.value - 0.5f) * 2f * amplitude * falloff,
                    (Random.value - 0.5f) * 2f * amplitude * falloff);
                _rt.anchoredPosition = _basePos + off;
                yield return null;
            }
            _rt.anchoredPosition = _basePos;
            _running = null;
        }
    }
}
