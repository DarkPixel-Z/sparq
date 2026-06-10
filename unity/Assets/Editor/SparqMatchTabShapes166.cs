using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 166: Make all 4 SocialPanel tabs share the polished octagon shape
    /// (Tab_BottomFlush Select = active, Disable = inactive).
    /// </summary>
    public static class SparqMatchTabShapes166
    {
        private const string TAB_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/166. Match all tab shapes (octagon Select/Disable)")]
        public static void Apply()
        {
            EnsureSprite(TAB_DIR + "Tab_BottomFlush_01_Single_Select.png");
            EnsureSprite(TAB_DIR + "Tab_BottomFlush_01_Single_Disable.png");

            var selectSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_DIR + "Tab_BottomFlush_01_Single_Select.png");
            var disableSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_DIR + "Tab_BottomFlush_01_Single_Disable.png");

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var tabs = social.transform.Find("Tabs");
            if (tabs == null) return;

            // All tabs get disabled sprite by default; first gets select
            for (int i = 0; i < tabs.childCount; i++)
            {
                var tab = tabs.GetChild(i);
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = (i == 0) ? selectSprite : disableSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                }
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 22;
                    tm.fontStyle = FontStyles.Bold;
                    tm.color = (i == 0) ? DEEP_NAVY : CREAM;
                    tm.outlineWidth = (i == 0) ? 0f : 0.20f;
                    tm.outlineColor = new Color(0, 0, 0, 0.85f);
                }
            }

            // (Re)attach TabSpriteSwap with new sprites
            var swap = social.GetComponent<TabSpriteSwap>();
            if (swap == null) swap = social.gameObject.AddComponent<TabSpriteSwap>();
            var so = new SerializedObject(swap);
            so.FindProperty("normalSprite").objectReferenceValue = disableSprite;
            so.FindProperty("selectSprite").objectReferenceValue = selectSprite;
            // Reuse buttons from TabGroup
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var tgSO = new SerializedObject(tg);
                var tabsArr = tgSO.FindProperty("tabs");
                int n = tabsArr.arraySize;
                var btns = so.FindProperty("tabButtons");
                btns.arraySize = n;
                for (int i = 0; i < n; i++)
                {
                    var entry = tabsArr.GetArrayElementAtIndex(i);
                    btns.GetArrayElementAtIndex(i).objectReferenceValue =
                        entry.FindPropertyRelative("button").objectReferenceValue;
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // Make sure TabGroup color tints don't fight the sprite swap
            if (tg != null)
            {
                var tgSO = new SerializedObject(tg);
                tgSO.FindProperty("activeBg").colorValue   = Color.white;
                tgSO.FindProperty("inactiveBg").colorValue = Color.white;
                tgSO.FindProperty("activeFg").colorValue   = DEEP_NAVY;
                tgSO.FindProperty("inactiveFg").colorValue = CREAM;
                tgSO.ApplyModifiedPropertiesWithoutUndo();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ All 4 tabs now use matching octagon shape:\n• Active = bright Select sprite\n• Inactive = dim Disable sprite (same shape)\n\nText flips dark navy on active, cream on inactive.\n\nHit ▶ Play.", "OK");
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
