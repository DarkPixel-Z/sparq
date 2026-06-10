using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 135:
    ///   • Top buttons → Layer Lab GUI Pro-FantasyHero (Brown / RPG style)
    ///   • Una help icon → wrapped in fantasy circle frame
    ///   • Bottom nav text → bolder weight + thicker outline
    /// </summary>
    public static class SparqProButtons135
    {
        private const string FH_BTN  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";
        private const string FH_ICON = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/";
        private const string FH_FRAME= "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/";
        private const string UNA     = "Assets/Art/Sparq/una-mage.png";

        // Same 5 top buttons, Layer Lab fantasy icons
        private static readonly (string btn, string icon, string label)[] TOP_BTNS = new[]
        {
            ("MapBtn",   "ItemIcon_Map.png",            "MAP"),
            ("ShopBtn",  "ItemIcon_Shop.png",           "SHOP"),
            ("BagBtn",   "ItemIcon_Bag.png",            "BAG"),
            ("PetsBtn",  "ItemIcon_GemStone_Red.png",   "PETS"),
            ("WorldBtn", "ItemIcon_Gem_Diamond_Blue.png","WORLD"),
        };

        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/135. Pro fantasy buttons + Una frame + bolder nav text")]
        public static void Apply()
        {
            BuildTopButtons();
            FrameUna();
            BoldenBottomNav();

            // MarkSceneDirty errors out if called while Play mode is active
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Sparq",
                "✅ Applied:\n\n" +
                "• Top buttons → Fantasy Hero style (brown wood w/ map/shop/bag/gem icons)\n" +
                "• Una → wrapped in fantasy circle frame\n" +
                "• Bottom nav text → bolder + thicker outline\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── Top buttons (Layer Lab fantasy) ─────────────────────
        private static void BuildTopButtons()
        {
            string bgPath = FH_BTN + "Button_01_Mian_l_Bg_Brown.png";
            EnsureSprite(bgPath);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgPath);
            if (bgSprite == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Brown button bg missing.\nFalls back to whatever sprite is found.", "OK");
            }

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Remove any existing layout group (Vertical etc) — we need a Horizontal one
            var existing = bar.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (existing != null && !(existing is HorizontalLayoutGroup))
                Object.DestroyImmediate(existing);
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(4, 4, 3, 3);
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;   // respect preferredWidth so buttons stay 54px
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            // Reposition the bar so all 5 fit cleanly
            var barRT = bar.GetComponent<RectTransform>();
            if (barRT != null)
            {
                barRT.anchorMin = new Vector2(1f, 1f);
                barRT.anchorMax = new Vector2(1f, 1f);
                barRT.pivot     = new Vector2(1f, 1f);
                barRT.anchoredPosition = new Vector2(-12, -135);
                barRT.sizeDelta = new Vector2(320, 52);
            }

            foreach (var (btnName, iconFile, label) in TOP_BTNS)
            {
                string iconPath = FH_ICON + iconFile;
                EnsureSprite(iconPath);
                var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                BuildButton(bar.transform, btnName, label, bgSprite, iconSprite);
            }
        }

        private static void BuildButton(Transform parent, string btnName, string label, Sprite bg, Sprite icon)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 0;
            le.minWidth = 54;
            le.preferredWidth = 54;
            le.minHeight = 46;
            le.preferredHeight = 46;

            var img = go.GetComponent<Image>();
            if (bg != null) img.sprite = bg;
            else img.color = new Color(0.55f, 0.36f, 0.20f);
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;

            // Icon centered upper
            if (icon != null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(go.transform, false);
                var rt = iconGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 4);
                rt.sizeDelta = new Vector2(20, 20);
                var iimg = iconGO.GetComponent<Image>();
                iimg.sprite = icon;
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;
            }

            // Label bottom
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0, 8);
            lrt.sizeDelta = new Vector2(0, 14);
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 8;
            tm.enableAutoSizing = true;
            tm.fontSizeMin = 7;
            tm.fontSizeMax = 9;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.95f, 0.82f);  // cream
            tm.outlineWidth = 0.25f;
            tm.outlineColor = new Color(0.10f, 0.06f, 0.02f, 1f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tm.overflowMode = TextOverflowModes.Overflow;
        }

        // ───────────────────── Una frame ─────────────────────
        private static void FrameUna()
        {
            string framePath = FH_FRAME + "BaseFrame_Border_Circle_H106.png";
            EnsureSprite(framePath);
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
            // Sharpen Una texture: bilinear filter, uncompressed, no mipmaps, 1024 max
            var unaImp = AssetImporter.GetAtPath(UNA) as TextureImporter;
            if (unaImp != null)
            {
                bool changed = false;
                if (unaImp.textureType != TextureImporterType.Sprite)
                { unaImp.textureType = TextureImporterType.Sprite; changed = true; }
                if (!unaImp.alphaIsTransparency)
                { unaImp.alphaIsTransparency = true; changed = true; }
                if (unaImp.filterMode != FilterMode.Bilinear)
                { unaImp.filterMode = FilterMode.Bilinear; changed = true; }
                if (unaImp.textureCompression != TextureImporterCompression.Uncompressed)
                { unaImp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (unaImp.maxTextureSize < 1024)
                { unaImp.maxTextureSize = 1024; changed = true; }
                if (unaImp.mipmapEnabled)
                { unaImp.mipmapEnabled = false; changed = true; }
                if (changed && !Application.isPlaying) unaImp.SaveAndReimport();
            }
            else { EnsureSprite(UNA); }
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(UNA);

            var canvas = GameObject.Find("UI Canvas");
            if (canvas == null)
            {
                var c = Object.FindAnyObjectByType<Canvas>();
                if (c != null) canvas = c.gameObject;
            }
            if (canvas == null) return;

            var old = GameObject.Find("HelpIcon");
            if (old != null) Object.DestroyImmediate(old);

            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);

            var hrt = help.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot     = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(10f, 90f);
            hrt.sizeDelta = new Vector2(110, 110);

            var himg = help.GetComponent<Image>();
            if (frameSprite != null) himg.sprite = frameSprite;
            himg.preserveAspect = true;
            himg.color = Color.white;

            // Una sprite inside the frame
            if (unaSprite != null)
            {
                var una = new GameObject("Una", typeof(RectTransform), typeof(Image));
                una.transform.SetParent(help.transform, false);
                var urt = una.GetComponent<RectTransform>();
                urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                urt.offsetMin = new Vector2(5, 5); urt.offsetMax = new Vector2(-5, -5);
                var uimg = una.GetComponent<Image>();
                uimg.sprite = unaSprite;
                uimg.preserveAspect = true;
                uimg.raycastTarget = false;
            }

            // Small "?" badge top-right
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(help.transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f); brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot     = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(0f, 0f);
            brt.sizeDelta = new Vector2(22, 22);
            badge.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f);

            var q = new GameObject("Q", typeof(RectTransform));
            q.transform.SetParent(badge.transform, false);
            var qrt = q.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var qtm = q.AddComponent<TextMeshProUGUI>();
            qtm.text = "?";
            qtm.fontSize = 14;
            qtm.fontStyle = FontStyles.Bold;
            qtm.color = DEEP_NAVY;
            qtm.alignment = TextAlignmentOptions.Center;
            qtm.raycastTarget = false;

            help.transform.SetAsLastSibling();
        }

        // ───────────────────── Bottom nav: bolder text ─────────────────────
        private static void BoldenBottomNav()
        {
            var bar = GameObject.Find("BottomNav");
            if (bar == null) return;

            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var tab = bar.transform.GetChild(i);
                var le = tab.GetComponent<LayoutElement>();
                if (le != null && le.ignoreLayout) continue;

                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                    tm.color = DEEP_NAVY;
                    tm.outlineWidth = 0.30f;                          // thicker outline
                    tm.outlineColor = new Color(1f, 0.95f, 0.75f, 1f); // cream halo
                    tm.fontSizeMin = 9;
                    tm.fontSizeMax = 13;
                }
            }
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
