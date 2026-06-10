using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 176: Professional layout pass.
    ///   • Send button → real click handler (clears input, fires toast)
    ///   • Input bar → bordered fantasy frame (BaseFrame_Border_Rectangle_H60)
    ///   • Send button → polished Button_01 yellow sprite
    ///   • Tab buttons → border halo + rounded corners feel
    /// </summary>
    public static class SparqProSocial176
    {
        private const string FRAME_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/";
        private const string BTN_DIR   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";

        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/176. Pro polish — Send works + bordered input + nicer button")]
        public static void Apply()
        {
            EnsureSprite(FRAME_DIR + "BaseFrame_Border_Rectangle_H60_Bg.png");
            EnsureSprite(FRAME_DIR + "BaseFrame_Border_Rectangle_H60_Border.png");
            EnsureSprite(BTN_DIR   + "Button_01_Mian_l_Bg_Yellow.png");

            var bgSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(FRAME_DIR + "BaseFrame_Border_Rectangle_H60_Bg.png");
            var borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FRAME_DIR + "BaseFrame_Border_Rectangle_H60_Border.png");
            var sendSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_DIR   + "Button_01_Mian_l_Bg_Yellow.png");

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var bar = social.transform.Find("ChatInputBar");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "ChatInputBar not found. Run #175 first.", "OK"); return; }

            // ───── Input field bordered + bg sprite ─────
            var fieldT = bar.Find("Input");
            if (fieldT != null)
            {
                var img = fieldT.GetComponent<Image>();
                if (img != null && bgSprite != null)
                {
                    img.sprite = bgSprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(1f, 1f, 1f, 0.85f);
                }

                // Add border overlay (sliced, no raycast)
                var existingBorder = fieldT.Find("Border");
                if (existingBorder == null && borderSprite != null)
                {
                    var bdr = new GameObject("Border", typeof(RectTransform), typeof(Image));
                    bdr.transform.SetParent(fieldT, false);
                    var brt = bdr.GetComponent<RectTransform>();
                    brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                    brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                    var bImg = bdr.GetComponent<Image>();
                    bImg.sprite = borderSprite;
                    bImg.type = Image.Type.Sliced;
                    bImg.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f);
                    bImg.raycastTarget = false;
                }
            }

            // ───── Send button polished sprite + click handler ─────
            var sendT = bar.Find("Send");
            if (sendT != null)
            {
                var sImg = sendT.GetComponent<Image>();
                if (sImg != null && sendSprite != null)
                {
                    sImg.sprite = sendSprite;
                    sImg.type = Image.Type.Sliced;
                    sImg.color = Color.white;
                }

                // Wire ChatSender component (real click action)
                var inputField = fieldT?.GetComponent<TMP_InputField>();
                var sendBtn = sendT.GetComponent<Button>();
                if (inputField != null && sendBtn != null)
                {
                    var sender = bar.GetComponent<ChatSender>();
                    if (sender == null) sender = bar.gameObject.AddComponent<ChatSender>();
                    var so = new SerializedObject(sender);
                    so.FindProperty("input").objectReferenceValue = inputField;
                    so.FindProperty("sendButton").objectReferenceValue = sendBtn;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // ───── Tab halo borders for premium feel ─────
            var tabs = social.transform.Find("Tabs");
            if (tabs != null && borderSprite != null)
            {
                for (int i = 0; i < tabs.childCount; i++)
                {
                    var tab = tabs.GetChild(i);
                    var existing = tab.Find("Halo");
                    if (existing != null) continue;
                    var halo = new GameObject("Halo", typeof(RectTransform), typeof(Image));
                    halo.transform.SetParent(tab, false);
                    var hrt = halo.GetComponent<RectTransform>();
                    hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
                    hrt.offsetMin = new Vector2(-2, -2); hrt.offsetMax = new Vector2(2, 2);
                    var hImg = halo.GetComponent<Image>();
                    hImg.sprite = borderSprite;
                    hImg.type = Image.Type.Sliced;
                    hImg.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.55f);
                    hImg.raycastTarget = false;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Pro polish applied:\n" +
                "• Send button now fires (clears input, shows toast)\n" +
                "• Input bar wrapped in bordered fantasy frame w/ gold halo\n" +
                "• Send button → polished yellow Button_01 sprite\n" +
                "• Each tab gets a gold halo border\n\n" +
                "Hit ▶ Play, type something, tap Send.", "OK");
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
