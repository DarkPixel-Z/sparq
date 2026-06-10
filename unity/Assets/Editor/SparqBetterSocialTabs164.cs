using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 164: Replace SocialPanel tab buttons with the polished Button_01
    /// fantasy buttons (Brown for inactive, Yellow for active) — same style as
    /// the home top buttons.
    /// </summary>
    public static class SparqBetterSocialTabs164
    {
        private const string BTN_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/164. Better SocialPanel tab buttons (Brown/Yellow)")]
        public static void Apply()
        {
            EnsureSprite(BTN_DIR + "Button_01_Mian_l_Bg_Brown.png");
            EnsureSprite(BTN_DIR + "Button_01_Mian_l_Bg_Yellow.png");

            var normalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_DIR + "Button_01_Mian_l_Bg_Brown.png");
            var selectSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_DIR + "Button_01_Mian_l_Bg_Yellow.png");

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var tabs = social.transform.Find("Tabs");
            if (tabs == null) { EditorUtility.DisplayDialog("Sparq", "Tabs row not found.", "OK"); return; }

            // Make the tab row a bit taller so the chunky buttons look right
            var trt = tabs.GetComponent<RectTransform>();
            if (trt != null) trt.sizeDelta = new Vector2(trt.sizeDelta.x, 96);
            var hlg = tabs.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) { hlg.spacing = 12; hlg.padding = new RectOffset(8, 8, 4, 4); }

            // Apply normal sprite to all
            for (int i = 0; i < tabs.childCount; i++)
            {
                var tab = tabs.GetChild(i);
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = normalSprite;
                    img.type = Image.Type.Sliced;
                    img.color = Color.white;
                    img.preserveAspect = false;
                }
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = CREAM;
                    tm.fontStyle = FontStyles.Bold;
                    tm.fontSize = 26;
                    tm.outlineWidth = 0.30f;
                    tm.outlineColor = new Color(0.10f, 0.06f, 0.02f, 0.95f);
                }
            }

            // First tab gets select sprite to start (default index 0)
            if (tabs.childCount > 0 && selectSprite != null)
            {
                var firstImg = tabs.GetChild(0).GetComponent<Image>();
                if (firstImg != null) firstImg.sprite = selectSprite;
                // Active tab gets dark text on bright bg
                foreach (var tm in tabs.GetChild(0).GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = DEEP_NAVY;
                    tm.outlineColor = new Color(1f, 0.95f, 0.75f, 0.9f);
                }
            }

            // Update / re-add TabSpriteSwap with new sprites
            var swap = social.GetComponent<TabSpriteSwap>();
            if (swap != null)
            {
                var so = new SerializedObject(swap);
                so.FindProperty("normalSprite").objectReferenceValue = normalSprite;
                so.FindProperty("selectSprite").objectReferenceValue = selectSprite;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Update TabGroup colors so text flips properly on active
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                so.FindProperty("activeBg").colorValue   = Color.white;
                so.FindProperty("inactiveBg").colorValue = Color.white;
                so.FindProperty("activeFg").colorValue   = DEEP_NAVY;
                so.FindProperty("inactiveFg").colorValue = CREAM;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Tab buttons restyled to match home top buttons:\n• Brown = inactive\n• Yellow = active\n• 26pt bold cream text → flips to dark navy when active\n\nHit ▶ Play.", "OK");
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
