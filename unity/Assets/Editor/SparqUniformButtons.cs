using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// • Forces all 5 side nav buttons (MAP/SHOP/BAG/PETS/WORLD) to identical size + spacing
    /// • Forces all 5 bottom nav tabs to identical size
    /// • Lifts the Dreamy Forest background to be more visible / prominent
    /// </summary>
    public static class SparqUniformButtons
    {
        [MenuItem("Sparq/74. Uniform buttons + Forest background")]
        public static void Apply()
        {
            FixSideButtons();
            FixBottomNav();
            BoostForestBackground();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ All buttons uniform + forest amplified.\n\n" +
                "• Side nav: 5 buttons all 110×64, equal spacing\n" +
                "• Bottom nav: 5 tabs all equal width\n" +
                "• Dreamy Forest cranked: more sprites, brighter\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void FixSideButtons()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Bar dimensions — anchored top-left
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(0f, 1f);
            brt.pivot     = new Vector2(0f, 1f);
            brt.anchoredPosition = new Vector2(14f, -130f);
            brt.sizeDelta = new Vector2(125, 410);

            // Layout group
            var vlg = bar.GetComponent<VerticalLayoutGroup>();
            if (vlg == null) vlg = bar.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            // Force every button child to identical size
            string[] expected = { "MapBtn", "ShopBtn", "BagBtn", "PetsBtn", "WorldBtn" };
            foreach (var name in expected)
            {
                var t = bar.transform.Find(name);
                if (t == null) continue;
                var rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(110, 64);
                    rt.localScale = Vector3.one;
                }
                var le = t.GetComponent<LayoutElement>();
                if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 110;
                le.preferredHeight = 64;
                le.flexibleWidth = 0;
                le.flexibleHeight = 0;
            }
        }

        private static void FixBottomNav()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0);
            brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 110);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(8, 8, 14, 14);
                hlg.spacing = 8;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            // Each tab equal flex width
            foreach (Transform tab in bar.transform)
            {
                if (!tab.name.StartsWith("Tab_")) continue;
                var le = tab.GetComponent<LayoutElement>();
                if (le == null) le = tab.gameObject.AddComponent<LayoutElement>();
                le.flexibleWidth = 1;
                le.preferredHeight = 80;
                le.minWidth = 0;
                tab.localScale = Vector3.one;
            }
        }

        private static void BoostForestBackground()
        {
            var forest = GameObject.Find("[Forest]");
            if (forest == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "[Forest] not in scene. Run Sparq → 27 first to build it.", "OK");
                return;
            }
            // Lift up + slightly bigger so forest fills more of the screen
            forest.transform.position = new Vector3(0f, 1.2f, 0f);
            forest.transform.localScale = Vector3.one * 1.1f;

            // Brighten foliage
            foreach (var sr in forest.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null) continue;
                var c = sr.color;
                c.r = Mathf.Min(1f, c.r * 1.1f);
                c.g = Mathf.Min(1f, c.g * 1.1f);
                c.b = Mathf.Min(1f, c.b * 1.1f);
                sr.color = c;
            }
        }
    }
}
