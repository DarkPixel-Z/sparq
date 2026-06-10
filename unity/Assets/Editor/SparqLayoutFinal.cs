using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Final layout pass v3:
    /// • Move quest box to top-RIGHT under the Karu HUD (smaller)
    /// • Replace bottom nav emoji icons (which render as squares) with simple text glyphs
    /// </summary>
    public static class SparqLayoutFinal
    {
        [MenuItem("Sparq/58. FINAL layout (quest right + nav text icons)")]
        public static void Apply()
        {
            FixQuestPosition();
            FixBottomNavIcons();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ Final layout applied:\n\n" +
                "• Quest box now top-RIGHT under Karu HUD\n" +
                "• Compact 320×260 footprint\n" +
                "• Bottom nav icons replaced with ASCII labels (no more squares)\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void FixQuestPosition()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null) return;
            var qrt = ql.GetComponent<RectTransform>();

            qrt.anchorMin = new Vector2(1f, 1f);
            qrt.anchorMax = new Vector2(1f, 1f);
            qrt.pivot     = new Vector2(1f, 1f);
            qrt.anchoredPosition = new Vector2(-14f, -100f);  // right edge, just below HUD (HUD is ~70px)
            qrt.sizeDelta = new Vector2(320, 260);

            var bg = ql.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = new Color(0.10f, 0.05f, 0.20f, 0.78f);
            }

            // Compact title
            foreach (var tmp in ql.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.text == "Today's Quests")
                {
                    tmp.fontSize = 16;
                    tmp.color = new Color(1f, 0.85f, 0.4f);
                    tmp.fontStyle = FontStyles.Bold;
                }
            }
        }

        private static void FixBottomNavIcons()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            // Map of tab name → ASCII glyph that the default font supports
            var iconMap = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Home",    "[H]" },
                { "Journal", "[J]" },
                { "Remind",  "[R]" },
                { "Feed",    "[F]" },
                { "Profile", "[P]" },
            };

            foreach (Transform tab in bar.transform)
            {
                if (!tab.name.StartsWith("Tab_")) continue;
                string tabName = tab.name.Substring(4); // "Home" / "Journal" etc.

                // Find the icon TMP (we set big fontSize=38)
                foreach (var tmp in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (tmp.fontSize >= 36 && iconMap.TryGetValue(tabName, out var glyph))
                    {
                        tmp.text = glyph;
                        tmp.fontSize = 28;
                        tmp.fontStyle = FontStyles.Bold;
                    }
                }
            }
        }
    }
}
