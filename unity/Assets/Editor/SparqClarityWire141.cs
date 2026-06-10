using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.Systems;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 141: Wires Spirit Stone (mood log) + Tome of Clarity (mental
    /// strength deck) into the scene.
    ///   • Adds floating mood prompt above the bottom nav
    ///   • Builds a fullscreen Clarity panel with 6 wisdom cards
    ///   • Replaces the WORLD top button with TOME (book icon → opens Clarity)
    /// </summary>
    public static class SparqClarityWire141
    {
        private const string FH_ICON = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/";
        private const string FH_LABEL = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/";

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f, 0.95f);
        private static readonly Color GOLD      = new Color(1f, 0.82f, 0.32f);

        [MenuItem("Sparq/141. Wire Spirit Stone + Tome of Clarity")]
        public static void Apply()
        {
            EnsureSprite(FH_ICON  + "ItemIcon_Book_1_Purple.png");
            EnsureSprite(FH_LABEL + "Label_Bubble_01_Bg.png");

            var canvas = FindCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Sparq", "No UI Canvas found.", "OK");
                return;
            }

            var prompt = BuildMoodPrompt(canvas.transform);
            var panel  = BuildClarityPanel(canvas.transform);
            ReplaceWorldWithTome(panel);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Wired:\n\n" +
                "• Spirit Stone mood prompt above bottom nav (5 crystals)\n" +
                "• Tome of Clarity panel with 6 wisdom cards\n" +
                "• Top WORLD button → TOME (purple book icon)\n\n" +
                "Each card practiced grants +5 XP.\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Mood prompt ─────────────────────
        private static GameObject BuildMoodPrompt(Transform canvas)
        {
            var old = GameObject.Find("MoodPrompt");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("MoodPrompt", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 80);     // sits above bottom nav
            rt.sizeDelta = new Vector2(-24, 64);

            var bg = go.GetComponent<Image>();
            var bubble = AssetDatabase.LoadAssetAtPath<Sprite>(FH_LABEL + "Label_Bubble_01_Bg.png");
            if (bubble != null) { bg.sprite = bubble; bg.type = Image.Type.Sliced; bg.color = Color.white; }
            else bg.color = DEEP_NAVY;

            var prompt = new GameObject("Label", typeof(RectTransform));
            prompt.transform.SetParent(go.transform, false);
            var prt = prompt.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1);
            prt.pivot = new Vector2(0.5f, 1);
            prt.anchoredPosition = new Vector2(0, -4);
            prt.sizeDelta = new Vector2(-16, 22);
            var ptm = prompt.AddComponent<TextMeshProUGUI>();
            ptm.text = "How fares your spirit?";
            ptm.fontSize = 14;
            ptm.fontStyle = FontStyles.Bold;
            ptm.color = DEEP_NAVY;
            ptm.alignment = TextAlignmentOptions.Center;
            ptm.raycastTarget = false;

            var streak = new GameObject("Streak", typeof(RectTransform));
            streak.transform.SetParent(go.transform, false);
            var srt = streak.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(1, 1);
            srt.anchoredPosition = new Vector2(-12, -4);
            srt.sizeDelta = new Vector2(110, 18);
            var stm = streak.AddComponent<TextMeshProUGUI>();
            stm.text = "";
            stm.fontSize = 11;
            stm.fontStyle = FontStyles.Bold;
            stm.color = GOLD;
            stm.alignment = TextAlignmentOptions.MidlineRight;
            stm.raycastTarget = false;
            streak.SetActive(false);

            // Crystal row
            var crystals = new GameObject("Crystals", typeof(RectTransform));
            crystals.transform.SetParent(go.transform, false);
            var crt = crystals.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 0);
            crt.pivot = new Vector2(0.5f, 0);
            crt.anchoredPosition = new Vector2(0, 4);
            crt.sizeDelta = new Vector2(-24, 36);
            var hlg = crystals.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var ctrl = go.AddComponent<MoodPrompt>();

            var crystalButtons = new System.Collections.Generic.List<Button>();
            for (int i = 0; i < MoodService.Crystals.Length; i++)
            {
                var (mood, label, color) = MoodService.Crystals[i];
                int idx = i;
                var btn = BuildCrystalButton(crystals.transform, label, color);
                btn.onClick.AddListener(() => ctrl.OnCrystalTapped(idx));
                crystalButtons.Add(btn);
            }

            // Wire SerializedObject fields
            var so = new SerializedObject(ctrl);
            so.FindProperty("crystalsRoot").objectReferenceValue = crystals;
            so.FindProperty("promptLabel").objectReferenceValue  = ptm;
            so.FindProperty("streakLabel").objectReferenceValue  = stm;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static Button BuildCrystalButton(Transform parent, string label, Color color)
        {
            var go = new GameObject(label + "Crystal", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 56; le.preferredHeight = 32;

            var img = go.GetComponent<Image>();
            img.color = color;

            var lbl = new GameObject("Lbl", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 11;
            tm.fontStyle = FontStyles.Bold;
            tm.color = DEEP_NAVY;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        // ───────────────────── Clarity panel ─────────────────────
        private static GameObject BuildClarityPanel(Transform canvas)
        {
            var old = GameObject.Find("ClarityPanel");
            if (old != null) Object.DestroyImmediate(old);

            var panel = new GameObject("ClarityPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            panel.SetActive(false);

            // Header
            var header = BuildPanelText(panel.transform, "Header",
                "Tome of Clarity", 22, FontStyles.Bold,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(0, -28),
                new Vector2(800, 36));
            header.color = GOLD;

            // Close X
            var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            close.transform.SetParent(panel.transform, false);
            var crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(1, 1);
            crt.anchoredPosition = new Vector2(-16, -16);
            crt.sizeDelta = new Vector2(40, 40);
            close.GetComponent<Image>().color = new Color(0.4f, 0.2f, 0.6f);
            BuildPanelText(close.transform, "X", "X", 22, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).color = Color.white;

            // Card grid (2 cols × 3 rows)
            var grid = new GameObject("Deck", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(panel.transform, false);
            var grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f); grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0, 0);
            grt.sizeDelta = new Vector2(640, 480);
            var glg = grid.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(200, 140);
            glg.spacing = new Vector2(16, 16);
            glg.padding = new RectOffset(8, 8, 8, 8);
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;

            // Detail sub-panel (hidden until a card tapped)
            var detail = BuildDetailSubPanel(panel.transform);

            // Wire panel controller
            var ctrl = panel.AddComponent<ClarityPanel>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("deckRoot").objectReferenceValue       = grid.GetComponent<RectTransform>();
            so.FindProperty("cardDetailRoot").objectReferenceValue = detail.go;
            so.FindProperty("detailTitle").objectReferenceValue    = detail.title;
            so.FindProperty("detailBody").objectReferenceValue     = detail.body;
            so.FindProperty("detailPracticedCount").objectReferenceValue = detail.count;
            so.FindProperty("detailPracticeBtn").objectReferenceValue = detail.practiceBtn;
            so.FindProperty("detailCloseBtn").objectReferenceValue = detail.closeBtn;
            so.FindProperty("panelCloseBtn").objectReferenceValue  = close.GetComponent<Button>();
            so.FindProperty("headerLabel").objectReferenceValue    = header;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Build 6 cards in the grid, wire each to OpenCard
            foreach (var card in ClarityService.Deck)
                BuildCardTile(grid.transform, card, ctrl);

            return panel;
        }

        private struct DetailRefs { public GameObject go; public TMP_Text title, body, count; public Button practiceBtn, closeBtn; }

        private static DetailRefs BuildDetailSubPanel(Transform parent)
        {
            var d = new GameObject("CardDetail", typeof(RectTransform), typeof(Image));
            d.transform.SetParent(parent, false);
            var rt = d.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            d.GetComponent<Image>().color = new Color(0, 0, 0, 0.92f);

            var title = BuildPanelText(d.transform, "Title", "—", 28, FontStyles.Bold,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90),
                new Vector2(640, 40));
            title.color = GOLD;

            var body = BuildPanelText(d.transform, "Body", "—", 18, FontStyles.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30),
                new Vector2(560, 200));
            body.color = CREAM;
            body.alignment = TextAlignmentOptions.Center;

            var count = BuildPanelText(d.transform, "Count", "Practiced 0×", 14, FontStyles.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -120),
                new Vector2(300, 24));
            count.color = new Color(0.7f, 0.7f, 0.8f);

            var practiceBtn = BuildBigButton(d.transform, "PracticeBtn",  "Practice (+5 XP)",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-110, 80), new Vector2(200, 56), GOLD);

            var closeBtn = BuildBigButton(d.transform, "BackBtn", "Back",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2( 110, 80), new Vector2(200, 56), new Color(0.5f, 0.5f, 0.6f));

            d.SetActive(false);

            return new DetailRefs {
                go = d, title = title, body = body, count = count,
                practiceBtn = practiceBtn, closeBtn = closeBtn
            };
        }

        private static void BuildCardTile(Transform parent, ClarityService.Card card, ClarityPanel panel)
        {
            var tile = new GameObject(card.id + "Card",
                typeof(RectTransform), typeof(Image), typeof(Button));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Image>().color = new Color(card.tint.r * 0.4f, card.tint.g * 0.4f, card.tint.b * 0.4f, 0.95f);

            var glyphBg = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyphBg.transform.SetParent(tile.transform, false);
            var grt = glyphBg.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0, 0.5f);
            grt.anchoredPosition = new Vector2(10, 0);
            grt.sizeDelta = new Vector2(56, 56);
            glyphBg.GetComponent<Image>().color = card.tint;

            var glyph = new GameObject("G", typeof(RectTransform));
            glyph.transform.SetParent(glyphBg.transform, false);
            var gtRT = glyph.GetComponent<RectTransform>();
            gtRT.anchorMin = Vector2.zero; gtRT.anchorMax = Vector2.one;
            gtRT.offsetMin = Vector2.zero; gtRT.offsetMax = Vector2.zero;
            var gtm = glyph.AddComponent<TextMeshProUGUI>();
            gtm.text = card.glyph;
            gtm.fontSize = 32;
            gtm.fontStyle = FontStyles.Bold;
            gtm.color = DEEP_NAVY;
            gtm.alignment = TextAlignmentOptions.Center;
            gtm.raycastTarget = false;

            var title = BuildPanelText(tile.transform, "Title", card.title, 16, FontStyles.Bold,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -10),
                new Vector2(-90, 48));
            title.color = CREAM;
            title.alignment = TextAlignmentOptions.TopLeft;
            title.textWrappingMode = TMPro.TextWrappingModes.Normal;

            string id = card.id;
            tile.GetComponent<Button>().onClick.AddListener(() => panel.OpenCard(id));
        }

        // ───────────────────── Tome top button (replaces WORLD) ─────────────────────
        private static void ReplaceWorldWithTome(GameObject clarityPanel)
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Find WorldBtn child
            Transform world = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var c = bar.transform.GetChild(i);
                if (c.name.ToLower().Contains("world"))
                { world = c; break; }
            }
            if (world == null) return;

            // Rename + swap icon
            world.name = "TomeBtn";
            var iconTr = world.Find("Icon");
            if (iconTr != null)
            {
                var bookSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Book_1_Purple.png");
                var iimg = iconTr.GetComponent<Image>();
                if (iimg != null && bookSprite != null) iimg.sprite = bookSprite;
            }
            var lblTr = world.Find("Label");
            if (lblTr != null)
            {
                foreach (var tm in lblTr.GetComponentsInChildren<TMP_Text>(true))
                    tm.text = "TOME";
            }

            // Wire click → open ClarityPanel
            var btn = world.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var panel = clarityPanel.GetComponent<ClarityPanel>();
                    if (panel != null) panel.Show();
                });
            }
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

        private static TMP_Text BuildPanelText(Transform parent, string name, string text, float size, FontStyles style,
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
            tm.color = CREAM;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
            return tm;
        }

        private static Button BuildBigButton(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            go.GetComponent<Image>().color = color;
            var lbl = BuildPanelText(go.transform, "Lbl", label, 16, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            lbl.color = DEEP_NAVY;
            return go.GetComponent<Button>();
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
