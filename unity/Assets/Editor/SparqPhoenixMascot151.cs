using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 151: Reverts the phoenix-recolor and instead places an actual
    /// Phoenix (Pyro-Griffin) mascot beside the Daily Trial bubble, with a
    /// speech-tail pointer so the bubble visually originates from the phoenix.
    /// </summary>
    public static class SparqPhoenixMascot151
    {
        private const string PYRO_GRIFFIN = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Griffin/Pyro-Griffin.png";

        // Restore palette
        private static readonly Color CREAM     = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color GOLD      = new Color(1f, 0.82f, 0.32f);
        private static readonly Color CRIMSON   = new Color(0.62f, 0.13f, 0.18f);
        private static readonly Color RED_KIND  = new Color(0.85f, 0.40f, 0.45f);
        private static readonly Color FLAME     = new Color(1f, 0.55f, 0.20f);

        [MenuItem("Sparq/151. Phoenix mascot beside bubble (undo 150)")]
        public static void Apply()
        {
            EnsureSprite(PYRO_GRIFFIN);

            var card = GameObject.Find("DailyTrialCard");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found.", "OK"); return; }

            // 1. Revert #150 palette
            var img = card.GetComponent<Image>();
            if (img != null) img.color = new Color(1f, 1f, 1f, 0.96f);

            var ribbon = card.transform.Find("Ribbon");
            if (ribbon != null)
            {
                var rImg = ribbon.GetComponent<Image>();
                if (rImg != null) rImg.color = GOLD;
                foreach (var tm in ribbon.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = DEEP_NAVY;
                    tm.outlineWidth = 0;
                }

                // Remove flame embers from #150 if present
                foreach (var n in new[] { "EmberLeft", "EmberRight" })
                {
                    var e = ribbon.Find(n);
                    if (e != null) Object.DestroyImmediate(e.gameObject);
                }
            }

            var glyphBg = card.transform.Find("GlyphBg");
            if (glyphBg != null)
            {
                var gImg = glyphBg.GetComponent<Image>();
                if (gImg != null) gImg.color = RED_KIND;
                foreach (var tm in glyphBg.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = DEEP_NAVY;
                    tm.outlineWidth = 0;
                }
            }

            var title = card.transform.Find("Title");
            if (title != null)
                foreach (var tm in title.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = CRIMSON;
                    tm.fontStyle = FontStyles.Bold;
                    tm.outlineWidth = 0.18f;
                    tm.outlineColor = new Color(1f, 0.95f, 0.82f, 0.9f);
                }

            var sub = card.transform.Find("Sub");
            if (sub != null)
                foreach (var tm in sub.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = new Color(0.25f, 0.20f, 0.10f);
                    tm.outlineWidth = 0;
                }

            var reward = card.transform.Find("Reward");
            if (reward != null)
                foreach (var tm in reward.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = new Color(0.55f, 0.30f, 0.05f);
                    tm.fontStyle = FontStyles.Bold;
                    tm.outlineWidth = 0;
                }

            var begin = card.transform.Find("BeginBtn");
            if (begin != null)
            {
                var bImg = begin.GetComponent<Image>();
                if (bImg != null) bImg.color = GOLD;
                foreach (var tm in begin.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = DEEP_NAVY;
                    tm.outlineWidth = 0;
                }
            }

            // 2. Add Phoenix mascot to the LEFT of the card
            //    (sibling so it can hang outside the card's rect)
            var parent = card.transform.parent;
            var oldPhx = parent.Find("PhoenixMascot");
            if (oldPhx != null) Object.DestroyImmediate(oldPhx.gameObject);

            var phx = new GameObject("PhoenixMascot", typeof(RectTransform), typeof(Image));
            phx.transform.SetParent(parent, false);

            // Match card transform settings, then offset to its lower-left edge
            var cardRT = card.GetComponent<RectTransform>();
            var phxRT  = phx.GetComponent<RectTransform>();
            phxRT.anchorMin = cardRT.anchorMin;
            phxRT.anchorMax = cardRT.anchorMax;
            phxRT.pivot     = new Vector2(0.5f, 0.5f);
            // Place to the LEFT of the card, with bottom slightly below card bottom
            float halfCardW = cardRT.sizeDelta.x * 0.5f;
            phxRT.anchoredPosition = cardRT.anchoredPosition + new Vector2(-halfCardW - 50, -25);
            phxRT.sizeDelta = new Vector2(140, 140);

            var pImg = phx.GetComponent<Image>();
            pImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PYRO_GRIFFIN);
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;

            // Tiny idle bob via a simple component
            var bob = phx.AddComponent<UIIdleBob>();
            bob.amplitude = 4f; bob.frequency = 0.6f;

            // 3. Add a speech tail on the card pointing toward phoenix
            var oldTail = card.transform.Find("SpeechTail");
            if (oldTail != null) Object.DestroyImmediate(oldTail.gameObject);

            var tail = new GameObject("SpeechTail", typeof(RectTransform), typeof(Image));
            tail.transform.SetParent(card.transform, false);
            var trt = tail.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.4f);
            trt.anchorMax = new Vector2(0, 0.4f);
            trt.pivot     = new Vector2(1, 0.5f);
            trt.anchoredPosition = new Vector2(2, -8);
            trt.sizeDelta = new Vector2(28, 22);
            trt.localRotation = Quaternion.Euler(0, 0, -25);

            var tImg = tail.GetComponent<Image>();
            tImg.sprite = BuildTriangleSprite();
            tImg.color  = new Color(1f, 1f, 1f, 0.96f); // matches bubble bg
            tImg.preserveAspect = false;
            tImg.raycastTarget = false;

            // Render order: phoenix BEHIND card so the bubble overlaps slightly
            phx.transform.SetSiblingIndex(card.transform.GetSiblingIndex());
            card.transform.SetAsLastSibling();
            tail.transform.SetAsFirstSibling();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Phoenix mascot added.\n\n" +
                "• Pyro-Griffin sprite to the left of the bubble\n" +
                "• Subtle idle bob\n" +
                "• Bubble has a speech-tail pointing at the phoenix\n" +
                "• #150 palette reverted (cream / gold / crimson)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Triangle sprite for speech tail ─────────────────────
        private static Sprite BuildTriangleSprite()
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                // Triangle pointing left: alpha 1 if x <= (s - y * s/s)
                // Right-angled triangle with apex on the LEFT, flat side on the RIGHT
                float fy = (float)y / s;
                float maxX = s - (Mathf.Abs(fy - 0.5f) * 2f) * s;
                tex.SetPixel(x, y, x <= maxX ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }

    // Inline helper since IdleBob is for SpriteRenderers; this one drives a UI RectTransform.
    public class UIIdleBob : MonoBehaviour
    {
        public float amplitude = 4f;
        public float frequency = 0.6f;
        private Vector2 _base;
        private RectTransform _rt;
        private void Awake() { _rt = GetComponent<RectTransform>(); _base = _rt.anchoredPosition; }
        private void OnEnable() { if (_rt != null) _base = _rt.anchoredPosition; }
        private void Update()
        {
            if (_rt == null) return;
            float y = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
            _rt.anchoredPosition = new Vector2(_base.x, _base.y + y);
        }
    }
}
