using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Comic-book-style "SPARQ ⚡" logo: bold crimson text, thick black outline,
    /// yellow burst behind, slight rotation. No external PSD needed — fully procedural.
    /// </summary>
    public static class SparqLogoComic
    {
        [MenuItem("Sparq/73. SPARQ Comic Logo (crimson + bolt)")]
        public static void Build()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Root
            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -16f);
            rt.sizeDelta = new Vector2(340, 110);

            // Slight tilt for comic-book energy
            titleGO.transform.localRotation = Quaternion.Euler(0, 0, -4f);

            // Yellow starburst behind
            var burst = new GameObject("Burst", typeof(RectTransform), typeof(Image));
            burst.transform.SetParent(titleGO.transform, false);
            var burt = burst.GetComponent<RectTransform>();
            burt.anchorMin = new Vector2(0.5f, 0.5f);
            burt.anchorMax = new Vector2(0.5f, 0.5f);
            burt.pivot = new Vector2(0.5f, 0.5f);
            burt.anchoredPosition = Vector2.zero;
            burt.sizeDelta = new Vector2(380, 130);
            burst.GetComponent<Image>().sprite = MakeStarburst();
            burst.GetComponent<Image>().color = new Color(1f, 0.85f, 0.2f, 0.95f);
            burst.GetComponent<Image>().raycastTarget = false;
            burst.AddComponent<BurstSpin>();

            // Stacked dark shadows behind text for thickness
            for (int i = 4; i >= 1; i--)
            {
                var shadow = new GameObject($"Shadow_{i}", typeof(RectTransform));
                shadow.transform.SetParent(titleGO.transform, false);
                var srt = shadow.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(i * 2.5f, -i * 2.5f);
                srt.offsetMax = new Vector2(i * 2.5f, -i * 2.5f);
                var stm = shadow.AddComponent<TextMeshProUGUI>();
                stm.text = "SPARQ⚡";
                stm.fontSize = 60;
                stm.fontStyle = FontStyles.Bold | FontStyles.Italic;
                stm.color = new Color(0, 0, 0, 0.85f);
                stm.alignment = TextAlignmentOptions.Center;
                stm.outlineWidth = 0.5f;
                stm.outlineColor = new Color(0, 0, 0, 1f);
                stm.raycastTarget = false;
            }

            // Main text — CRIMSON with thick black outline
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(titleGO.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "SPARQ⚡";
            wtm.fontSize = 60;
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.color = new Color(0.95f, 0.18f, 0.25f);          // crimson
            wtm.alignment = TextAlignmentOptions.Center;
            wtm.outlineWidth = 0.7f;                              // thick comic outline
            wtm.outlineColor = new Color(0, 0, 0, 1f);            // pure black
            wtm.faceColor = new Color(0.95f, 0.18f, 0.25f);
            wtm.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ SPARQ Comic Logo built!\n\n" +
                "• CRIMSON text with thick black outline (comic book style)\n" +
                "• Yellow starburst spinning behind\n" +
                "• 4-layer drop shadow for depth\n" +
                "• Slight tilt for energy\n" +
                "• ⚡ bolt as part of the word\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // Generates a yellow starburst sprite (sunray pattern)
        private static Sprite MakeStarburst()
        {
            const int W = 512;
            const int H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 c = new Vector2(W / 2f, H / 2f);
            int rays = 16;
            float radius = Mathf.Min(W, H) * 0.5f;

            var pixels = new Color[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = (x - c.x) / radius;
                float dy = (y - c.y) / radius;
                float r  = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) + Mathf.PI;
                float angRays = (angle / (2f * Mathf.PI)) * rays;
                float frac = angRays - Mathf.Floor(angRays);
                float ray = Mathf.Abs(frac - 0.5f) * 2f;  // 0..1, 0 at ray center
                float falloff = Mathf.Clamp01(1f - r);
                float a = (ray < 0.6f ? 1f - ray / 0.6f : 0f) * falloff;
                pixels[y * W + x] = new Color(1f, 0.85f, 0.2f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        private class BurstSpin : MonoBehaviour
        {
            float t;
            void Update()
            {
                t += Time.deltaTime * 8f;
                transform.localRotation = Quaternion.Euler(0, 0, t);
            }
        }
    }
}
