using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.Systems;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 142: Replaces the white mood bubble + Tome with one unified
    /// "Forge" panel. Forge button (hammer icon) on the top bar opens a
    /// fullscreen panel with Spirit Stone crystals + Wisdom cards.
    /// </summary>
    public static class SparqForgeRebuild142
    {
        private const string FH_ICON = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/";

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color GOLD      = new Color(1f, 0.82f, 0.32f);
        private static readonly Color PARCH     = new Color(0.18f, 0.13f, 0.10f, 0.96f);

        [MenuItem("Sparq/142. Rebuild as Forge (kill mood bubble + rename Tome)")]
        public static void Apply()
        {
            EnsureSprite(FH_ICON + "ItemIcon_Gear_Hammer.png");

            // 1. Kill the white mood bubble
            var mood = GameObject.Find("MoodPrompt");
            if (mood != null) Object.DestroyImmediate(mood);

            // 2. Wipe old clarity panel
            var oldPanel = GameObject.Find("ClarityPanel");
            if (oldPanel != null) Object.DestroyImmediate(oldPanel);

            var canvas = FindCanvas();
            if (canvas == null) { EditorUtility.DisplayDialog("Sparq", "No Canvas.", "OK"); return; }

            // 3. Build new Forge panel (mood section + wisdom cards)
            var panel = BuildForgePanel(canvas.transform);

            // 4. Rename top button TOME → FORGE w/ hammer icon, hooked to Forge panel
            RenameTomeToForge(panel);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Forge wired:\n\n" +
                "• White mood bubble removed\n" +
                "• Top button: TOME → FORGE (hammer icon)\n" +
                "• Tap FORGE → fullscreen 'The Forge' panel\n" +
                "  · top: Spirit Stone (5 mood crystals)\n" +
                "  · below: 6 wisdom cards (+5 XP each)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Forge panel ─────────────────────
        private static GameObject BuildForgePanel(Transform canvas)
        {
            var panel = new GameObject("ForgePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas, false);
            var prt = panel.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.05f, 0.04f, 0.10f, 0.92f);
            panel.SetActive(false);

            // Header
            var header = MakeText(panel.transform, "Header", "The Forge",
                28, FontStyles.Bold, GOLD,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28),
                new Vector2(800, 36));

            var sub = MakeText(panel.transform, "Sub", "hone your spirit · craft yourself stronger",
                13, FontStyles.Italic, new Color(0.85f, 0.82f, 0.65f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -56),
                new Vector2(800, 22));

            // Close X
            var close = MakeBtn(panel.transform, "Close", "X",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-16, -16), new Vector2(40, 40),
                new Color(0.4f, 0.2f, 0.6f), Color.white, 22);

            // ─── Spirit Stone section ───
            MakeText(panel.transform, "MoodHeader", "How fares your spirit?",
                18, FontStyles.Bold, CREAM,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100),
                new Vector2(800, 28));

            var crystalsRow = new GameObject("Crystals", typeof(RectTransform));
            crystalsRow.transform.SetParent(panel.transform, false);
            var crt = crystalsRow.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1); crt.anchorMax = new Vector2(0.5f, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = new Vector2(0, -132);
            crt.sizeDelta = new Vector2(680, 56);
            var hlg = crystalsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(0, 0, 0, 0);
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            var moodFeedback = MakeText(panel.transform, "MoodFeedback", "",
                14, FontStyles.Bold, GOLD,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -198),
                new Vector2(800, 22));

            // Pre-fill if logged today
            if (MoodService.LoggedToday())
                moodFeedback.text = $"✓ Spirit logged today  ·  {MoodService.StreakDays()}-day streak";

            for (int i = 0; i < MoodService.Crystals.Length; i++)
            {
                var (mood, lbl, color) = MoodService.Crystals[i];
                int idx = i;
                var btn = MakeCrystal(crystalsRow.transform, lbl, color);
                btn.onClick.AddListener(() =>
                {
                    MoodService.Log(MoodService.Crystals[idx].mood);
                    moodFeedback.text = $"✓ Spirit logged: {MoodService.Crystals[idx].label}  ·  {MoodService.StreakDays()}-day streak";
                });
            }

            // ─── Divider ───
            var div = new GameObject("Div", typeof(RectTransform), typeof(Image));
            div.transform.SetParent(panel.transform, false);
            var drt = div.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 1); drt.anchorMax = new Vector2(0.5f, 1);
            drt.pivot = new Vector2(0.5f, 1);
            drt.anchoredPosition = new Vector2(0, -228);
            drt.sizeDelta = new Vector2(700, 2);
            div.GetComponent<Image>().color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.4f);

            // ─── Wisdom cards section ───
            MakeText(panel.transform, "CardsHeader", "Wisdom Cards",
                18, FontStyles.Bold, CREAM,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -252),
                new Vector2(800, 28));

            MakeText(panel.transform, "CardsSub",
                $"{ClarityService.TotalPracticed} practiced  ·  +5 XP each",
                12, FontStyles.Normal, new Color(0.75f, 0.72f, 0.55f),
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -278),
                new Vector2(800, 20));

            var grid = new GameObject("Deck", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(panel.transform, false);
            var grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 1); grt.anchorMax = new Vector2(0.5f, 1);
            grt.pivot = new Vector2(0.5f, 1);
            grt.anchoredPosition = new Vector2(0, -300);
            grt.sizeDelta = new Vector2(680, 320);
            var glg = grid.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(210, 96);
            glg.spacing = new Vector2(12, 12);
            glg.padding = new RectOffset(0, 0, 0, 0);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;

            // Detail sub-panel
            var detail = BuildDetailPanel(panel.transform);

            // Wire ClarityPanel controller
            var ctrl = panel.AddComponent<ClarityPanel>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("deckRoot").objectReferenceValue       = grid.GetComponent<RectTransform>();
            so.FindProperty("cardDetailRoot").objectReferenceValue = detail.go;
            so.FindProperty("detailTitle").objectReferenceValue    = detail.title;
            so.FindProperty("detailBody").objectReferenceValue     = detail.body;
            so.FindProperty("detailPracticedCount").objectReferenceValue = detail.count;
            so.FindProperty("detailPracticeBtn").objectReferenceValue    = detail.practiceBtn;
            so.FindProperty("detailCloseBtn").objectReferenceValue       = detail.closeBtn;
            so.FindProperty("panelCloseBtn").objectReferenceValue        = close;
            so.FindProperty("headerLabel").objectReferenceValue          = header;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Build cards
            foreach (var card in ClarityService.Deck)
                BuildCardTile(grid.transform, card, ctrl);

            return panel;
        }

        private struct DetailRefs { public GameObject go; public TMP_Text title, body, count; public Button practiceBtn, closeBtn; }

        private static DetailRefs BuildDetailPanel(Transform parent)
        {
            var d = new GameObject("CardDetail", typeof(RectTransform), typeof(Image));
            d.transform.SetParent(parent, false);
            var rt = d.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            d.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.96f);

            var title = MakeText(d.transform, "Title", "—",
                32, FontStyles.Bold, GOLD,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100),
                new Vector2(800, 44));

            var body = MakeText(d.transform, "Body", "—",
                20, FontStyles.Normal, CREAM,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30),
                new Vector2(620, 220));
            body.alignment = TextAlignmentOptions.Center;
            body.textWrappingMode = TMPro.TextWrappingModes.Normal;

            var count = MakeText(d.transform, "Count", "Practiced 0×",
                14, FontStyles.Bold, new Color(0.75f, 0.72f, 0.55f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -130),
                new Vector2(300, 24));

            var practiceBtn = MakeBtn(d.transform, "PracticeBtn", "Practice (+5 XP)",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, 80), new Vector2(200, 56),
                GOLD, DEEP_NAVY, 16);

            var closeBtn = MakeBtn(d.transform, "Back", "Back",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(110, 80), new Vector2(200, 56),
                new Color(0.4f, 0.4f, 0.5f), CREAM, 16);

            d.SetActive(false);
            return new DetailRefs { go = d, title = title, body = body, count = count,
                practiceBtn = practiceBtn, closeBtn = closeBtn };
        }

        private static void BuildCardTile(Transform parent, ClarityService.Card card, ClarityPanel panel)
        {
            var tile = new GameObject(card.id + "Card",
                typeof(RectTransform), typeof(Image), typeof(Button));
            tile.transform.SetParent(parent, false);
            var col = card.tint;
            tile.GetComponent<Image>().color = new Color(col.r * 0.35f, col.g * 0.35f, col.b * 0.35f, 0.95f);

            // Glyph circle
            var glyphBg = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyphBg.transform.SetParent(tile.transform, false);
            var grt = glyphBg.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0, 0.5f);
            grt.anchoredPosition = new Vector2(8, 0);
            grt.sizeDelta = new Vector2(46, 46);
            glyphBg.GetComponent<Image>().color = card.tint;

            var g = MakeText(glyphBg.transform, "G", card.glyph,
                26, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var title = MakeText(tile.transform, "Title", card.title,
                14, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(30, -10), new Vector2(-72, 48));
            title.alignment = TextAlignmentOptions.TopLeft;
            title.textWrappingMode = TMPro.TextWrappingModes.Normal;

            string id = card.id;
            tile.GetComponent<Button>().onClick.AddListener(() => panel.OpenCard(id));
        }

        private static Button MakeCrystal(Transform parent, string label, Color color)
        {
            var go = new GameObject(label + "Crystal",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredWidth = 116; le.preferredHeight = 48;

            var img = go.GetComponent<Image>();
            img.color = color;

            MakeText(go.transform, "Lbl", label,
                14, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            return go.GetComponent<Button>();
        }

        // ───────────────────── Top button: rename to FORGE ─────────────────────
        private static void RenameTomeToForge(GameObject forgePanel)
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            Transform tome = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var c = bar.transform.GetChild(i);
                string n = c.name.ToLower();
                if (n.Contains("tome") || n.Contains("world"))
                { tome = c; break; }
            }
            if (tome == null) return;

            tome.name = "ForgeBtn";
            var iconTr = tome.Find("Icon");
            if (iconTr != null)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Gear_Hammer.png");
                var iimg = iconTr.GetComponent<Image>();
                if (iimg != null && sp != null) iimg.sprite = sp;
            }
            var lblTr = tome.Find("Label");
            if (lblTr != null)
                foreach (var tm in lblTr.GetComponentsInChildren<TMP_Text>(true))
                    tm.text = "FORGE";

            var btn = tome.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var p = forgePanel.GetComponent<ClarityPanel>();
                    if (p != null) p.Show();
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

        private static Button MakeBtn(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color bg, Color fg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;
            var tm = MakeText(go.transform, "Lbl", label,
                fontSize, FontStyles.Bold, fg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
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
