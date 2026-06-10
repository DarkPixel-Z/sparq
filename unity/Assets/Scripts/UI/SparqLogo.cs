using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// The wow-factor SPARQ logo. Animates:
    ///   • Shine sweep across the letters
    ///   • Pulsing yellow halo behind
    ///   • Bolt wiggle + scale punch
    ///   • Sparks fly from the bolt
    ///   • Gentle bounce on the whole word
    /// </summary>
    public class SparqLogo : MonoBehaviour
    {
        public TMP_Text wordText;
        public RectTransform halo;
        public Image haloImg;
        public RectTransform bolt;
        public TMP_Text boltText;
        public Image      boltImage;   // visible bolt sprite
        public RectTransform wordContainer;

        private float _t;
        private float _nextSpark;

        private void Update()
        {
            _t += Time.deltaTime;

            // Halo pulse — yellow glow grows + fades
            if (haloImg != null && halo != null)
            {
                float pulse = (Mathf.Sin(_t * 1.4f) + 1f) * 0.5f;
                halo.localScale = Vector3.one * Mathf.Lerp(0.96f, 1.10f, pulse);
                var c = haloImg.color; c.a = Mathf.Lerp(0.18f, 0.45f, pulse); haloImg.color = c;
            }

            // Word bounce + color shift
            if (wordContainer != null)
            {
                wordContainer.localPosition = new Vector3(
                    wordContainer.localPosition.x,
                    Mathf.Sin(_t * 2.0f) * 2f,
                    0);
                float scale = 1f + Mathf.Sin(_t * 1.6f) * 0.02f;
                wordContainer.localScale = Vector3.one * scale;
            }

            // Bolt wiggle + flash
            if (bolt != null)
            {
                bolt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_t * 4f) * 12f);
                float bs = 1f + Mathf.Abs(Mathf.Sin(_t * 3.5f)) * 0.15f;
                bolt.localScale = new Vector3(bs, bs, 1f);
            }
            if (boltImage != null)
            {
                float boltGlow = (Mathf.Sin(_t * 6f) + 1f) * 0.5f;
                boltImage.color = Color.Lerp(
                    new Color(1f, 0.85f, 0.25f),
                    new Color(1f, 1f, 0.85f),
                    boltGlow);
            }

            // Spark every 0.18-0.4s
            if (_t >= _nextSpark)
            {
                _nextSpark = _t + Random.Range(0.18f, 0.42f);
                SpawnSpark();
            }

            // Color shift disabled — let the editor-set color stick.
            // (User picks color via Sparq → 61.* menus.)
        }

        private void SpawnSpark()
        {
            if (bolt == null) return;

            var sp = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            sp.transform.SetParent(transform, false);
            var srt = sp.GetComponent<RectTransform>();
            srt.anchorMin = bolt.anchorMin; srt.anchorMax = bolt.anchorMax;
            srt.pivot = new Vector2(0.5f, 0.5f);
            srt.anchoredPosition = bolt.anchoredPosition + new Vector2(Random.Range(-12f, 12f), Random.Range(-15f, 15f));
            srt.sizeDelta = new Vector2(Random.Range(5f, 12f), Random.Range(5f, 12f));
            var img = sp.GetComponent<Image>();
            img.color = new Color(1f, Random.Range(0.85f, 1f), Random.Range(0.2f, 0.6f), 1f);
            img.raycastTarget = false;
            var fly = sp.AddComponent<SparkFly>();
            fly.velocity = new Vector2(Random.Range(-50f, 80f), Random.Range(-30f, 100f));
            fly.life = Random.Range(0.55f, 0.95f);
        }

        private class SparkFly : MonoBehaviour
        {
            public Vector2 velocity;
            public float life;
            float t;
            Image img;
            RectTransform rt;
            void Awake() { img = GetComponent<Image>(); rt = (RectTransform)transform; }
            void Update()
            {
                t += Time.deltaTime;
                if (rt != null) rt.anchoredPosition += velocity * Time.deltaTime;
                velocity *= 0.94f;
                if (img != null) { var c = img.color; c.a = 1f - (t / life); img.color = c; }
                if (t >= life) Destroy(gameObject);
            }
        }
    }
}
