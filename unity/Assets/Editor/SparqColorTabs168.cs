using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 168: Each side tab gets a different colored Button_01 sprite.
    /// Active tab is full-bright; inactive tabs dim. Main chat scaled slightly down.
    /// </summary>
    public static class SparqColorTabs168
    {
        private const string BTN_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";

        // Color per tab — Chat / Clan / Ranking / Profile
        private static readonly string[] TAB_SPRITES = new[]
        {
            "Button_01_Mian_l_Bg_Blue.png",   // Chat
            "Button_01_Mian_l_Bg_Red.png",    // Clan
            "Button_01_Mian_l_Bg_Green.png",  // Ranking
            "Button_01_Mian_l_Bg_Purple.png", // Profile
        };

        private static readonly Color CREAM     = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/168. Color side tabs + slightly smaller chat")]
        public static void Apply()
        {
            foreach (var p in TAB_SPRITES) EnsureSprite(BTN_DIR + p);

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // Remove TabSpriteSwap — each tab keeps its own color now
            foreach (var s in social.GetComponents<TabSpriteSwap>())
                Object.DestroyImmediate(s);

            var tabs = social.transform.Find("Tabs");
            if (tabs == null) return;

            for (int i = 0; i < tabs.childCount && i < TAB_SPRITES.Length; i++)
            {
                var tab = tabs.GetChild(i);
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_DIR + TAB_SPRITES[i]);
                    if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
                    img.color = (i == 0) ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.85f);
                }
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 22;
                    tm.fontStyle = FontStyles.Bold;
                    tm.color = CREAM;
                    tm.outlineWidth = 0.30f;
                    tm.outlineColor = new Color(0, 0, 0, 0.9f);
                }
            }

            // TabGroup: instead of swapping bg color, just dim/brighten via Image.color
            // Set activeBg=white (full bright), inactiveBg=gray (dimmed) — keeps sprite colors
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                so.FindProperty("activeBg").colorValue   = Color.white;
                so.FindProperty("inactiveBg").colorValue = new Color(0.65f, 0.65f, 0.65f, 0.85f);
                so.FindProperty("activeFg").colorValue   = CREAM; // stays cream — text color same
                so.FindProperty("inactiveFg").colorValue = new Color(1f, 0.95f, 0.82f, 0.7f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Slightly smaller chat box
            var content = social.transform.Find("Content");
            if (content != null)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var rt = c.GetComponent<RectTransform>();
                    if (rt == null) continue;
                    rt.localScale = new Vector3(0.78f, 0.78f, 1f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Tabs colored:\n• Chat = Blue\n• Clan = Red\n• Ranking = Green\n• Profile = Purple\n\nActive = full bright, inactive = dimmed.\n\nChat box scaled to 0.78×.\n\nHit ▶ Play.", "OK");
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
