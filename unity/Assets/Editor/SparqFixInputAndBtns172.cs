using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 172: Two fixes —
    ///   1. Add real TMP_InputField on top of any "Enter Text..." / "Text" placeholder
    ///      so the chat input actually accepts typing.
    ///   2. Try a different tab button style (Tab_BottomFlush_02 banner with deco line).
    /// </summary>
    public static class SparqFixInputAndBtns172
    {
        private const string BTN_DIR  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";
        private static readonly Color CREAM = new Color(1f, 0.95f, 0.82f);
        private static readonly Color GOLD  = new Color(1f, 0.82f, 0.32f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        // Keep the colored Title_Flag look but try the V2 sprites that have a deco line
        private static readonly string[] TAB_SPRITES = new[]
        {
            "Button_01_Mian_l_Bg_Sky.png",
            "Button_01_Mian_l_Bg_Pink.png",
            "Button_01_Mian_l_Bg_Mint.png",
            "Button_01_Mian_l_Bg_Plum.png",
        };

        [MenuItem("Sparq/172. Better tabs (Sky/Pink/Mint/Plum) + working chat input")]
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

            // 1. Apply new tab sprites
            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                for (int i = 0; i < tabs.childCount && i < TAB_SPRITES.Length; i++)
                {
                    var tab = tabs.GetChild(i);
                    var img = tab.GetComponent<Image>();
                    if (img != null)
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(BTN_DIR + TAB_SPRITES[i]);
                        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
                        img.color = Color.white;
                    }
                    foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.color = DEEP_NAVY;
                        tm.outlineWidth = 0.20f;
                        tm.outlineColor = new Color(1f, 1f, 1f, 0.85f);
                        tm.fontSize = 24;
                        tm.fontStyle = FontStyles.Bold;
                    }
                }
            }

            // 2. Find chat input placeholders and add real TMP_InputField on top
            int inputsAdded = 0;
            var content = social.transform.Find("Content");
            if (content != null)
            {
                foreach (var tmp in content.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    string txt = tmp.text == null ? "" : tmp.text.ToLower();
                    bool isPlaceholder =
                        txt.Contains("enter text") ||
                        txt == "text" ||
                        txt.Contains("type a message") ||
                        txt.Contains("...");
                    if (!isPlaceholder) continue;

                    // The TMP_Text is the placeholder container. Promote it to a TMP_InputField
                    // by adding an InputField component on its parent rect and wiring text/placeholder.
                    var parentRT = tmp.transform.parent as RectTransform;
                    if (parentRT == null) continue;
                    if (parentRT.GetComponent<TMP_InputField>() != null) continue; // already an input

                    // Create placeholder + text children inside the parent
                    var inputGO = parentRT.gameObject;
                    var input = inputGO.GetComponent<TMP_InputField>();
                    if (input == null) input = inputGO.AddComponent<TMP_InputField>();

                    // Make the parent has Image to receive raycast
                    var parImg = inputGO.GetComponent<Image>();
                    if (parImg == null)
                    {
                        parImg = inputGO.AddComponent<Image>();
                        parImg.color = new Color(1f, 1f, 1f, 0.05f);
                    }

                    // Text component (visible typed text)
                    var textGO = new GameObject("InputText", typeof(RectTransform));
                    textGO.transform.SetParent(parentRT, false);
                    var trt = textGO.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                    trt.offsetMin = new Vector2(12, 4); trt.offsetMax = new Vector2(-12, -4);
                    var ttm = textGO.AddComponent<TextMeshProUGUI>();
                    ttm.text = "";
                    ttm.fontSize = tmp.fontSize;
                    ttm.color = DEEP_NAVY;
                    ttm.alignment = TextAlignmentOptions.MidlineLeft;
                    ttm.font = tmp.font;
                    ttm.fontSharedMaterial = tmp.fontSharedMaterial;

                    // Make the original TMP_Text the placeholder
                    tmp.color = new Color(0.5f, 0.5f, 0.55f);

                    // Wire fields on the InputField
                    input.textViewport = parentRT;
                    input.textComponent = ttm;
                    input.placeholder = tmp;
                    input.lineType = TMP_InputField.LineType.SingleLine;
                    input.contentType = TMP_InputField.ContentType.Standard;
                    input.text = "";

                    inputsAdded++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Done:\n• Tabs → Sky/Pink/Mint/Plum colored buttons w/ dark text\n• {inputsAdded} chat input field(s) made typable (real TMP_InputField)\n\nHit ▶ Play, click the input box, type something.", "OK");
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
