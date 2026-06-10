using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 170: Move side tabs further left, and reset prefab scale to 1× so
    /// the ScrollRect inside the chat prefab works correctly.
    /// </summary>
    public static class SparqFixScrollAndTabs170
    {
        [MenuItem("Sparq/170. Fix scroll + nudge tabs left")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Move tab column further left (away from right edge)
            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                var rt = tabs.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-60, 0); // was -20
                rt.sizeDelta = new Vector2(220, -200);
            }

            // 2. Content area: leave more right room for the shifted tabs
            var content = social.transform.Find("Content");
            if (content != null)
            {
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(20, 20);
                rt.offsetMax = new Vector2(-300, -100);

                // Reset prefab tab scale to 1.0 — scaled UI breaks ScrollRect math
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var crt = c.GetComponent<RectTransform>();
                    if (crt == null) continue;
                    crt.localScale = Vector3.one;     // native scale → scroll works
                    crt.anchorMin = new Vector2(0.5f, 0.5f);
                    crt.anchorMax = new Vector2(0.5f, 0.5f);
                    crt.pivot     = new Vector2(0.5f, 0.5f);
                    crt.anchoredPosition = Vector2.zero;
                }
            }

            // 3. Find any ScrollRects inside the prefabs and force-refresh them
            int scrollFixed = 0;
            if (content != null)
            {
                foreach (var sr in content.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (sr == null) continue;
                    sr.movementType = ScrollRect.MovementType.Elastic;
                    sr.inertia = true;
                    sr.scrollSensitivity = Mathf.Max(sr.scrollSensitivity, 16f);
                    if (sr.viewport != null) sr.viewport.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    Canvas.ForceUpdateCanvases();
                    sr.verticalNormalizedPosition = 1f;
                    scrollFixed++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Layout fixed:\n" +
                "• Tabs nudged 40px left (now -60 from right edge)\n" +
                "• Prefab scale reset to 1× (ScrollRect math depends on this)\n" +
                "• Content area: more right padding for shifted tabs\n" +
                $"• {scrollFixed} ScrollRect(s) refreshed\n\n" +
                "Hit ▶ Play and try scrolling chat.", "OK");
        }
    }
}
