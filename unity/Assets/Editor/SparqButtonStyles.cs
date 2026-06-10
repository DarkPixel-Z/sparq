using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Three different button styles you can switch between for the top nav row.
    /// Each style replaces the current painted Fantasy Hero prefabs.
    /// </summary>
    public static class SparqButtonStyles
    {
        private const string ICON_DIR = "Assets/FantasyIconPack/256/";

        private static readonly (string btn, string icon, string label, Color color)[] BTNS = new[]
        {
            ("MapBtn",   "Map.png",      "MAP",   new Color(0.30f, 0.85f, 0.45f)),
            ("ShopBtn",  "Coin.png",     "SHOP",  new Color(0.95f, 0.65f, 0.20f)),
            ("BagBtn",   "Backpack.png", "BAG",   new Color(0.30f, 0.55f, 0.95f)),
            ("PetsBtn",  "GemRed.png",   "PETS",  new Color(0.95f, 0.40f, 0.55f)),
            ("WorldBtn", "GemBlue.png",  "WORLD", new Color(0.65f, 0.40f, 0.95f)),
        };

        // ── Style A: Flat modern circles ──
        [MenuItem("Sparq/111. Buttons → Flat circles (modern)")]
        public static void StyleCircles() => Apply(BuildCircle);

        // ── Style B: Square dark plates ──
        [MenuItem("Sparq/111a. Buttons → Dark square tiles")]
        public static void StyleDarkSquares() => Apply(BuildDarkSquare);

        // ── Style C: Outline-only minimal ──
        [MenuItem("Sparq/111b. Buttons → Outline minimal")]
        public static void StyleOutline() => Apply(BuildOutline);

        private static void Apply(System.Action<Transform, string, string, string, Color> builder)
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) return;

            for (int i = bar.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bar.transform.GetChild(i).gameObject);

            // Bar layout: horizontal flex
            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.padding = new RectOffset(8, 8, 4, 4);
                hlg.spacing = 8;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
            }

            foreach (var (btnName, icon, label, color) in BTNS)
            {
                builder(bar.transform, btnName, icon, label, color);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq", "✅ Button style applied. Hit ▶ Play.\n\nNot what you wanted? Try another menu (111 / 111a / 111b).", "OK");
        }

        private static void BuildCircle(Transform parent, string btnName, string iconFile, string label, Color color)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var img = go.GetComponent<Image>();
            img.sprite = MakeCircleSprite();
            img.color = color;
            img.preserveAspect = true;

            // Icon inside
            AddIcon(go.transform, iconFile, 32, new Color(1, 1, 1, 0.95f));
        }

        private static void BuildDarkSquare(Transform parent, string btnName, string iconFile, string label, Color color)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.10f, 0.06f, 0.18f, 0.95f); // dark square plate

            // Tinted accent bar at bottom
            var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(go.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(0, 3);
            bar.GetComponent<Image>().color = color;
            bar.GetComponent<Image>().raycastTarget = false;

            AddIcon(go.transform, iconFile, 32, color);
        }

        private static void BuildOutline(Transform parent, string btnName, string iconFile, string label, Color color)
        {
            var go = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var img = go.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // transparent fill

            // Outline (4 thin colored strips)
            CreateOutlineStrip(go.transform, "Top",    new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 2), color);
            CreateOutlineStrip(go.transform, "Bottom", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 2), color);
            CreateOutlineStrip(go.transform, "Left",   new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(2, 0), color);
            CreateOutlineStrip(go.transform, "Right",  new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(2, 0), color);

            AddIcon(go.transform, iconFile, 32, color);
        }

        private static void CreateOutlineStrip(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Color c)
        {
            var s = new GameObject(name, typeof(RectTransform), typeof(Image));
            s.transform.SetParent(parent, false);
            var rt = s.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            s.GetComponent<Image>().color = c;
            s.GetComponent<Image>().raycastTarget = false;
        }

        private static void AddIcon(Transform parent, string iconFile, float size, Color tint)
        {
            string path = ICON_DIR + iconFile;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (iconSprite == null) return;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(parent, false);
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);

            var img = icon.GetComponent<Image>();
            img.sprite = iconSprite;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static Sprite MakeCircleSprite()
        {
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Vector2 c = new Vector2(N * 0.5f, N * 0.5f);
            float r = N * 0.48f;
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                float a = Mathf.Clamp01(1f - (d - r));
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
