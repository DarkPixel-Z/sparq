using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Final layout pass:
    /// • Hide the bottom green XP bar entirely (Karu card has its own XP)
    /// • Fix stat tile widths (ATK/DEF/SPD wrapping)
    /// • Move Karu card to top center, full width
    /// • Move Quest list below Karu card (no overlap)
    /// </summary>
    public static class SparqLayoutFix
    {
        [MenuItem("Sparq/54. FIX layout (hide bottom bar + fix stats + reposition)")]
        public static void Apply()
        {
            HideBottomXPBar();
            FixStatTiles();
            RepositionKaruCard();
            RepositionQuests();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout Fix",
                "✅ Layout cleaned up:\n\n" +
                "• Bottom green XP bar HIDDEN (Karu card has the XP)\n" +
                "• Stat tiles widened — ATK/DEF/SPD no longer wrap\n" +
                "• Karu card top-center, full width\n" +
                "• Quest list moved below Karu card\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void HideBottomXPBar()
        {
            var bar = GameObject.Find("FantasyXPBar");
            if (bar != null) bar.SetActive(false);
        }

        private static void FixStatTiles()
        {
            var card = GameObject.Find("KaruStatsCard");
            if (card == null) return;

            // Walk the stats row and ensure each stat tile's text doesn't wrap
            var statsRow = card.transform.Find("Stats");
            if (statsRow == null) return;

            foreach (Transform tile in statsRow)
            {
                // Each tile has 3 TMP_Text children: icon, value, label
                foreach (var tmp in tile.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                    // Make label wider/cleaner
                    var rt = tmp.rectTransform;
                    if (rt != null)
                    {
                        var sd = rt.sizeDelta;
                        if (sd.x < 100) rt.sizeDelta = new Vector2(140, sd.y);
                    }
                }
            }
        }

        private static void RepositionKaruCard()
        {
            var card = GameObject.Find("KaruStatsCard");
            if (card == null) return;
            var rt = card.GetComponent<RectTransform>();

            // Stretch wider, anchor at top center, push down a bit
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -130f);
            rt.sizeDelta = new Vector2(580, 230);
        }

        private static void RepositionQuests()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null) return;
            var qrt = ql.GetComponent<RectTransform>();
            if (qrt == null) return;

            // Quests sit below Karu card, top-center
            qrt.anchorMin = new Vector2(0.5f, 1f);
            qrt.anchorMax = new Vector2(0.5f, 1f);
            qrt.pivot     = new Vector2(0.5f, 1f);
            qrt.anchoredPosition = new Vector2(0f, -380f); // below the karu card (which ends ~ -360)
            qrt.sizeDelta = new Vector2(580, 320);
        }
    }
}
