using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqButtonIcons
    {
        private const string ICON_DIR = "Assets/FantasyIconPack/256/";

        // Icon mapping: button name → icon PNG
        private static readonly (string btn, string icon)[] ICONS = new[]
        {
            ("MapBtn",   "Map.png"),
            ("ShopBtn",  "Coin.png"),
            ("BagBtn",   "Backpack.png"),
            ("PetsBtn",  "GemRed.png"),
            ("WorldBtn", "GemBlue.png"),
        };

        [MenuItem("Sparq/110. Replace top button text with icons")]
        public static void Apply()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK");
                return;
            }

            int updated = 0;
            foreach (var (btnName, iconName) in ICONS)
            {
                var t = bar.transform.Find(btnName);
                if (t == null) continue;

                string iconPath = ICON_DIR + iconName;
                EnsureSprite(iconPath);
                var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (iconSprite == null) continue;

                // Hide all TMP_Text labels in the button
                foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                {
                    tmp.enabled = false;
                }

                // Add an Icon Image inside (or update existing)
                Transform iconT = null;
                foreach (Transform child in t.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null && child.name == "Icon") { iconT = child; break; }
                }
                GameObject iconGO;
                if (iconT == null)
                {
                    iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    // Parent it to the deepest button so it sits on top
                    iconGO.transform.SetParent(t, false);
                }
                else iconGO = iconT.gameObject;

                var rt = iconGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 4); // slight offset up
                rt.sizeDelta = new Vector2(36, 36);

                var img = iconGO.GetComponent<Image>();
                if (img == null) img = iconGO.AddComponent<Image>();
                img.sprite = iconSprite;
                img.preserveAspect = true;
                img.color = Color.white;
                img.raycastTarget = false;

                updated++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ {updated} button(s) updated with icons:\n\n" +
                "• MAP → 🗺 Map.png\n" +
                "• SHOP → 🪙 Coin.png\n" +
                "• BAG → 🎒 Backpack.png\n" +
                "• PETS → 💎 GemRed\n" +
                "• WORLD → 💎 GemBlue\n\n" +
                "Hit ▶ Play.", "OK");
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
