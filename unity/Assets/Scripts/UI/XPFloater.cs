using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// A short-lived floating text like "+20 XP" that rises up and fades out.
    /// Spawn via XPFloater.Spawn(parentCanvas, worldOrScreenPos, "+20 XP").
    /// </summary>
    public class XPFloater : MonoBehaviour
    {
        private TMP_Text _text;
        private CanvasGroup _cg;
        private float _elapsed;
        private float _duration = 1.2f;
        private Vector3 _startPos;
        private Vector3 _riseOffset = new Vector3(0, 80f, 0);

        public static XPFloater Spawn(Transform canvasTransform, Vector3 screenPos, string label, Color? color = null)
        {
            var go = new GameObject("XPFloater");
            go.transform.SetParent(canvasTransform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 48);

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 1;

            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 36;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = color ?? new Color(1f, 0.92f, 0.2f); // yellow default
            tm.fontStyle = FontStyles.Bold;
            tm.raycastTarget = false;
            // Strong outline for visibility on busy backgrounds
            tm.fontMaterial = new Material(tm.fontMaterial);
            tm.outlineWidth = 0.25f;
            tm.outlineColor = new Color(0.1f, 0f, 0.25f, 1f);

            rt.position = screenPos;

            var floater = go.AddComponent<XPFloater>();
            floater._cg = cg;
            floater._text = tm;
            floater._startPos = rt.position;

            return floater;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            // Rise with ease-out
            float eased = 1f - (1f - t) * (1f - t);
            var rt = (RectTransform)transform;
            rt.position = _startPos + _riseOffset * eased;

            // Fade out in the last 40%
            if (t > 0.6f) _cg.alpha = Mathf.Lerp(1, 0, (t - 0.6f) / 0.4f);

            // Scale up briefly then normal
            float scale = t < 0.15f ? Mathf.Lerp(0.5f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.85f);
            rt.localScale = new Vector3(scale, scale, 1f);

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
