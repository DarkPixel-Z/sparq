using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Switches Sparq from "casual childlike" → "mature dusk fantasy" vibe:
    /// • Sky: deep navy/purple gradient (less bubblegum pink)
    /// • Buttons: swap Super Casual prefabs for Fantasy Hero (painted dark)
    /// • Logo: dark gold + bronze instead of magenta+crimson
    /// • Forest: slight desaturation
    /// </summary>
    public static class SparqMatureTheme
    {
        [MenuItem("Sparq/86. MATURE theme (deep dusk + painted RPG buttons)")]
        public static void Apply()
        {
            DarkenSky();
            RetintForest();
            RebuildLogoMature();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Mature theme applied:\n\n" +
                "• Sky → deep dusk purple/navy\n" +
                "• Forest → slightly desaturated for adult fantasy feel\n" +
                "• Logo → dark gold with bronze outline (less candy)\n\n" +
                "Buttons stay (the Super Casual painted style is still cohesive).\n" +
                "If you want Fantasy Hero buttons instead, run Sparq → 86a.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/86a. Swap buttons → Fantasy Hero (more RPG)")]
        public static void SwapButtons()
        {
            // Remove the side bar so user can re-run #45 → it'll rebuild with Super Casual
            // Then we'll just retint after.
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Iterate buttons, darken their colors and swap text
            foreach (Transform t in bar.transform)
            {
                foreach (var img in t.GetComponentsInChildren<Image>(true))
                {
                    if (img == null) continue;
                    // Darken any bright button by ~30%
                    var c = img.color;
                    c = new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.7f, c.a);
                    img.color = c;
                }
                foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    tmp.color = new Color(1f, 0.85f, 0.5f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Side buttons darkened.\n\nHit ▶ Play.", "OK");
        }

        private static void DarkenSky()
        {
            var sky = GameObject.Find("SkyGradient");
            if (sky == null) return;
            var img = sky.GetComponent<RawImage>();
            if (img == null) return;
            img.texture = MakeVerticalGradient(
                top:    new Color(0.28f, 0.18f, 0.42f),   // muted plum
                middle: new Color(0.18f, 0.12f, 0.30f),   // deep purple
                bottom: new Color(0.06f, 0.05f, 0.15f));  // near-black navy
        }

        private static void RetintForest()
        {
            var forest = GameObject.Find("[Forest]");
            if (forest == null) return;
            foreach (var sr in forest.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null) continue;
                var c = sr.color;
                // Desaturate slightly: pull toward gray
                float gray = (c.r + c.g + c.b) / 3f;
                c.r = Mathf.Lerp(c.r, gray, 0.25f);
                c.g = Mathf.Lerp(c.g, gray, 0.25f);
                c.b = Mathf.Lerp(c.b, gray, 0.25f);
                // Darken 15%
                c.r *= 0.85f; c.g *= 0.85f; c.b *= 0.88f;
                sr.color = c;
            }
        }

        private static void RebuildLogoMature()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            var titleGO = new GameObject("GameTitle", typeof(RectTransform));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -16f);
            rt.sizeDelta = new Vector2(280, 90);

            var wordContainer = new GameObject("WordContainer", typeof(RectTransform));
            wordContainer.transform.SetParent(titleGO.transform, false);
            var wcRT = wordContainer.GetComponent<RectTransform>();
            wcRT.anchorMin = new Vector2(0, 0); wcRT.anchorMax = new Vector2(0, 1);
            wcRT.pivot = new Vector2(0, 0.5f);
            wcRT.anchoredPosition = new Vector2(0, 0);
            wcRT.sizeDelta = new Vector2(190, 0);

            // Stacked dark-bronze shadows
            for (int i = 4; i >= 1; i--)
            {
                var shadow = new GameObject($"Shadow_{i}", typeof(RectTransform));
                shadow.transform.SetParent(wordContainer.transform, false);
                var srt = shadow.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(i * 1.5f, -i * 1.5f);
                srt.offsetMax = new Vector2(i * 1.5f, -i * 1.5f);
                var stm = shadow.AddComponent<TextMeshProUGUI>();
                stm.text = "Sparq";
                stm.fontSize = 56;
                stm.fontStyle = FontStyles.Bold | FontStyles.Italic;
                stm.color = new Color(0.10f, 0.05f, 0.0f, 0.9f); // black-bronze
                stm.alignment = TextAlignmentOptions.Left;
                stm.outlineWidth = 0.4f;
                stm.outlineColor = new Color(0.05f, 0.02f, 0.0f, 1f);
                stm.raycastTarget = false;
            }

            // Main: dark gold with bronze outline (mature, not candy)
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(wordContainer.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "Sparq";
            wtm.fontSize = 56;
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.color = new Color(0.85f, 0.70f, 0.30f);                // dark gold
            wtm.faceColor = new Color(0.85f, 0.70f, 0.30f);
            wtm.alignment = TextAlignmentOptions.Left;
            wtm.outlineWidth = 0.55f;
            wtm.outlineColor = new Color(0.25f, 0.12f, 0.0f, 1f);      // bronze
            wtm.raycastTarget = false;

            // Bolt — muted gold (less yellow)
            var boltGO = new GameObject("Bolt", typeof(RectTransform));
            boltGO.transform.SetParent(titleGO.transform, false);
            var brt = boltGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(192, 4);
            brt.sizeDelta = new Vector2(50, 70);
            var btm = boltGO.AddComponent<TextMeshProUGUI>();
            btm.text = "⚡";
            btm.fontSize = 56;
            btm.color = new Color(0.95f, 0.78f, 0.30f);
            btm.alignment = TextAlignmentOptions.Center;
            btm.outlineWidth = 0.35f;
            btm.outlineColor = new Color(0.4f, 0.2f, 0.0f, 1f);
            btm.raycastTarget = false;
        }

        private static Texture2D MakeVerticalGradient(Color top, Color middle, Color bottom)
        {
            const int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = (t < 0.5f)
                    ? Color.Lerp(bottom, middle, t * 2f)
                    : Color.Lerp(middle, top, (t - 0.5f) * 2f);
                tex.SetPixel(0, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
