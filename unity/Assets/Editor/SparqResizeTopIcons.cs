using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 200: Resize the existing MAP/SHOP/BAG/PETS/WORLD top-button icons
    /// in-place without rebuilding the whole nav (no LayoutGroup conflicts).
    /// </summary>
    public static class SparqResizeTopIcons
    {
        [MenuItem("Sparq/200. Resize + reposition top-nav buttons")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Couldn't find 'HomeNavButtons' in the scene.\nAre you in the home scene with the top buttons?", "OK");
                return;
            }

            // 0. Force the layout group to NOT expand children
            var hlg = bar.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
                hlg.spacing = 4;
                hlg.padding = new RectOffset(4, 4, 3, 3);
                hlg.childAlignment = TextAnchor.MiddleCenter;
            }

            // 1. Shrink + reposition the bar itself so it tucks under the stat box
            var barRT = bar.GetComponent<RectTransform>();
            if (barRT != null)
            {
                barRT.anchorMin = new Vector2(1f, 1f);
                barRT.anchorMax = new Vector2(1f, 1f);
                barRT.pivot     = new Vector2(1f, 1f);
                barRT.anchoredPosition = new Vector2(-12, -135);
                barRT.sizeDelta = new Vector2(320, 52);
            }

            // 2. Shrink each button's preferred height + the icon inside
            int touched = 0;
            foreach (Transform child in bar.transform)
            {
                // The button itself
                var le = child.GetComponent<LayoutElement>();
                if (le != null)
                {
                    le.preferredHeight = 46;
                    le.minHeight = 46;
                    le.minWidth = 54;
                    le.preferredWidth = 54;
                }
                var btnRT = child.GetComponent<RectTransform>();
                if (btnRT != null && btnRT.sizeDelta.y > 0)
                    btnRT.sizeDelta = new Vector2(btnRT.sizeDelta.x, 46);

                // Icon inside
                var iconT = child.Find("Icon");
                if (iconT == null)
                {
                    foreach (Transform inner in child)
                    {
                        var i2 = inner.Find("Icon");
                        if (i2 != null) { iconT = i2; break; }
                    }
                }
                if (iconT != null)
                {
                    var irt = iconT.GetComponent<RectTransform>();
                    if (irt != null)
                    {
                        // Square box at fixed center anchor — never lets the icon distort
                        irt.anchorMin = new Vector2(0.5f, 0.5f);
                        irt.anchorMax = new Vector2(0.5f, 0.5f);
                        irt.pivot     = new Vector2(0.5f, 0.5f);
                        irt.sizeDelta = new Vector2(20, 20);
                        irt.anchoredPosition = new Vector2(0, 4);
                    }
                    var iimg = iconT.GetComponent<UnityEngine.UI.Image>();
                    if (iimg != null) iimg.preserveAspect = true;
                }

                // Label — shrink so it fits the smaller pill
                var lblT = child.Find("Label");
                if (lblT != null)
                {
                    var tm = lblT.GetComponent<TMPro.TMP_Text>();
                    if (tm != null) tm.fontSize = 8;
                }
                touched++;
            }

            // Force layout refresh
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(barRT);

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Sparq",
                $"Resized {touched} top-nav button(s) to 56px tall, icon 22×22, repositioned under stat box.",
                "OK");
        }
    }
}
