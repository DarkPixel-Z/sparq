using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 167: Move SocialPanel tabs to the RIGHT side as a vertical column,
    /// and grow the content area / prefab so the chat is much bigger.
    /// </summary>
    public static class SparqVerticalTabs167
    {
        [MenuItem("Sparq/167. Tabs → right side, chat bigger")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Reposition Tabs as vertical column on the right
            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                var rt = tabs.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-16, 0);
                rt.sizeDelta = new Vector2(220, -160); // full height minus a bit for X close

                // Remove HorizontalLayoutGroup, add VerticalLayoutGroup
                var hlg = tabs.GetComponent<HorizontalLayoutGroup>();
                if (hlg != null) Object.DestroyImmediate(hlg);
                var vlg = tabs.GetComponent<VerticalLayoutGroup>();
                if (vlg == null) vlg = tabs.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(0, 0, 0, 0);
                vlg.spacing = 14;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;

                // Each tab: fixed height
                for (int i = 0; i < tabs.childCount; i++)
                {
                    var tab = tabs.GetChild(i);
                    var le = tab.GetComponent<LayoutElement>();
                    if (le == null) le = tab.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 90;
                    le.flexibleHeight = 0;
                    le.preferredWidth = 0;
                    le.flexibleWidth = 1;
                }
            }

            // 2. Resize Content area to fill the left side (everything except tabs column)
            var content = social.transform.Find("Content");
            if (content != null)
            {
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(0, 0);
                rt.offsetMax = new Vector2(-260, 0); // leaves 260px on right for tab column + padding
            }

            // 3. Scale up the prefab tabs to use the new bigger space
            if (content != null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var rt = c.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot     = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                    rt.localScale = new Vector3(0.85f, 0.85f, 1f); // bigger than 0.55 since we have more space
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Tabs moved to right column, chat bigger:\n" +
                "• Tabs: vertical stack on right, each 90 tall\n" +
                "• Content area takes left ~80% of screen\n" +
                "• Prefab tabs scaled to 0.85× (was 0.55×)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
