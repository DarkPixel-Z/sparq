using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Final top layout:
    ///   • Buttons at the absolute top of the screen
    ///   • Sparq logo smaller, below buttons
    ///   • Karu HUD smaller, below buttons (top-right)
    ///   • Quest list shifts down accordingly
    /// </summary>
    public static class SparqTopLayoutFinal
    {
        [MenuItem("Sparq/77. TOP layout (buttons very top + smaller logo)")]
        public static void Apply()
        {
            // 1. Move buttons to the very top — full width strip
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 1f);
                brt.anchorMax = new Vector2(0.5f, 1f);
                brt.pivot     = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -8f);   // very top, 8px from edge
                brt.sizeDelta = new Vector2(540, 44);

                // smaller buttons
                string[] btnNames = { "MapBtn", "ShopBtn", "BagBtn", "PetsBtn", "WorldBtn" };
                foreach (var name in btnNames)
                {
                    var t = bar.transform.Find(name);
                    if (t == null) continue;
                    var rt = t.GetComponent<RectTransform>();
                    if (rt != null) rt.sizeDelta = new Vector2(96, 38);
                    var le = t.GetComponent<LayoutElement>();
                    if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 96;
                    le.preferredHeight = 38;
                    foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tmp.fontSize = 14;
                        tmp.fontStyle = FontStyles.Bold;
                    }
                }
            }

            // 2. Logo smaller + below the button strip
            var logo = GameObject.Find("GameTitle");
            if (logo != null)
            {
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 1f); lrt.anchorMax = new Vector2(0f, 1f);
                lrt.pivot = new Vector2(0f, 1f);
                lrt.anchoredPosition = new Vector2(14f, -60f);  // below buttons
                lrt.sizeDelta = new Vector2(180, 60);

                // Shrink the text inside
                foreach (var tmp in logo.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    tmp.fontSize = 38;
                }
                // Shrink the bolt
                var bolt = logo.transform.Find("Bolt");
                if (bolt != null)
                {
                    var brt2 = bolt.GetComponent<RectTransform>();
                    if (brt2 != null) brt2.sizeDelta = new Vector2(36, 50);
                    brt2.anchoredPosition = new Vector2(126, 4);
                }
                // Shift word container size to fit smaller text
                var wc = logo.transform.Find("WordContainer");
                if (wc != null)
                {
                    var wcRT = wc.GetComponent<RectTransform>();
                    if (wcRT != null) wcRT.sizeDelta = new Vector2(125, 0);
                }
            }

            // 3. Karu HUD - shift down a bit to clear button strip
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchoredPosition = new Vector2(-14f, -60f);
                hrt.sizeDelta = new Vector2(220, 64);
            }

            // 4. Quest list — shift down to clear HUD
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchoredPosition = new Vector2(-14f, -140f);
                qrt.sizeDelta = new Vector2(320, 250);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ Top layout applied:\n\n" +
                "• Buttons at very top (96×38 each, 14pt font)\n" +
                "• Sparq logo smaller (180×60, 38pt) below buttons\n" +
                "• Karu HUD smaller (220×64) below buttons\n" +
                "• Quest list 320×250 below HUD\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
