using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 134: Comprehensive layout cleanup.
    /// • Shrinks bottom nav so 6 tabs fit
    /// • Re-applies round candy top buttons (Hyper Casual)
    /// • Hard-clips quest list to 3 rows via Mask
    /// • Hides quest box by default (only appears when QUESTS tab tapped)
    /// • Restores Una help icon
    /// • Updates bottom nav text colors for readability
    /// </summary>
    public static class SparqLayoutCleanup134
    {
        private const string TN_DIR  = "Assets/[UIFabrica]TrollNest_Free_v01/02.PNG/";
        private const string UNA_PATH = "Assets/Art/Sparq/una-mage.png";

        // Palette
        private static readonly Color GOLD       = new Color(1.00f, 0.82f, 0.32f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/134. Final layout cleanup (bottom nav fit + quests hidden + Una back)")]
        public static void Apply()
        {
            ShrinkBottomNav();
            ClipQuestBoxWithMask();
            HideQuestBoxByDefault();
            RestoreUnaHelpIcon();
            FixBottomNavTextColors();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Cleanup applied:\n\n" +
                "• Bottom nav: 6 tabs fit in visible width\n" +
                "• Quest list: hard-clipped to 3 rows (Mask)\n" +
                "• Quest box: hidden by default (tap QUESTS to show)\n" +
                "• Una help icon: restored\n" +
                "• Bottom nav text: gold/cream for readability\n\n" +
                "Top buttons unchanged — re-run Sparq → 128 if you want round candy back.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Bottom nav: shrink to fit 6 tabs ─────────────────────
        private static void ShrinkBottomNav()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            // Shrink each tab wrapper
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var tab = bar.transform.GetChild(i);
                var le = tab.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;

                if (le != null)
                {
                    le.preferredWidth = 0;       // let HLG distribute
                    le.flexibleWidth  = 1;
                    le.minWidth       = 40;      // 6 tabs × 40 = 240 min
                    le.preferredHeight = 56;     // shorter
                }

                // Inner button rect: tighter padding so label fits
                var btn = tab.Find("Btn");
                if (btn != null)
                {
                    var brt = btn.GetComponent<RectTransform>();
                    if (brt != null)
                    {
                        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                        brt.offsetMin = new Vector2(1, 1); brt.offsetMax = new Vector2(-1, -1);
                    }
                }

                // Shrink label font so QUESTS/JOURNAL/PROFILE fit at narrow widths
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.enableAutoSizing = true;
                    tm.fontSizeMin = 8;
                    tm.fontSizeMax = 12;
                    tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    tm.overflowMode = TextOverflowModes.Overflow;
                    tm.margin = new Vector4(1, 0, 1, 0);
                }
            }

            // Tighten HLG
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(4, 4, 2, 2);
                hlg.spacing = 2;
            }
        }

        // ───────────────────── Quest list: Mask + height clamp ─────────────────────
        private static void ClipQuestBoxWithMask()
        {
            string[] candidates = { "QuestList", "QuestBox", "TodayQuests", "TodaysQuests", "QuestPanel" };
            GameObject list = null;
            foreach (var n in candidates) { var g = GameObject.Find(n); if (g != null) { list = g; break; } }
            if (list == null) return;

            // Walk up to the visible frame (parent with quest in name + Image)
            Transform frame = list.transform;
            if (frame.parent != null
                && frame.parent.GetComponent<Image>() != null
                && frame.parent.name.ToLower().Contains("quest"))
                frame = frame.parent;

            // Add a Mask to the frame so children get clipped to its rect
            var img = frame.GetComponent<Image>();
            if (img != null)
            {
                var mask = frame.GetComponent<Mask>();
                if (mask == null) mask = frame.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = true;
            }

            // Lock frame size so child VLG can't push it
            var rt = frame.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-12, -320);
                rt.sizeDelta = new Vector2(420, 200);
            }

            // Disable any ContentSizeFitter that grows the frame
            foreach (var f in frame.GetComponentsInChildren<ContentSizeFitter>())
            {
                f.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                f.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
        }

        // ───────────────────── Quest box: hidden by default ─────────────────────
        private static void HideQuestBoxByDefault()
        {
            string[] candidates = { "QuestList", "QuestBox", "TodayQuests", "TodaysQuests", "QuestPanel" };
            foreach (var n in candidates)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                Transform frame = go.transform;
                if (frame.parent != null
                    && frame.parent.GetComponent<Image>() != null
                    && frame.parent.name.ToLower().Contains("quest"))
                    frame = frame.parent;
                frame.gameObject.SetActive(false);
                return;
            }
        }

        // ───────────────────── Una help icon: restored ─────────────────────
        private static void RestoreUnaHelpIcon()
        {
            // If HelpIcon already visible, ensure it's active and positioned
            var help = GameObject.Find("HelpIcon");
            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null)
            {
                var c = Object.FindAnyObjectByType<Canvas>();
                if (c != null) canvas = c.gameObject;
            }
            if (canvas == null) return;

            if (help == null)
            {
                // Build minimal: round bg + Una sprite + ? badge
                var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TN_DIR + "trollnest_main-btn-normal.png");
                var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UNA_PATH);

                help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
                help.transform.SetParent(canvas.transform, false);
                var img = help.GetComponent<Image>();
                if (bgSprite != null) img.sprite = bgSprite;
                else img.color = new Color(0.4f, 0.2f, 0.6f);
                img.preserveAspect = true;

                if (unaSprite != null)
                {
                    var una = new GameObject("Una", typeof(RectTransform), typeof(Image));
                    una.transform.SetParent(help.transform, false);
                    var urt = una.GetComponent<RectTransform>();
                    urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                    urt.offsetMin = new Vector2(8, 8); urt.offsetMax = new Vector2(-8, -8);
                    var uimg = una.GetComponent<Image>();
                    uimg.sprite = unaSprite;
                    uimg.preserveAspect = true;
                    uimg.raycastTarget = false;
                }
            }
            help.SetActive(true);

            var hrt = help.GetComponent<RectTransform>();
            if (hrt != null)
            {
                hrt.anchorMin = new Vector2(0f, 0f);
                hrt.anchorMax = new Vector2(0f, 0f);
                hrt.pivot     = new Vector2(0f, 0f);
                hrt.anchoredPosition = new Vector2(10f, 100f);
                hrt.sizeDelta = new Vector2(72, 72);
            }
            help.transform.SetAsLastSibling(); // render on top
        }

        // ───────────────────── Bottom nav text colors ─────────────────────
        private static void FixBottomNavTextColors()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var tab = bar.transform.GetChild(i);
                var le = tab.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;

                // All tabs use the same look as HOME — dark navy bold text with cream outline
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = DEEP_NAVY;
                    tm.fontStyle = FontStyles.Bold;
                    tm.outlineWidth = 0.15f;
                    tm.outlineColor = new Color(1f, 0.95f, 0.75f, 0.9f);
                }
            }
        }
    }
}
