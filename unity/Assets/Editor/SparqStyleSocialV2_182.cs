using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 182: Apply Layer Lab fantasy sprite art to the SocialPanel V2.
    /// Tabs → colored Button_01 sprites with Border halo.
    /// Chat bubbles → Slider_Border_Rectangle frames (rounded + bordered).
    /// Input → bordered fantasy frame.
    /// Rows → bordered cards.
    /// </summary>
    public static class SparqStyleSocialV2_182
    {
        private const string SP = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/";

        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        // Tab color per index
        private static readonly string[] TAB_BGS = new[]
        {
            "Button/Button_01_Mian_l_Bg_Sky.png",   // Chat
            "Button/Button_01_Mian_l_Bg_Red.png",   // Clan
            "Button/Button_01_Mian_l_Bg_Yellow.png",// Ranking
            "Button/Button_01_Mian_l_Bg_Purple.png",// Profile
        };

        [MenuItem("Sparq/182. Apply fantasy sprite art to SocialPanel V2")]
        public static void Apply()
        {
            foreach (var p in TAB_BGS) EnsureSprite(SP + p);
            EnsureSprite(SP + "Slider/Slider_Border_Rectangle_01_Bg.png");
            EnsureSprite(SP + "Slider/Slider_Border_Rectangle_01_Border.png");
            EnsureSprite(SP + "Slider/Slider_Border_Rectangle_01_Fill_Yellow.png");
            EnsureSprite(SP + "Frame/BaseFrame_Border_Rectangle_H50_Bg.png");
            EnsureSprite(SP + "Frame/BaseFrame_Border_Rectangle_H50_Border.png");
            EnsureSprite(SP + "Frame/BaseFrame_Border_Rectangle_H80_Bg.png");

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel V2 missing. Run #180 first.", "OK"); return; }

            var card = social.transform.Find("Card");
            if (card == null) return;

            // ───── Tabs ─────
            var tabs = card.Find("Tabs");
            if (tabs != null)
            {
                for (int i = 0; i < tabs.childCount && i < TAB_BGS.Length; i++)
                {
                    var tab = tabs.GetChild(i);
                    var img = tab.GetComponent<Image>();
                    if (img != null)
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(SP + TAB_BGS[i]);
                        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
                        img.color = Color.white;
                    }
                    foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.color = DEEP_NAVY;
                        tm.fontSize = 24;
                        tm.fontStyle = FontStyles.Bold;
                        // Skip outline (crashes if fontMaterial isn't initialized yet)
                    }
                }

                // Update TabGroup so all tabs stay full-bright (no dim)
                var tg = social.GetComponent<Sparq.UI.TabGroup>();
                if (tg != null)
                {
                    var so = new SerializedObject(tg);
                    so.FindProperty("activeBg").colorValue   = Color.white;
                    so.FindProperty("inactiveBg").colorValue = new Color(0.6f, 0.6f, 0.6f, 0.85f);
                    so.FindProperty("activeFg").colorValue   = DEEP_NAVY;
                    so.FindProperty("inactiveFg").colorValue = DEEP_NAVY;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // ───── Chat tab: bubbles + input ─────
            var chatTab = card.Find("Content/Chat_Tab");
            if (chatTab != null)
            {
                var bubbleBg = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Slider/Slider_Border_Rectangle_01_Bg.png");
                var bubbleFill = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Slider/Slider_Border_Rectangle_01_Fill_Yellow.png");

                // Style each existing bubble
                var msgList = chatTab.Find("Scroll/Viewport/List");
                if (msgList != null)
                {
                    for (int i = 0; i < msgList.childCount; i++)
                    {
                        var row = msgList.GetChild(i);
                        var bubble = row.Find("Bubble");
                        if (bubble == null) continue;
                        var img = bubble.GetComponent<Image>();
                        if (img == null) continue;
                        bool fromMe = (img.color.r > 0.8f && img.color.g > 0.6f); // gold = me
                        var sp = fromMe ? bubbleFill : bubbleBg;
                        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
                        img.color = Color.white;

                        // Force bubble proper size if it collapsed
                        var brt = bubble.GetComponent<RectTransform>();
                        if (brt != null && brt.sizeDelta.x < 80) brt.sizeDelta = new Vector2(380, brt.sizeDelta.y);

                        // Fix any empty body text
                        var body = bubble.Find("Body");
                        if (body != null)
                        {
                            var bodyTM = body.GetComponent<TMP_Text>();
                            if (bodyTM != null && bodyTM.font == null)
                                bodyTM.font = TMP_Settings.defaultFontAsset;
                        }
                    }
                }

                // Input bar: bordered frame
                var input = chatTab.Find("InputBar/Input");
                if (input != null)
                {
                    var img = input.GetComponent<Image>();
                    var bg = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Frame/BaseFrame_Border_Rectangle_H50_Bg.png");
                    if (img != null && bg != null) { img.sprite = bg; img.type = Image.Type.Sliced; img.color = Color.white; }

                    // Add border overlay
                    var existingBorder = input.Find("Border");
                    if (existingBorder == null)
                    {
                        var bSp = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Frame/BaseFrame_Border_Rectangle_H50_Border.png");
                        if (bSp != null)
                        {
                            var bdr = new GameObject("Border", typeof(RectTransform), typeof(Image));
                            bdr.transform.SetParent(input, false);
                            var brt = bdr.GetComponent<RectTransform>();
                            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                            var bImg = bdr.GetComponent<Image>();
                            bImg.sprite = bSp;
                            bImg.type = Image.Type.Sliced;
                            bImg.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.95f);
                            bImg.raycastTarget = false;
                        }
                    }

                    // Make typed text dark navy on the cream bg
                    var inputComp = input.GetComponent<TMP_InputField>();
                    if (inputComp != null && inputComp.textComponent != null)
                        inputComp.textComponent.color = DEEP_NAVY;
                    var ph = input.Find("Placeholder");
                    if (ph != null)
                    {
                        var phTM = ph.GetComponent<TMP_Text>();
                        if (phTM != null) phTM.color = new Color(0.45f, 0.40f, 0.50f);
                    }
                }

                // Send button: yellow polished sprite
                var send = chatTab.Find("InputBar/Send");
                if (send != null)
                {
                    var img = send.GetComponent<Image>();
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Button/Button_01_Mian_l_Bg_Yellow.png");
                    if (img != null && sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white; }
                }

                // Header strip: bordered top
                var hdr = chatTab.Find("Hdr");
                if (hdr != null)
                {
                    var img = hdr.GetComponent<Image>();
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Frame/BaseFrame_Border_Rectangle_H50_Bg.png");
                    if (img != null && sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.95f); }
                }
            }

            // ───── Clan / Ranking / Profile rows: card-style sprites ─────
            var content = card.Find("Content");
            if (content != null)
            {
                var rowSp = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Frame/BaseFrame_Border_Rectangle_H50_Bg.png");
                foreach (var img in content.GetComponentsInChildren<Image>(true))
                {
                    if (img == null || img.gameObject == null) continue;
                    string n = img.gameObject.name;
                    bool isRow = n.StartsWith("Member_") || n.StartsWith("Rank_") || n.StartsWith("Stat_");
                    if (!isRow) continue;
                    if (rowSp != null)
                    {
                        img.sprite = rowSp;
                        img.type = Image.Type.Sliced;
                        // Tint slightly to keep highlight rows golden
                        if (img.color.r > 0.8f && img.color.g > 0.6f && img.color.a < 0.5f)
                        {
                            img.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.55f);
                        }
                        else img.color = new Color(1f, 1f, 1f, 0.85f);
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Fantasy art applied:\n" +
                "• Tabs: colored Button_01 sprites (Sky/Red/Yellow/Purple)\n" +
                "• Chat bubbles: Slider_Border_Rectangle (rounded + bordered)\n" +
                "• Input bar: bordered fantasy frame w/ gold halo\n" +
                "• Send button: polished yellow Button sprite\n" +
                "• Clan/Ranking/Profile rows: bordered card sprites\n\n" +
                "Hit ▶ Play.", "OK");
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
