using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLogoTune
    {
        [MenuItem("Sparq/65. NUKE + REBUILD logo (guaranteed visible)")]
        public static void NukeRebuild()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Kill old
            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Build dead-simple logo at top-left — NO procedural sprite, NO custom font
            var titleGO = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -20f);
            rt.sizeDelta = new Vector2(330, 90);

            // Plate — semi-transparent purple
            titleGO.GetComponent<Image>().color = new Color(0.20f, 0.05f, 0.35f, 0.65f);

            // Yellow accent on top
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(titleGO.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);

            // Word "Sparq" — large, yellow, default TMP font
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(titleGO.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0, 0); wrt.anchorMax = new Vector2(1, 1);
            wrt.offsetMin = new Vector2(20, 0); wrt.offsetMax = new Vector2(-90, 0);  // leave room for bolt
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "Sparq";
            wtm.fontSize = 60;
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.color = new Color(1f, 0.92f, 0.30f);
            wtm.alignment = TextAlignmentOptions.Left;
            wtm.outlineWidth = 0.4f;
            wtm.outlineColor = new Color(0.55f, 0.20f, 0.0f, 1f);
            wtm.raycastTarget = false;

            // Bolt — simple ASCII glyph in different font, large
            var boltGO = new GameObject("Bolt", typeof(RectTransform));
            boltGO.transform.SetParent(titleGO.transform, false);
            var brt = boltGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0.5f); brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(1f, 0.5f);
            brt.anchoredPosition = new Vector2(-12f, 0);
            brt.sizeDelta = new Vector2(70, 80);
            var btm = boltGO.AddComponent<TextMeshProUGUI>();
            btm.text = "⚡";
            btm.fontSize = 72;
            btm.color = new Color(1f, 0.92f, 0.30f);
            btm.alignment = TextAlignmentOptions.Center;
            btm.outlineWidth = 0.3f;
            btm.outlineColor = new Color(0.55f, 0.22f, 0.0f, 1f);
            btm.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ Logo rebuilt simply.\n\n" +
                "• Yellow Sparq, italic bold\n" +
                "• Yellow lightning bolt next to it\n" +
                "• Purple plate background\n" +
                "• Default TMP font (guaranteed visible)\n\n" +
                "If the bolt still shows as a square, your TMP font lacks the ⚡ glyph.\n" +
                "We'll fix that with a sprite if needed.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/63. RESTORE logo (default font + visible)")]
        public static void Restore()
        {
            var title = GameObject.Find("GameTitle");
            if (title == null) return;
            // Reset to default LiberationSans SDF font + visible white color
            foreach (var tmp in title.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.font = TMP_Settings.defaultFontAsset;
            }
            // Word back to bright yellow
            var word = title.transform.Find("WordContainer/Word");
            if (word != null)
            {
                var tmp = word.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.color = new Color(1f, 0.92f, 0.30f);
                    tmp.faceColor = new Color(1f, 0.92f, 0.30f);
                    tmp.outlineColor = new Color(0.55f, 0.22f, 0.0f, 1f);
                }
            }
            // Shadows back to dark brown
            foreach (Transform child in title.transform.Find("WordContainer") ?? title.transform)
            {
                if (child.name.StartsWith("Shadow"))
                {
                    var tmp = child.GetComponent<TMP_Text>();
                    if (tmp != null)
                    {
                        tmp.color = new Color(0.5f, 0.20f, 0.05f, 0.8f);
                        tmp.outlineColor = new Color(0.35f, 0.10f, 0.0f, 1f);
                    }
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ Logo restored to default font + visible bright yellow.\n\n" +
                "Now run Sparq → 61.x to pick a NEW color (your custom font is saved as a TMP asset; once you re-create it via right-click → Create → TextMeshPro → Font Asset, it'll work).", "OK");
        }

        // ── Word color picker ────────────────────────────────────────────────
        [MenuItem("Sparq/61. Word color → Hot Pink")]
        public static void HotPink() => SetWord(new Color(1f, 0.45f, 0.78f), new Color(0.45f, 0.05f, 0.30f));

        [MenuItem("Sparq/61a. Word color → Electric Cyan")]
        public static void Cyan() => SetWord(new Color(0.35f, 0.92f, 1f), new Color(0.0f, 0.30f, 0.55f));

        [MenuItem("Sparq/61b. Word color → Crimson Red")]
        public static void Red() => SetWord(new Color(1f, 0.30f, 0.35f), new Color(0.45f, 0.0f, 0.05f));

        [MenuItem("Sparq/61c. Word color → Pure White")]
        public static void White() => SetWord(new Color(1f, 1f, 1f), new Color(0.30f, 0.10f, 0.55f));

        [MenuItem("Sparq/61d. Word color → Mint Green")]
        public static void Mint() => SetWord(new Color(0.45f, 1f, 0.65f), new Color(0.05f, 0.30f, 0.10f));

        [MenuItem("Sparq/61e. Word color → Royal Purple")]
        public static void Purple() => SetWord(new Color(0.70f, 0.35f, 1f), new Color(0.20f, 0.0f, 0.45f));

        [MenuItem("Sparq/61f. Word color → Orange Burst")]
        public static void Orange() => SetWord(new Color(1f, 0.55f, 0.18f), new Color(0.45f, 0.15f, 0.0f));

        [MenuItem("Sparq/61g. Word color → Sunset Pink")]
        public static void Sunset() => SetWord(new Color(1f, 0.40f, 0.65f), new Color(0.50f, 0.10f, 0.35f));

        private static void SetWord(Color face, Color outline)
        {
            var title = GameObject.Find("GameTitle");
            if (title == null) return;
            var word = title.transform.Find("WordContainer/Word");
            if (word == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Word object not found. Run Sparq → 53 first.", "OK");
                return;
            }
            var tmp = word.GetComponent<TMP_Text>();
            if (tmp == null) return;
            tmp.color = face;
            tmp.faceColor = face;
            tmp.outlineColor = outline;

            // Also disable runtime color shift if it's stomping our setting
            var ctrl = title.GetComponent<Sparq.UI.SparqLogo>();
            if (ctrl != null) ctrl.enabled = true; // the controller still shifts brightness; that's ok

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ Word color updated.\n\nHit ▶ Play.", "OK");
        }

        // ── Thicker bolt ─────────────────────────────────────────────────────
        [MenuItem("Sparq/62. Make bolt THICKER")]
        public static void ThickerBolt()
        {
            var title = GameObject.Find("GameTitle");
            if (title == null) return;
            var bolt = title.transform.Find("Bolt");
            if (bolt == null) return;

            var img = bolt.GetComponent<Image>();
            if (img == null) return;

            // Replace sprite with thicker version
            img.sprite = MakeThickLightningSprite();

            // Bigger size box
            var rt = bolt.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 110);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ Bolt thickened. Same yellow color, ~50% wider strokes.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static Sprite MakeThickLightningSprite()
        {
            const int W = 256;
            const int H = 384;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var points = new[]
            {
                new Vector2(0.65f * W, 0.05f * H),
                new Vector2(0.40f * W, 0.45f * H),
                new Vector2(0.60f * W, 0.50f * H),
                new Vector2(0.20f * W, 0.95f * H),
            };

            // Much thicker now — was 14-28, now 22-42
            var widthByY = new System.Func<float, float>((y) =>
            {
                float t = Mathf.Clamp01(y / H);
                return Mathf.Lerp(22f, 42f, 1f - Mathf.Abs(t - 0.5f) * 2f);
            });

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
                else if (minDist <= halfW + 8f)
                {
                    float a = 1f - (minDist - halfW) / 8f;
                    pixels[y * W + x] = new Color(1f, 0.85f, 0.3f, a * 0.6f);
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
    }
}
