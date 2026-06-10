using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Builds the Sparq WebView-style bottom nav: Home | Journal | Remind | Feed | Profile
    /// • 5 evenly-spaced tabs at the bottom of the canvas
    /// • Active tab indicator (small gold underline)
    /// • Icon + label per tab
    /// • Hides the side MAP/SHOP/BAG/PETS bar (or moves it to top-left small)
    /// </summary>
    public static class SparqBottomNav
    {
        [MenuItem("Sparq/49. Build BOTTOM NAV (Home/Journal/Remind/Feed/Profile)")]
        public static void Build()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Remove old nav if present
            var old = GameObject.Find("BottomNav");
            if (old != null) Object.DestroyImmediate(old);

            // Root bar
            var bar = new GameObject("BottomNav", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(canvas.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 110);

            var bImg = bar.GetComponent<Image>();
            bImg.color = new Color(0.12f, 0.05f, 0.22f, 0.95f);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 6, 6);
            hlg.spacing = 4;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Top accent line (yellow)
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(bar.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 3);
            accent.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.7f);
            accent.GetComponent<Image>().raycastTarget = false;
            // Skip this child in horizontal layout
            var le = accent.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // Add controller
            var ctrl = bar.AddComponent<Sparq.UI.BottomNavBar>();

            // Build the 5 tabs
            foreach (var t in Sparq.UI.BottomNavBar.GetTabs())
            {
                BuildTab(bar.transform, t, ctrl);
            }

            // Move existing nav buttons (MAP/SHOP/BAG/PETS) to make sure they don't overlap with bottom nav
            var sideBar = GameObject.Find("HomeNavButtons");
            if (sideBar != null)
            {
                var srt = sideBar.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0f, 1f);
                srt.anchorMax = new Vector2(0f, 1f);
                srt.pivot     = new Vector2(0f, 1f);
                srt.anchoredPosition = new Vector2(14f, -120f);
            }

            // Pull XP bar up so the bottom nav doesn't cover it
            var xp = GameObject.Find("FantasyXPBar");
            if (xp != null)
            {
                var xrt = xp.GetComponent<RectTransform>();
                if (xrt != null)
                {
                    xrt.anchorMin = new Vector2(0.5f, 0f);
                    xrt.anchorMax = new Vector2(0.5f, 0f);
                    xrt.pivot = new Vector2(0.5f, 0f);
                    var pos = xrt.anchoredPosition;
                    xrt.anchoredPosition = new Vector2(pos.x, 130f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Bottom Nav",
                "✅ Bottom nav built!\n\n" +
                "• Home (active) | Journal | Remind | Feed | Profile\n" +
                "• Yellow accent strip on top\n" +
                "• Tap any tab → 'Coming Soon' floater (Home is the only working one)\n" +
                "• XP bar raised so nav doesn't cover it\n\n" +
                "Side MAP/SHOP/BAG/PETS still on the left.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildTab(Transform parent, Sparq.UI.BottomNavBar.TabConfig config, Sparq.UI.BottomNavBar ctrl)
        {
            var tabType = config.tab;
            string label = config.label;
            string icon  = config.icon;
            Color tint   = config.tint;

            var tabGO = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(Button));
            tabGO.transform.SetParent(parent, false);
            var le = tabGO.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 96;

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(tabGO.transform, false);
            var irt = iconGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(0, 14f);
            irt.sizeDelta = new Vector2(80, 50);
            var iTM = iconGO.AddComponent<TextMeshProUGUI>();
            iTM.text = icon;
            iTM.fontSize = 38;
            iTM.alignment = TextAlignmentOptions.Center;
            iTM.color = (tabType == ctrl.activeTab) ? tint : new Color(tint.r * 0.6f, tint.g * 0.6f, tint.b * 0.6f);
            iTM.raycastTarget = false;

            // Label
            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(tabGO.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0, -22f);
            lrt.sizeDelta = new Vector2(120, 24);
            var lTM = lblGO.AddComponent<TextMeshProUGUI>();
            lTM.text = label;
            lTM.fontSize = 16;
            lTM.alignment = TextAlignmentOptions.Center;
            lTM.color = (tabType == ctrl.activeTab) ? new Color(1f, 0.92f, 0.35f) : new Color(0.7f, 0.65f, 0.85f);
            lTM.fontStyle = (tabType == ctrl.activeTab) ? FontStyles.Bold : FontStyles.Normal;
            lTM.raycastTarget = false;

            // Active indicator dot (small yellow circle below label)
            var indicator = new GameObject("Active", typeof(RectTransform), typeof(Image));
            indicator.transform.SetParent(tabGO.transform, false);
            var arrt = indicator.GetComponent<RectTransform>();
            arrt.anchorMin = new Vector2(0.5f, 0f);
            arrt.anchorMax = new Vector2(0.5f, 0f);
            arrt.pivot = new Vector2(0.5f, 0.5f);
            arrt.anchoredPosition = new Vector2(0, 8);
            arrt.sizeDelta = new Vector2(28, 4);
            indicator.GetComponent<Image>().color = new Color(1f, 0.92f, 0.35f);
            indicator.GetComponent<Image>().raycastTarget = false;
            indicator.SetActive(tabType == ctrl.activeTab);

            // Tab button — captured value to avoid closure issue
            var btn = tabGO.GetComponent<Button>();
            var capturedTab = tabType;
            var capturedCtrl = ctrl;
            btn.onClick.AddListener(() => capturedCtrl.OnTabClicked(capturedTab));
            btn.transition = Selectable.Transition.None;

            // Register so controller can update visuals
            ctrl.RegisterTab(tabType, indicator, lTM);
        }
    }
}
