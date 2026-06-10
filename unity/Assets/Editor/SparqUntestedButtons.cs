using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqUntestedButtons
    {
        private const string PLATFORMER_BTN  = "Assets/2D Fantasy Platformer Pack/GUI/Buttons/menu_button_1.png";
        private const string PLATFORMER_BTN2 = "Assets/2D Fantasy Platformer Pack/GUI/Buttons/menu_button_2.png";
        private const string ICON_PLATE      = "Assets/2D Fantasy Platformer Pack/GUI/Icon Buttons/button_background.png";
        private const string MONSTER_BTN     = "Assets/2D Fantasy Monster Sprite Pack/UI/Button.png";

        private const string ICON_DIR = "Assets/FantasyIconPack/256/";

        private static readonly (string btn, string label, string icon)[] BTNS = new[]
        {
            ("MapBtn",   "MAP",   "Map.png"),
            ("ShopBtn",  "SHOP",  "Coin.png"),
            ("BagBtn",   "BAG",   "Backpack.png"),
            ("PetsBtn",  "PETS",  "GemRed.png"),
            ("WorldBtn", "WORLD", "GemBlue.png"),
        };

        [MenuItem("Sparq/126. Try → Pixel art menu_button_1 (Platformer)")]
        public static void Pixel1() => Apply(PLATFORMER_BTN, "Pixel Menu 1");

        [MenuItem("Sparq/126a. Try → Pixel art menu_button_2 (Platformer)")]
        public static void Pixel2() => Apply(PLATFORMER_BTN2, "Pixel Menu 2");

        [MenuItem("Sparq/126b. Try → Icon plate (Platformer)")]
        public static void IconPlate() => Apply(ICON_PLATE, "Icon Plate");

        [MenuItem("Sparq/126c. Try → Monster Pack Button")]
        public static void Monster() => Apply(MONSTER_BTN, "Monster Pack");

        private static void Apply(string buttonPath, string styleName)
        {
            EnsureSprite(buttonPath, true); // pixel art = point filter
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonPath);
            if (bgSprite == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    $"Sprite not found:\n{buttonPath}", "OK");
                return;
            }

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Layout
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 6;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            foreach (var (btnName, label, iconFile) in BTNS)
            {
                BuildBtn(bar.transform, btnName, label, iconFile, bgSprite);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Buttons → {styleName}.\n\n" +
                $"Source: {System.IO.Path.GetFileName(buttonPath)}\n\n" +
                "Try other variants: Sparq → 126 / 126a / 126b / 126c.\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildBtn(Transform parent, string btnName, string label, string iconFile, Sprite bgSprite)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 64;  // taller to fit icon + label

            var img = go.GetComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;

            // Icon (top-half)
            string iconPath = ICON_DIR + iconFile;
            EnsureSprite(iconPath, false);
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (iconSprite != null)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                var rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 8);  // shift up to make room for label
                rt.sizeDelta = new Vector2(40, 40);  // bigger icons
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = iconSprite;
                iimg.preserveAspect = true;
                iimg.raycastTarget = false;
            }

            // Label (bottom-half)
            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0); lrt.anchorMax = new Vector2(1, 0);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.anchoredPosition = new Vector2(0, 4);
            lrt.sizeDelta = new Vector2(0, 16);
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 11;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(0.25f, 0.15f, 0.05f);  // dark wood/brown text
            tm.alignment = TextAlignmentOptions.Center;
            tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tm.raycastTarget = false;
        }

        private static void EnsureSprite(string path, bool pointFilter)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                changed = true;
            }
            if (pointFilter && imp.filterMode != FilterMode.Point)
            {
                imp.filterMode = FilterMode.Point;  // crisp pixel art
                changed = true;
            }
            if (changed) imp.SaveAndReimport();
        }
    }
}
