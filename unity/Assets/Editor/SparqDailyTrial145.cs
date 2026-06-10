using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 145: A + B combo — adds idle bob to hero & pet, plus a
    /// floating "Today's Trial" card in the upper-middle of the screen.
    /// </summary>
    public static class SparqDailyTrial145
    {
        private const string FH_LABEL = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/";

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color GOLD      = new Color(1f, 0.82f, 0.32f);

        [MenuItem("Sparq/145. Daily Trial card + idle bob (hero + pet)")]
        public static void Apply()
        {
            EnsureSprite(FH_LABEL + "Label_Bubble_01_Bg.png");

            AddIdleBob();
            BuildTrialCard();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Wired:\n\n" +
                "• Karu + Wisp idle bob (subtle, different phases)\n" +
                "• Today's Trial card in mid-upper area\n" +
                "  · Rotates daily (7 trials)\n" +
                "  · Tap BEGIN → trial-accepted toast\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Idle bob ─────────────────────
        private static void AddIdleBob()
        {
            AttachBob("Karu",  amp: 0.06f, freq: 0.8f, phase: 0f);
            AttachBob("Mochi", amp: 0.10f, freq: 1.2f, phase: 0.5f); // pet bobs slightly faster
        }

        private static void AttachBob(string name, float amp, float freq, float phase)
        {
            var go = GameObject.Find(name);
            if (go == null) return;
            var bob = go.GetComponent<IdleBob>();
            if (bob == null) bob = go.AddComponent<IdleBob>();
            bob.SetParams(amp, freq, phase);
        }

        // ───────────────────── Daily Trial card ─────────────────────
        private static void BuildTrialCard()
        {
            var canvas = FindCanvas();
            if (canvas == null) return;

            var old = GameObject.Find("DailyTrialCard");
            if (old != null) Object.DestroyImmediate(old);

            var bubble = AssetDatabase.LoadAssetAtPath<Sprite>(FH_LABEL + "Label_Bubble_01_Bg.png");

            var card = new GameObject("DailyTrialCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);

            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 80); // upper-middle
            rt.sizeDelta = new Vector2(440, 110);

            var bg = card.GetComponent<Image>();
            if (bubble != null) { bg.sprite = bubble; bg.type = Image.Type.Sliced; bg.color = new Color(1, 1, 1, 0.95f); }
            else bg.color = new Color(0.15f, 0.10f, 0.22f, 0.92f);

            // Top-strip ribbon: "TODAY'S TRIAL"
            var ribbon = new GameObject("Ribbon", typeof(RectTransform), typeof(Image));
            ribbon.transform.SetParent(card.transform, false);
            var rrt = ribbon.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1);
            rrt.anchoredPosition = new Vector2(0, -8);
            rrt.sizeDelta = new Vector2(-40, 22);
            ribbon.GetComponent<Image>().color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.95f);
            MakeText(ribbon.transform, "RibTxt", "TODAY'S TRIAL",
                12, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Glyph circle (left)
            var glyphBg = new GameObject("GlyphBg", typeof(RectTransform), typeof(Image));
            glyphBg.transform.SetParent(card.transform, false);
            var grt = glyphBg.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0, 0.5f);
            grt.anchoredPosition = new Vector2(14, -10);
            grt.sizeDelta = new Vector2(56, 56);
            var gImg = glyphBg.GetComponent<Image>();
            gImg.color = new Color(0.85f, 0.40f, 0.45f);

            var glyph = MakeText(glyphBg.transform, "Glyph", "S",
                32, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Title
            var title = MakeText(card.transform, "Title", "Forest Patrol",
                18, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(80, -38), new Vector2(-200, 24));
            title.alignment = TextAlignmentOptions.MidlineLeft;

            // Subtitle
            var sub = MakeText(card.transform, "Sub", "Slay 3 forest goblins",
                13, FontStyles.Normal, new Color(0.25f, 0.20f, 0.10f),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(80, -62), new Vector2(-200, 18));
            sub.alignment = TextAlignmentOptions.MidlineLeft;

            // Reward
            var reward = MakeText(card.transform, "Reward", "+30 XP",
                14, FontStyles.Bold, new Color(0.55f, 0.30f, 0.05f),
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(80, -82), new Vector2(-200, 18));
            reward.alignment = TextAlignmentOptions.MidlineLeft;

            // BEGIN button (right)
            var begin = new GameObject("BeginBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            begin.transform.SetParent(card.transform, false);
            var brt = begin.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1, 0.5f); brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.anchoredPosition = new Vector2(-14, -8);
            brt.sizeDelta = new Vector2(110, 50);
            begin.GetComponent<Image>().color = GOLD;
            MakeText(begin.transform, "Lbl", "BEGIN",
                18, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Wire controller
            var ctrl = card.AddComponent<DailyTrialCard>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("title").objectReferenceValue    = title;
            so.FindProperty("subtitle").objectReferenceValue = sub;
            so.FindProperty("glyph").objectReferenceValue    = glyph;
            so.FindProperty("reward").objectReferenceValue   = reward;
            so.FindProperty("glyphBg").objectReferenceValue  = gImg;
            so.FindProperty("beginBtn").objectReferenceValue = begin.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            // Render order: above environment, below currency/stats chrome
            card.transform.SetSiblingIndex(2);
        }

        // ───────────────────── Helpers ─────────────────────
        private static Canvas FindCanvas()
        {
            var named = GameObject.Find("UI Canvas");
            if (named != null) { var c = named.GetComponent<Canvas>(); if (c != null) return c; }
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.renderMode == RenderMode.ScreenSpaceOverlay
                 || c.renderMode == RenderMode.ScreenSpaceCamera) return c;
            return null;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
            return tm;
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
}
