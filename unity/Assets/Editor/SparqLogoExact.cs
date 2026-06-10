using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Exact recreation of the WebView logo:
    ///   • "Sparq" in crimson italic bold, comic-book thick, dark red outline
    ///   • Yellow ⚡ bolt next to it, separate (not part of word)
    ///   • No burst, no plate — clean
    /// </summary>
    public static class SparqLogoExact
    {
        [MenuItem("Sparq/75. EXACT Sparq logo (crimson + yellow bolt)")]
        public static void Build()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Container
            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -28f);
            rt.sizeDelta = new Vector2(280, 90);

            // ── 1. WORD CONTAINER ────────────────────────────────────────────
            var wordC = new GameObject("WordContainer", typeof(RectTransform));
            wordC.transform.SetParent(titleGO.transform, false);
            var wcRT = wordC.GetComponent<RectTransform>();
            wcRT.anchorMin = new Vector2(0, 0); wcRT.anchorMax = new Vector2(0, 1);
            wcRT.pivot = new Vector2(0, 0.5f);
            wcRT.anchoredPosition = new Vector2(0, 0);
            wcRT.sizeDelta = new Vector2(190, 0);

            // Stacked dark-red shadows for thickness
            for (int i = 4; i >= 1; i--)
            {
                var shadow = new GameObject($"Shadow_{i}", typeof(RectTransform));
                shadow.transform.SetParent(wordC.transform, false);
                var srt = shadow.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(i * 1.8f, -i * 1.8f);
                srt.offsetMax = new Vector2(i * 1.8f, -i * 1.8f);
                var stm = shadow.AddComponent<TextMeshProUGUI>();
                stm.text = "Sparq";
                stm.fontSize = 60;
                stm.fontStyle = FontStyles.Bold | FontStyles.Italic;
                stm.color = new Color(0.30f, 0.0f, 0.05f, 0.95f);    // deep dark crimson
                stm.alignment = TextAlignmentOptions.Left;
                stm.outlineWidth = 0.55f;
                stm.outlineColor = new Color(0.20f, 0.0f, 0.0f, 1f);
                stm.raycastTarget = false;
            }

            // Main word — CRIMSON, comic-thick, dark outline
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(wordC.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "Sparq";
            wtm.fontSize = 60;
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.color = new Color(0.95f, 0.20f, 0.30f);              // bright crimson
            wtm.alignment = TextAlignmentOptions.Left;
            wtm.outlineWidth = 0.62f;                                // thick outline
            wtm.outlineColor = new Color(0.30f, 0.0f, 0.05f, 1f);    // dark crimson outline
            wtm.faceColor = new Color(0.95f, 0.20f, 0.30f);
            wtm.raycastTarget = false;

            // ── 2. YELLOW LIGHTNING BOLT ─────────────────────────────────────
            var boltGO = new GameObject("Bolt", typeof(RectTransform), typeof(Image));
            boltGO.transform.SetParent(titleGO.transform, false);
            var brt = boltGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(192, 4);
            brt.sizeDelta = new Vector2(56, 78);
            var bImg = boltGO.GetComponent<Image>();
            bImg.sprite = MakeBoltSprite();
            bImg.color = new Color(1f, 0.92f, 0.20f);
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;
            boltGO.AddComponent<BoltWiggle>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ Exact WebView-style Sparq logo built.\n\n" +
                "• Crimson 'Sparq' in italic bold\n" +
                "• Thick dark-crimson outline + 4-stack shadows\n" +
                "• Yellow lightning bolt sprite next to it\n" +
                "• Slight bolt wiggle for life\n" +
                "• Clean, no plate background\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static Sprite MakeBoltSprite()
        {
            const int W = 200;
            const int H = 320;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var points = new[]
            {
                new Vector2(0.65f * W, 0.05f * H),
                new Vector2(0.35f * W, 0.45f * H),
                new Vector2(0.60f * W, 0.50f * H),
                new Vector2(0.20f * W, 0.95f * H),
            };

            System.Func<float, float> widthByY = (y) =>
            {
                float t = Mathf.Clamp01(y / H);
                return Mathf.Lerp(20f, 36f, 1f - Mathf.Abs(t - 0.5f) * 2f);
            };

            var pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2(x, y);
                float minDist = float.MaxValue;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    float d = DistanceToSegment(p, points[i], points[i + 1]);
                    if (d < minDist) minDist = d;
                }
                float halfW = widthByY(y);
                if (minDist <= halfW)
                {
                    float t = minDist / halfW;
                    Color core = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.3f, 1f), t);
                    pixels[y * W + x] = core;
                }
                else if (minDist <= halfW + 4f)
                {
                    // dark outline ring (so the bolt has a contour like the comic book)
                    pixels[y * W + x] = new Color(0.45f, 0.20f, 0f, (halfW + 4f - minDist) / 4f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            return (p - (a + ab * t)).magnitude;
        }

        private class BoltWiggle : MonoBehaviour
        {
            float t;
            void Awake() { t = Random.value * 5f; }
            void Update()
            {
                t += Time.deltaTime;
                transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.5f) * 6f);
                float s = 1f + Mathf.Sin(t * 2.0f) * 0.05f;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }
}
