using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 162: Style the SocialPanel tab buttons with the proper Layer Lab
    /// Tab_BottomFlush sprites (normal + select states).
    /// </summary>
    public static class SparqStyleSocialTabs162
    {
        private const string TAB_NORMAL = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_01_Single_Nomal.png";
        private const string TAB_SELECT = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_01_Single_Select.png";

        private static readonly Color GOLD     = new Color(1f, 0.82f, 0.32f);
        private static readonly Color CREAM    = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/162. Style SocialPanel tabs (proper Tab sprites)")]
        public static void Apply()
        {
            EnsureSprite(TAB_NORMAL);
            EnsureSprite(TAB_SELECT);

            var normalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_NORMAL);
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_SELECT);

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var tabs = social.transform.Find("Tabs");
            if (tabs == null) { EditorUtility.DisplayDialog("Sparq", "Tabs row not found.", "OK"); return; }

            // Apply normal sprite to all tab buttons + style text
            for (int i = 0; i < tabs.childCount; i++)
            {
                var tab = tabs.GetChild(i);
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = normalSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = CREAM;
                    tm.fontStyle = FontStyles.Bold;
                    tm.fontSize = 24;
                    tm.outlineWidth = 0.20f;
                    tm.outlineColor = new Color(0, 0, 0, 0.8f);
                }
            }

            // Update TabGroup component colors so swap-on-select uses sprites instead of color tints
            var tabGroup = social.GetComponent<TabGroup>();
            if (tabGroup != null)
            {
                var so = new SerializedObject(tabGroup);
                // The TabGroup applies colors via Image.color — change those colors to white tints
                // so the sprites' own colors show
                so.FindProperty("activeBg").colorValue   = Color.white;
                so.FindProperty("inactiveBg").colorValue = new Color(1, 1, 1, 0.55f);
                so.FindProperty("activeFg").colorValue   = GOLD;
                so.FindProperty("inactiveFg").colorValue = CREAM;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Manually swap each button's sprite based on which is the default tab (idx 0)
            // Active gets selectSprite, others get normalSprite. TabGroup will reapply on click,
            // but it only changes Image.color — it won't swap sprites. So write a tiny helper
            // component that swaps sprites on the active tab.
            // Simpler: just set first tab to select sprite for visual at start.
            if (tabs.childCount > 0)
            {
                var first = tabs.GetChild(0);
                var firstImg = first.GetComponent<Image>();
                if (firstImg != null && selectSprite != null)
                    firstImg.sprite = selectSprite;
            }

            // Add TabSpriteSwap component so sprite changes when tab is clicked
            var swap = social.GetComponent<TabSpriteSwap>();
            if (swap == null) swap = social.gameObject.AddComponent<TabSpriteSwap>();
            var swapSO = new SerializedObject(swap);
            swapSO.FindProperty("normalSprite").objectReferenceValue = normalSprite;
            swapSO.FindProperty("selectSprite").objectReferenceValue = selectSprite;
            // Pull buttons from TabGroup
            var tabsArr = tabGroup != null ? new SerializedObject(tabGroup).FindProperty("tabs") : null;
            if (tabsArr != null)
            {
                int n = tabsArr.arraySize;
                var btnsProp = swapSO.FindProperty("tabButtons");
                btnsProp.arraySize = n;
                for (int i = 0; i < n; i++)
                {
                    var entry = tabsArr.GetArrayElementAtIndex(i);
                    btnsProp.GetArrayElementAtIndex(i).objectReferenceValue =
                        entry.FindPropertyRelative("button").objectReferenceValue;
                }
            }
            swapSO.ApplyModifiedPropertiesWithoutUndo();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ SocialPanel tabs restyled:\n• Normal + Select sprites from Layer Lab\n• Active tab swaps to Select sprite on click\n• Text 24pt bold cream w/ outline\n\nHit ▶ Play.", "OK");
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }
}
