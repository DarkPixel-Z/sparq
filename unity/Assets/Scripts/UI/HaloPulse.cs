using UnityEngine;
using UnityEngine.UI;

namespace Sparq.UI
{
    /// <summary>
    /// Glowing halo: pulses scale + alpha sinusoidally. Used on the "current"
    /// stage node to draw the eye and signal "tap me", Top Heroes style.
    /// </summary>
    public class HaloPulse : MonoBehaviour
    {
        public float minScale = 0.95f;
        public float maxScale = 1.25f;
        public float minAlpha = 0.10f;
        public float maxAlpha = 0.50f;
        public float speed    = 1.2f;
        public float phase    = 0f;

        private Image  _img;
        private Color  _baseColor;

        private void Awake()
        {
            _img = GetComponent<Image>();
            if (_img != null) _baseColor = _img.color;
        }

        private void Update()
        {
            float t = (Time.time + phase) * speed * Mathf.PI * 2f;
            float k = (Mathf.Sin(t) * 0.5f) + 0.5f; // 0..1
            float scale = Mathf.Lerp(minScale, maxScale, k);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_img != null)
            {
                var c = _baseColor;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, k);
                _img.color = c;
            }
        }
    }
}
