using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 169: Make all side tabs full-bright (no dim on inactive).
    /// Active state shown via slight scale-up + gold text glow instead of dimming.
    /// Also lay out tabs and chat content cleanly.
    /// </summary>
    public static class SparqBrightTabs169
    {
        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color GOLD      = new Color(1f, 0.82f, 0.32f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/169. Brighter side tabs + cleaner layout")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                // Tighten + recenter the column
                var rt = tabs.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-20, 0);
                rt.sizeDelta = new Vector2(220, -200);

                var vlg = tabs.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.spacing = 18;
                    vlg.padding = new RectOffset(0, 0, 30, 30);
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;
                    vlg.childControlHeight = false;
                    vlg.childAlignment = TextAnchor.MiddleCenter;
                }

                // Each tab: full-bright sprite + bigger size
                for (int i = 0; i < tabs.childCount; i++)
                {
                    var tab = tabs.GetChild(i);
                    var le = tab.GetComponent<LayoutElement>();
                    if (le == null) le = tab.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 100;
                    le.flexibleHeight = 0;
                    le.preferredWidth = 0;
                    le.flexibleWidth = 1;

                    var img = tab.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = Color.white; // full bright always
                        img.preserveAspect = false;
                    }

                    foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 26;
                        tm.fontStyle = FontStyles.Bold;
                        tm.color = CREAM;
                        tm.outlineWidth = 0.32f;
                        tm.outlineColor = new Color(0, 0, 0, 0.95f);
                    }
                }
            }

            // TabGroup: keep all tabs full bright — active shown via TEXT color (gold glow)
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                so.FindProperty("activeBg").colorValue   = Color.white;
                so.FindProperty("inactiveBg").colorValue = Color.white;
                so.FindProperty("activeFg").colorValue   = GOLD;   // active = gold text
                so.FindProperty("inactiveFg").colorValue = CREAM;  // inactive = cream
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Add a subtle scale tween via component for the active tab (idempotent)
            var scaler = social.GetComponent<TabScaleHighlight>();
            if (scaler == null) scaler = social.gameObject.AddComponent<TabScaleHighlight>();
            var scaleSO = new SerializedObject(scaler);
            // Pull buttons from TabGroup
            if (tg != null)
            {
                var tgSO = new SerializedObject(tg);
                var tabsArr = tgSO.FindProperty("tabs");
                int n = tabsArr.arraySize;
                var btns = scaleSO.FindProperty("tabButtons");
                btns.arraySize = n;
                for (int i = 0; i < n; i++)
                {
                    var entry = tabsArr.GetArrayElementAtIndex(i);
                    btns.GetArrayElementAtIndex(i).objectReferenceValue =
                        entry.FindPropertyRelative("button").objectReferenceValue;
                }
            }
            scaleSO.ApplyModifiedPropertiesWithoutUndo();

            // Content: more breathing room — scale prefab to 0.78
            var content = social.transform.Find("Content");
            if (content != null)
            {
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(20, 20);
                rt.offsetMax = new Vector2(-260, -100); // leaves room for tabs + close
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var crt = c.GetComponent<RectTransform>();
                    if (crt == null) continue;
                    crt.anchorMin = new Vector2(0.5f, 0.5f);
                    crt.anchorMax = new Vector2(0.5f, 0.5f);
                    crt.pivot     = new Vector2(0.5f, 0.5f);
                    crt.anchoredPosition = Vector2.zero;
                    crt.localScale = new Vector3(0.78f, 0.78f, 1f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Layout cleaned + tabs full-bright:\n" +
                "• All tabs always bright (no dim)\n" +
                "• Active tab: gold text + 1.08× scale via TabScaleHighlight\n" +
                "• Inactive: cream text\n" +
                "• Content area inset cleanly\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
