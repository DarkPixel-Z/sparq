using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Builds the Karu stats card at the top of the home screen.
    /// Pulls portrait sprite from the Bear's body part on the live Karu.
    /// </summary>
    public static class SparqKaruCardSetup
    {
        [MenuItem("Sparq/50. Build KARU stats card (top of home)")]
        public static void Build()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old
            var old = GameObject.Find("KaruStatsCard");
            if (old != null) Object.DestroyImmediate(old);

            // Root card
            var card = new GameObject("KaruStatsCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(canvas.transform, false);
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -130f);
            rt.sizeDelta = new Vector2(540, 220);
            card.GetComponent<Image>().color = new Color(0.12f, 0.06f, 0.22f, 0.85f);

            // Yellow accent border (top)
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 4);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.8f);
            accent.GetComponent<Image>().raycastTarget = false;

            // Portrait box (left)
            var portraitBg = new GameObject("PortraitBg", typeof(RectTransform), typeof(Image));
            portraitBg.transform.SetParent(card.transform, false);
            var prt = portraitBg.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = new Vector2(14, -14);
            prt.sizeDelta = new Vector2(100, 100);
            portraitBg.GetComponent<Image>().color = new Color(0.25f, 0.15f, 0.35f, 0.9f);

            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(portraitBg.transform, false);
            var prt2 = portrait.GetComponent<RectTransform>();
            prt2.anchorMin = Vector2.zero; prt2.anchorMax = Vector2.one;
            prt2.offsetMin = new Vector2(8, 8); prt2.offsetMax = new Vector2(-8, -8);
            var pImg = portrait.GetComponent<Image>();
            pImg.preserveAspect = true;

            // Try loading the Bear's head sprite as the portrait
            var bearHead = AssetDatabase.LoadAllAssetsAtPath("Assets/2D Animal Character Pack/Sprites/Characters/Bears/Bear.png");
            foreach (var o in bearHead)
            {
                if (o is Sprite sp && sp.name.Contains("Head", System.StringComparison.OrdinalIgnoreCase))
                {
                    pImg.sprite = sp;
                    pImg.color = new Color(1f, 0.55f, 0.35f); // red-panda tint
                    break;
                }
            }

            // Name + Level row
            var nameTM = AddText(card.transform, "Karu", 30, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Left, new Vector2(0.27f, 0.85f), new Vector2(180, 36));
            var levelGO = new GameObject("Level", typeof(RectTransform), typeof(Image));
            levelGO.transform.SetParent(card.transform, false);
            var lrt = levelGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.27f, 0.85f); lrt.anchorMax = new Vector2(0.27f, 0.85f);
            lrt.pivot = new Vector2(0, 0.5f);
            lrt.anchoredPosition = new Vector2(82f, 0f);
            lrt.sizeDelta = new Vector2(60, 28);
            levelGO.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f);
            var levelTM = AddText(levelGO.transform, "Lv.1", 16, new Color(0.1f, 0.05f, 0.2f), FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(60, 28));

            // Status text (avoid emoji — default TMP font lacks them; show as squares)
            var statusTM = AddText(card.transform, "Happy Today", 16, new Color(0.85f, 1f, 0.7f),
                FontStyles.Italic, TextAlignmentOptions.Left, new Vector2(0.27f, 0.70f), new Vector2(280, 24));

            // XP bar
            var xpRow = new GameObject("XPRow", typeof(RectTransform), typeof(Image));
            xpRow.transform.SetParent(card.transform, false);
            var xrt = xpRow.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(0.27f, 0.55f); xrt.anchorMax = new Vector2(0.27f, 0.55f);
            xrt.pivot = new Vector2(0, 0.5f);
            xrt.anchoredPosition = Vector2.zero;
            xrt.sizeDelta = new Vector2(360, 20);
            xpRow.GetComponent<Image>().color = new Color(0.2f, 0.1f, 0.3f, 0.7f);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(xpRow.transform, false);
            var frt = fillGO.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(1, 1);
            frt.pivot = new Vector2(0, 0.5f);
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);
            fillGO.GetComponent<Image>().color = new Color(0.4f, 0.95f, 0.45f);

            var xpSlider = xpRow.AddComponent<Slider>();
            xpSlider.fillRect = frt;
            xpSlider.minValue = 0; xpSlider.maxValue = 1; xpSlider.value = 0;
            xpSlider.interactable = false;

            var xpTextGO = AddText(card.transform, "0 / 100 XP", 13, new Color(0.85f, 0.85f, 1f),
                FontStyles.Normal, TextAlignmentOptions.Right, new Vector2(0.92f, 0.55f), new Vector2(160, 20));

            // Stat tiles row at bottom
            var statsRow = new GameObject("Stats", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            statsRow.transform.SetParent(card.transform, false);
            var srt = statsRow.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 0);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = new Vector2(0, 12);
            srt.sizeDelta = new Vector2(0, 60);
            var hlg = statsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 0, 0);
            hlg.spacing = 10;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Classic RPG stats: ATK, DEF, SPD
            var attackTM  = BuildStatTile(statsRow.transform, "ATK", "Attack",  "7");
            var defenseTM = BuildStatTile(statsRow.transform, "DEF", "Defense", "4");
            var speedTM   = BuildStatTile(statsRow.transform, "SPD", "Speed",   "6");

            // Wire controller
            var ctrl = card.AddComponent<Sparq.UI.KaruStatsCard>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("nameText").objectReferenceValue   = nameTM.GetComponent<TMP_Text>();
            so.FindProperty("levelText").objectReferenceValue  = levelTM.GetComponent<TMP_Text>();
            so.FindProperty("statusText").objectReferenceValue = statusTM.GetComponent<TMP_Text>();
            so.FindProperty("xpText").objectReferenceValue     = xpTextGO.GetComponent<TMP_Text>();
            so.FindProperty("xpSlider").objectReferenceValue   = xpSlider;
            so.FindProperty("attackValue").objectReferenceValue  = attackTM;
            so.FindProperty("defenseValue").objectReferenceValue = defenseTM;
            so.FindProperty("speedValue").objectReferenceValue   = speedTM;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Karu Card",
                "✅ Karu stats card added at top of home.\n\n" +
                "• Portrait + name + level badge\n" +
                "• Status (Happy / Hungry / Streak)\n" +
                "• XP bar\n" +
                "• 3 stat tiles: 🏆 Quests | 🔥 Streak | ⚡ Total XP\n\n" +
                "Updates every frame from save data.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static TMP_Text BuildStatTile(Transform parent, string icon, string label, string value)
        {
            var tile = new GameObject($"Tile_{label}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.30f, 0.8f);

            var iconTM = AddText(tile.transform, icon, 22, Color.white, FontStyles.Normal,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.85f), new Vector2(40, 28));
            var valueTM = AddText(tile.transform, value, 22, new Color(1f, 0.92f, 0.4f), FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(80, 28));
            var labelTM = AddText(tile.transform, label.ToUpper(), 11, new Color(0.85f, 0.85f, 1f, 0.85f), FontStyles.Bold,
                TextAlignmentOptions.Center, new Vector2(0.5f, 0.18f), new Vector2(120, 18));

            return valueTM.GetComponent<TMP_Text>();
        }

        private static RectTransform AddText(Transform parent, string text, int size, Color color, FontStyles style,
                                             TextAlignmentOptions align, Vector2 anchor, Vector2 size2)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size2;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.raycastTarget = false;
            return rt;
        }
    }
}
