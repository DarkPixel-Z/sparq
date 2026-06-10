using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 173: Expert pass on the SocialPanel.
    ///   • Buttons: same shape, plum (inactive) / gold (active) — matches the
    ///     dark navy + gold palette of the chat prefab
    ///   • Content slightly smaller + ContentSizeFitter removed (was causing
    ///     layout-loop glitching)
    ///   • Cleaner spacing
    /// </summary>
    public static class SparqExpertSocial173
    {
        private const string TAB_BG     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_02_White_Bg.png";
        private const string TAB_BORDER = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_02_White_Border.png";

        // Palette matched to the dark navy + gold chat prefab
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color PLUM       = new Color(0.36f, 0.22f, 0.50f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/173. Expert pass — matched palette + glitch fix")]
        public static void Apply()
        {
            EnsureSprite(TAB_BG);
            EnsureSprite(TAB_BORDER);
            var bgSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_BG);
            var borderSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TAB_BORDER);

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // ───── Tabs: same sprite + plum/gold tints ─────
            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                var rt = tabs.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-90, 0);
                rt.sizeDelta = new Vector2(220, -220);

                var vlg = tabs.GetComponent<VerticalLayoutGroup>();
                if (vlg != null)
                {
                    vlg.spacing = 16;
                    vlg.padding = new RectOffset(0, 0, 40, 40);
                }

                for (int i = 0; i < tabs.childCount; i++)
                {
                    var tab = tabs.GetChild(i);
                    var img = tab.GetComponent<Image>();
                    if (img != null && bgSprite != null)
                    {
                        img.sprite = bgSprite;
                        img.type = Image.Type.Sliced;
                        img.preserveAspect = false;
                        img.color = (i == 0) ? GOLD : PLUM;
                    }

                    // Add border overlay child (sliced, no raycast)
                    var border = tab.Find("Border");
                    if (border == null && borderSprite != null)
                    {
                        var bdr = new GameObject("Border", typeof(RectTransform), typeof(Image));
                        bdr.transform.SetParent(tab, false);
                        var brt = bdr.GetComponent<RectTransform>();
                        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
                        var bImg = bdr.GetComponent<Image>();
                        bImg.sprite = borderSprite;
                        bImg.type = Image.Type.Sliced;
                        bImg.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.55f);
                        bImg.raycastTarget = false;
                    }

                    var le = tab.GetComponent<LayoutElement>();
                    if (le == null) le = tab.gameObject.AddComponent<LayoutElement>();
                    le.preferredHeight = 92;
                    le.preferredWidth = 0;
                    le.flexibleWidth = 1;

                    foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 24;
                        tm.fontStyle = FontStyles.Bold;
                        tm.color = (i == 0) ? DEEP_NAVY : CREAM;
                        tm.outlineWidth = 0.30f;
                        tm.outlineColor = (i == 0) ? new Color(1, 1, 1, 0.85f) : new Color(0, 0, 0, 0.95f);
                    }
                }
            }

            // TabGroup color tints — active = gold, inactive = plum
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                so.FindProperty("activeBg").colorValue   = GOLD;
                so.FindProperty("inactiveBg").colorValue = PLUM;
                so.FindProperty("activeFg").colorValue   = DEEP_NAVY;
                so.FindProperty("inactiveFg").colorValue = CREAM;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Strip TabSpriteSwap so it doesn't re-set sprites
            foreach (var s in social.GetComponents<TabSpriteSwap>())
                Object.DestroyImmediate(s);

            // ───── Content: smaller + remove ContentSizeFitter (was glitching) ─────
            var content = social.transform.Find("Content");
            if (content != null)
            {
                var rt = content.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(40, 40);
                rt.offsetMax = new Vector2(-320, -120);

                for (int i = 0; i < content.childCount; i++)
                {
                    var c = content.GetChild(i);
                    if (!c.name.EndsWith("_Tab")) continue;
                    var crt = c.GetComponent<RectTransform>();
                    if (crt == null) continue;
                    crt.localScale = new Vector3(0.92f, 0.92f, 1f);
                    crt.anchorMin = new Vector2(0.5f, 0.5f);
                    crt.anchorMax = new Vector2(0.5f, 0.5f);
                    crt.pivot     = new Vector2(0.5f, 0.5f);
                    crt.anchoredPosition = Vector2.zero;

                    // Strip the ContentSizeFitter we added earlier — it's the glitch source
                    foreach (var csf in c.GetComponentsInChildren<ContentSizeFitter>(true))
                    {
                        // Only nuke fitters we added (our auto-fix on direct ScrollRect content)
                        var sr = csf.GetComponentInParent<ScrollRect>();
                        if (sr != null && sr.content == csf.transform)
                            Object.DestroyImmediate(csf);
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Expert pass:\n" +
                "• Tabs: plum (inactive) / gold (active) — matches chat panel palette\n" +
                "• Border overlay (gold halo) on each tab\n" +
                "• Active = dark navy text on gold, inactive = cream on plum\n" +
                "• Content scaled 0.92× and ContentSizeFitter removed → glitch fixed\n\n" +
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
