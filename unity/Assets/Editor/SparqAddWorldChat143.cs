using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 143: Adds a 6th top button "WORLD" with chat icon
    /// for community / chats / friends. Widens the top bar so 6 fit cleanly.
    /// </summary>
    public static class SparqAddWorldChat143
    {
        private const string FH_ICON = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/";
        private const string FH_BTN  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/";

        private static readonly Color CREAM = new Color(1f, 0.95f, 0.82f);

        [MenuItem("Sparq/143. Add WORLD chat button (6th top button)")]
        public static void Apply()
        {
            EnsureSprite(FH_ICON + "ItemIcon_Chat.png");
            EnsureSprite(FH_BTN  + "Button_01_Mian_l_Bg_Brown.png");

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found. Run #138 first.", "OK"); return; }

            // Widen bar to accommodate 6 buttons
            var rt = bar.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(360, 88);
                rt.anchoredPosition = new Vector2(-56, -200);
            }

            // Remove existing WorldBtn (in case re-running)
            var oldWorld = bar.transform.Find("WorldBtn");
            if (oldWorld != null) Object.DestroyImmediate(oldWorld.gameObject);

            // Build new WorldBtn
            var bgSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(FH_BTN  + "Button_01_Mian_l_Bg_Brown.png");
            var iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FH_ICON + "ItemIcon_Chat.png");

            var go = new GameObject("WorldBtn",
                typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(bar.transform, false);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minWidth = 50;
            le.preferredHeight = 88;

            var img = go.GetComponent<Image>();
            if (bgSprite != null) { img.sprite = bgSprite; img.type = Image.Type.Sliced; }
            else img.color = new Color(0.55f, 0.36f, 0.20f);

            // Icon
            if (iconSprite != null)
            {
                var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconGO.transform.SetParent(go.transform, false);
                var irt = iconGO.GetComponent<RectTransform>();
                irt.anchorMin = new Vector2(0.5f, 0.5f);
                irt.anchorMax = new Vector2(0.5f, 0.5f);
                irt.pivot     = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = new Vector2(0, 8);
                irt.sizeDelta = new Vector2(40, 40);
                var iimg = iconGO.GetComponent<Image>();
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
            lrt.anchoredPosition = new Vector2(0, 12);
            lrt.sizeDelta = new Vector2(0, 18);
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = "WORLD";
            tm.fontSize = 12;
            tm.enableAutoSizing = true;
            tm.fontSizeMin = 8;
            tm.fontSizeMax = 13;
            tm.fontStyle = FontStyles.Bold;
            tm.color = CREAM;
            tm.outlineWidth = 0.25f;
            tm.outlineColor = new Color(0.10f, 0.06f, 0.02f, 1f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tm.overflowMode = TextOverflowModes.Overflow;

            // Click → coming-soon toast (later: chat panel)
            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                try
                {
                    var canvas = bar.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        Sparq.UI.XPFloater.Spawn(canvas.transform,
                            go.transform.position + new Vector3(0, 80, 0),
                            "World — chats coming soon!",
                            new Color(0.55f, 0.85f, 1f));
                    }
                    Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                } catch {}
            });

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ WORLD button added.\n\n" +
                "• Top bar widened to fit 6 buttons\n" +
                "• Icon: chat bubble (ItemIcon_Chat)\n" +
                "• Tap → 'Coming soon' toast (we'll wire chats panel next)\n\n" +
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
