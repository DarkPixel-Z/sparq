using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Briefly tints an Image red (or any color) on hit, then fades back.
    /// Punch-scale optional for extra impact.
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        private Image _img;
        private Color _baseColor;
        private RectTransform _rt;
        private Vector3 _baseScale;
        private Coroutine _running;

        private void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null) _baseColor = _img.color;
            _rt = GetComponent<RectTransform>();
            if (_rt != null) _baseScale = _rt.localScale;
        }

        public void Flash(Color flashColor, float duration = 0.18f, float punch = 1.12f)
        {
            if (_img == null) return;
            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(Run(flashColor, duration, punch));
        }

        private IEnumerator Run(Color flashColor, float duration, float punch)
        {
            float half = duration * 0.5f;
            float t = 0f;
            // Up
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                _img.color = Color.Lerp(_baseColor, flashColor, k);
                if (_rt != null) _rt.localScale = Vector3.Lerp(_baseScale, _baseScale * punch, k);
                yield return null;
            }
            // Down
            t = 0f;
            while (t < half)
            {
                t += Time.deltaTime;
                float k = t / half;
                _img.color = Color.Lerp(flashColor, _baseColor, k);
                if (_rt != null) _rt.localScale = Vector3.Lerp(_baseScale * punch, _baseScale, k);
                yield return null;
            }
            _img.color = _baseColor;
            if (_rt != null) _rt.localScale = _baseScale;
            _running = null;
        }
    }
}
