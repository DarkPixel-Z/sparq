using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Moves MAP/SHOP/BAG/PETS/WORLD from a left vertical strip to a TOP horizontal strip.
    /// Smaller buttons, uniform 16pt font, evenly spaced.
    /// Frees the left side of the home screen for the bear/forest.
    /// </summary>
    public static class SparqTopButtonStrip
    {
        [MenuItem("Sparq/76. Buttons → top horizontal strip (smaller, uniform)")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK");
                return;
            }

            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1f);
            brt.anchorMax = new Vector2(0.5f, 1f);
            brt.pivot     = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0f, -120f);  // sits below the logo & Karu HUD
            brt.sizeDelta = new Vector2(540, 50);

            // Replace VerticalLayout with HorizontalLayout
            var vlg = bar.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Object.DestroyImmediate(vlg);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Force uniform size + uniform font on each button
            string[] btnNames = { "MapBtn", "ShopBtn", "BagBtn", "PetsBtn", "WorldBtn" };
            foreach (var name in btnNames)
            {
                var t = bar.transform.Find(name);
                if (t == null) continue;

                var rt = t.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(96, 44);
                    rt.localScale = Vector3.one;
                }
                var le = t.GetComponent<LayoutElement>();
                if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 96;
                le.preferredHeight = 44;
                le.flexibleWidth = 0;
                le.flexibleHeight = 0;

                // Force uniform 16pt bold on every text inside the button
                foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    tmp.fontSize = 16;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                    tmp.overflowMode = TextOverflowModes.Overflow;
                }
            }

            // Make sure the SPARQ logo + Karu HUD don't overlap the new strip
            var logo = GameObject.Find("GameTitle");
            if (logo != null)
            {
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(20f, -20f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Buttons",
                "✅ Buttons moved to top horizontal strip.\n\n" +
                "• MAP / SHOP / BAG / PETS / WORLD across the top\n" +
                "• All 96×44, uniform size\n" +
                "• Uniform 16pt bold font\n" +
                "• 6px spacing between\n" +
                "• Centered top, below logo + HUD\n\n" +
                "Left side is now CLEAR — bear + forest get the spotlight.\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
