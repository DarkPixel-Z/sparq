using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqFantasyButtons
    {
        private const string UI_DIR = "Assets/Art/UI/";
        private const string ICON_DIR = "Assets/FantasyIconPack/256/";

        // Color variants → button sprite suffix
        private static readonly (string btn, string icon, string color)[] BTNS = new[]
        {
            ("MapBtn",   "Map.png",      "brown"),  // wooden brown
            ("ShopBtn",  "Coin.png",     "beige"),  // parchment beige
            ("BagBtn",   "Backpack.png", "blue"),   // metal blue
            ("PetsBtn",  "GemRed.png",   "brown"),  // wooden brown
            ("WorldBtn", "GemBlue.png",  "grey"),   // stone grey
        };

        [MenuItem("Sparq/123. Fantasy SQUARE buttons (wood/stone/parchment)")]
        public static void Square() => Apply("buttonSquare");

        [MenuItem("Sparq/123a. Fantasy ROUND buttons")]
        public static void Round() => Apply("buttonRound");

        [MenuItem("Sparq/123b. Fantasy LONG buttons (text fits inside)")]
        public static void Long() => Apply("buttonLong");

        private static void Apply(string buttonStyle)
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Layout
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.spacing = 8;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            foreach (var (btnName, iconFile, color) in BTNS)
            {
                BuildButton(bar.transform, btnName, iconFile, $"{buttonStyle}_{color}.png");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Buttons → {buttonStyle} style.\n\n" +
                "Classic Kenney-style fantasy UI:\n" +
                "• Wood / parchment / metal / stone\n" +
                "• Icon centered inside\n\n" +
                "Try other variants: Sparq → 123 (square), 123a (round), 123b (long).", "OK");
        }

        private static void BuildButton(Transform parent, string btnName, string iconFile, string buttonSpriteName)
        {
            string buttonPath = UI_DIR + buttonSpriteName;
            EnsureSprite(buttonPath);
            var buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(buttonPath);
            if (buttonSprite == null) return;

            string iconPath = ICON_DIR + iconFile;
            EnsureSprite(iconPath);
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            // Button GameObject
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var img = go.GetComponent<Image>();
            img.sprite = buttonSprite;
            img.preserveAspect = false;
            img.type = Image.Type.Sliced;  // 9-slice if supported

            // Icon centered
            if (iconSprite != null)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(go.transform, false);
                var rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(32, 32);
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = iconSprite;
                iimg.preserveAspect = true;
                iimg.color = Color.white;
                iimg.raycastTarget = false;
            }
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }
    }
}
