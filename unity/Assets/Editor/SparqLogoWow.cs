using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLogoWow
    {
        [MenuItem("Sparq/53. WOW Logo (animated Sparq + bolt)")]
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
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(28f, -22f);
            rt.sizeDelta = new Vector2(320, 110);

            // 1. HALO — radial yellow glow behind everything
            var halo = new GameObject("Halo", typeof(RectTransform), typeof(Image));
            halo.transform.SetParent(titleGO.transform, false);
            var hrt = halo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0.5f);
            hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.anchoredPosition = Vector2.zero;
            hrt.sizeDelta = new Vector2(360, 130);
            var haloImg = halo.GetComponent<Image>();
            haloImg.color = new Color(1f, 0.85f, 0.30f, 0.25f);
            haloImg.raycastTarget = false;
            // Soften via material (built-in default is sharp rectangle but the alpha animation softens visually)

            // 2. WORD CONTAINER (so we can bounce it)
            var wordContainer = new GameObject("WordContainer", typeof(RectTransform));
            wordContainer.transform.SetParent(titleGO.transform, false);
            var wcrt = wordContainer.GetComponent<RectTransform>();
            wcrt.anchorMin = new Vector2(0, 0); wcrt.anchorMax = new Vector2(1, 1);
            wcrt.offsetMin = Vector2.zero; wcrt.offsetMax = Vector2.zero;

            // 3. STACKED SHADOWS — multiple depth layers for 3D-thick chunky look
            // Each shadow at increasing offset creates visible thickness
            var shadowOffsets = new (float x, float y, float darkness, float scale)[]
            {
                (8f, -8f, 0.85f, 1.0f),
                (6f, -6f, 0.75f, 1.0f),
                (4f, -4f, 0.65f, 1.0f),
                (2f, -2f, 0.55f, 1.0f),
            };
            foreach (var so2 in shadowOffsets)
            {
                var shadow = new GameObject("Shadow_" + so2.x, typeof(RectTransform));
                shadow.transform.SetParent(wordContainer.transform, false);
                var srt = shadow.GetComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = new Vector2(so2.x, -so2.x);
                srt.offsetMax = new Vector2(so2.x, -so2.x);
                var stm = shadow.AddComponent<TextMeshProUGUI>();
                stm.text = "Sparq";
                stm.fontSize = 72;
                stm.fontStyle = FontStyles.Bold | FontStyles.Italic;
                stm.fontWeight = FontWeight.Black;
                stm.color = new Color(0.5f, 0.20f, 0.05f, so2.darkness);
                stm.alignment = TextAlignmentOptions.Left;
                stm.outlineWidth = 0.5f;
                stm.outlineColor = new Color(0.35f, 0.10f, 0.0f, 1f);
                stm.characterSpacing = 4f;
                stm.raycastTarget = false;
            }

            // 4. WORD MAIN — BRIGHT YELLOW/GOLD, ULTRA THICK
            var wordGO = new GameObject("Word", typeof(RectTransform));
            wordGO.transform.SetParent(wordContainer.transform, false);
            var wrt = wordGO.GetComponent<RectTransform>();
            wrt.anchorMin = Vector2.zero; wrt.anchorMax = Vector2.one;
            wrt.offsetMin = Vector2.zero; wrt.offsetMax = Vector2.zero;
            var wtm = wordGO.AddComponent<TextMeshProUGUI>();
            wtm.text = "Sparq";
            wtm.fontSize = 72;                                   // bigger
            wtm.fontStyle = FontStyles.Bold | FontStyles.Italic;
            wtm.fontWeight = FontWeight.Black;
            wtm.color = new Color(1f, 0.92f, 0.30f);             // BRIGHT GOLDEN YELLOW
            wtm.alignment = TextAlignmentOptions.Left;
            wtm.outlineWidth = 0.75f;                            // SUPER thick outline
            wtm.outlineColor = new Color(0.55f, 0.22f, 0.0f, 1f); // deep orange-brown
            wtm.faceColor = new Color(1f, 0.92f, 0.30f);
            wtm.characterSpacing = 4f;
            wtm.raycastTarget = false;

            // 5. BOLT — procedural lightning sprite (no broken unicode)
            var boltGO = new GameObject("Bolt", typeof(RectTransform), typeof(Image));
            boltGO.transform.SetParent(titleGO.transform, false);
            var brt = boltGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 0.5f);
            brt.anchorMax = new Vector2(1f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(-22f, 4f);
            brt.sizeDelta = new Vector2(70, 90);
            var bImg = boltGO.GetComponent<Image>();
            bImg.sprite = MakeLightningSprite();
            bImg.color = new Color(1f, 0.92f, 0.30f);
            bImg.preserveAspect = true;
            bImg.raycastTarget = false;
            // Need a TMP_Text for the SparqLogo controller's color animation — hide it
            var btmGO = new GameObject("BoltColorAnchor");
            btmGO.transform.SetParent(boltGO.transform, false);
            var btm = btmGO.AddComponent<TextMeshProUGUI>();
            btm.text = "";
            btm.enabled = false;

            // 6. CONTROLLER
            var ctrl = titleGO.AddComponent<Sparq.UI.SparqLogo>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("wordText").objectReferenceValue       = wtm;
            so.FindProperty("halo").objectReferenceValue           = hrt;
            so.FindProperty("haloImg").objectReferenceValue        = haloImg;
            so.FindProperty("bolt").objectReferenceValue           = brt;
            so.FindProperty("boltText").objectReferenceValue       = btm;
            so.FindProperty("boltImage").objectReferenceValue      = bImg;
            so.FindProperty("wordContainer").objectReferenceValue  = wcrt;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            // Update halo color to match yellow
            haloImg.color = new Color(1f, 0.85f, 0.30f, 0.32f);
            hrt.sizeDelta = new Vector2(420, 160);

            EditorUtility.DisplayDialog("Sparq Logo",
                "✅ WOW logo built!\n\n" +
                "Animations live every frame:\n" +
                "• Yellow halo pulses behind\n" +
                "• 'Sparq' bounces gently + color-shifts gold ↔ honey\n" +
                "• ⚡ bolt wiggles + scales + glows\n" +
                "• 2-5 sparks/second fly off the bolt\n" +
                "• Bold italic with thick brown outline + drop shadow\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ── Lightning bolt procedural sprite ───────────────────────────────────
        private static Sprite MakeLightningSprite()
        {
            const int W = 256;
            const int H = 384;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            // 4 zigzag points defining the bolt: top-right → middle-left → middle-right → bottom-left
            var points = new[]
            {
                new Vector2(0.65f * W, 0.05f * H),  // top right tip
                new Vector2(0.40f * W, 0.45f * H),  // first kink (left)
                new Vector2(0.60f * W, 0.50f * H),  // second kink (right)
                new Vector2(0.20f * W, 0.95f * H),  // bottom tip
            };

            // Closing wing points to give the classic bolt shape
            var widthByY = new System.Func<float, float>((y) =>
            {
                // Tapered: thicker in middle, thin at tips
                float t = Mathf.Clamp01(y / H);
                return Mathf.Lerp(14f, 28f, 1f - Mathf.Abs(t - 0.5f) * 2f);
            });

            // Clear to transparent
            var pixels = new Color[W * H];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0, 0, 0, 0);

            // For each pixel, find min distance to bolt path; if within width, fill
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
                    // Soft white core + yellow edges
                    Color core = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.3f, 1f), t);
                    pixels[y * W + x] = core;
                }
                else if (minDist <= halfW + 6f)
                {
                    // Soft outer glow
                    float a = 1f - (minDist - halfW) / 6f;
                    pixels[y * W + x] = new Color(1f, 0.85f, 0.3f, a * 0.5f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            return sprite;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector2 proj = a + ab * t;
            return (p - proj).magnitude;
        }
    }
}
