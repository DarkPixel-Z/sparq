using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqHyperCasualButtons
    {
        private const string ATLAS_HC  = "Assets/Belevich/Hyper casual mobile GUI/Sprites/buttons.png";
        private const string ATLAS_CUI = "Assets/2D Casual UI/Sprite/GUI.png";
        private const string ICON_DIR  = "Assets/FantasyIconPack/256/";

        // Pick 5 different sprites from each atlas (we'll guess — different colored buttons)
        private static readonly string[] HC_BUTTON_NAMES = { "buttons_3", "buttons_4", "buttons_5", "buttons_6", "buttons_7" };
        private static readonly string[] CUI_BUTTON_NAMES = { "GUI_0", "GUI_1", "GUI_2", "GUI_3", "GUI_4" };

        private static readonly (string btn, string icon, string label)[] BTNS = new[]
        {
            ("MapBtn",   "Map.png",      "MAP"),
            ("ShopBtn",  "Coin.png",     "SHOP"),
            ("BagBtn",   "Backpack.png", "BAG"),
            ("PetsBtn",  "GemRed.png",   "PETS"),
            ("WorldBtn", "GemBlue.png",  "WORLD"),
        };

        [MenuItem("Sparq/128. Hyper Casual buttons (Belevich pack)")]
        public static void HyperCasual() => Apply(ATLAS_HC, HC_BUTTON_NAMES, "Hyper Casual");

        [MenuItem("Sparq/128a. 2D Casual UI HD buttons")]
        public static void CasualUIHD() => Apply(ATLAS_CUI, CUI_BUTTON_NAMES, "Casual UI HD");

        [MenuItem("Sparq/128b. Hyper Casual buttons (variant 2 - sprites 8-12)")]
        public static void HyperCasualV2() => Apply(ATLAS_HC,
            new[]{ "buttons_8", "buttons_9", "buttons_10", "buttons_11", "buttons_12" }, "Hyper Casual v2");

        [MenuItem("Sparq/128c. Hyper Casual buttons (variant 3 - sprites 13-17)")]
        public static void HyperCasualV3() => Apply(ATLAS_HC,
            new[]{ "buttons_13", "buttons_14", "buttons_15", "buttons_16", "buttons_17" }, "Hyper Casual v3");

        private static void Apply(string atlasPath, string[] spriteNames, string styleName)
        {
            var allSubs = AssetDatabase.LoadAllAssetsAtPath(atlasPath);
            if (allSubs == null || allSubs.Length == 0)
            {
                EditorUtility.DisplayDialog("Sparq",
                    $"Atlas not found:\n{atlasPath}", "OK");
                return;
            }

            // Collect named sprites
            var spriteByName = new System.Collections.Generic.Dictionary<string, Sprite>();
            foreach (var o in allSubs)
            {
                if (o is Sprite sp) spriteByName[sp.name] = sp;
            }

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            // Reposition the bar so all 5 buttons fit on-screen
            var barRT = bar.GetComponent<RectTransform>();
            if (barRT != null)
            {
                // Right-aligned so WORLD sits flush with quest box right edge
                barRT.anchorMin = new Vector2(1f, 1f);
                barRT.anchorMax = new Vector2(1f, 1f);
                barRT.pivot     = new Vector2(1f, 1f);
                barRT.anchoredPosition = new Vector2(-12, -300);
                barRT.sizeDelta = new Vector2(420, 100);
            }

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 6;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            for (int i = 0; i < BTNS.Length; i++)
            {
                var (btnName, iconFile, label) = BTNS[i];
                if (i >= spriteNames.Length) continue;
                if (!spriteByName.TryGetValue(spriteNames[i], out var bgSprite)) continue;

                BuildBtn(bar.transform, btnName, label, iconFile, bgSprite);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Buttons → {styleName}.\n\n" +
                $"Used sprite names: {string.Join(", ", spriteNames)}\n\n" +
                "Try other variants: 128 / 128a / 128b / 128c.\n" +
                "If still not right, the atlas has 50+ sprites — we can swap to any specific sprite name.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildBtn(Transform parent, string btnName, string label, string iconFile, Sprite bgSprite)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 90;
            le.minWidth = 60;

            var img = go.GetComponent<Image>();
            img.sprite = bgSprite;
            img.preserveAspect = true;

            // Icon
            string iconPath = ICON_DIR + iconFile;
            var iconImp = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (iconImp != null && iconImp.textureType != TextureImporterType.Sprite)
            {
                iconImp.textureType = TextureImporterType.Sprite;
                iconImp.SaveAndReimport();
            }
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (iconSprite != null)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                var rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 14);
                rt.sizeDelta = new Vector2(46, 46);
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = iconSprite;
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;
            }

            // Label
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0.5f);
            lrt.anchoredPosition = new Vector2(0, 14);
            lrt.sizeDelta = new Vector2(0, 18);
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 12;
            tm.enableAutoSizing = true;
            tm.fontSizeMin = 8;
            tm.fontSizeMax = 13;
            tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tm.overflowMode = TextOverflowModes.Overflow;
            tm.margin = new Vector4(2, 0, 2, 0);
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(0.10f, 0.05f, 0.02f); // dark brown - readable on bright buttons
            tm.outlineWidth = 0.2f;
            tm.outlineColor = new Color(1f, 0.95f, 0.75f, 0.9f); // cream outline for contrast
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;
        }
    }
}
