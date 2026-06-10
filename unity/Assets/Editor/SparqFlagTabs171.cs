using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 171: Try Title_Flag-style tab buttons (banner/flag shape, full bright)
    /// + nudge tabs further left + ensure ScrollRect content has a ContentSizeFitter
    /// so the scroll inside Chat actually has scrollable content.
    /// </summary>
    public static class SparqFlagTabs171
    {
        private const string TITLE_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Title/";

        // Different colored flags per tab
        private static readonly string[] FLAGS = new[]
        {
            "Title_Flag_01_Blue.Png",
            "Title_Flag_01_Red.Png",
            "Title_Flag_01_Green.Png",
            "Title_Flag_01_Purple.Png",
        };

        private static readonly Color CREAM = new Color(1f, 0.95f, 0.82f);
        private static readonly Color GOLD  = new Color(1f, 0.82f, 0.32f);

        [MenuItem("Sparq/171. Flag-style tabs + force scroll content sizing")]
        public static void Apply()
        {
            foreach (var f in FLAGS) EnsureSprite(TITLE_DIR + f);

            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Tabs nudged further left + flag sprites
            var tabs = social.transform.Find("Tabs");
            if (tabs != null)
            {
                var rt = tabs.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 0.5f);
                rt.anchoredPosition = new Vector2(-90, 0);
                rt.sizeDelta = new Vector2(220, -200);

                for (int i = 0; i < tabs.childCount && i < FLAGS.Length; i++)
                {
                    var tab = tabs.GetChild(i);
                    var img = tab.GetComponent<Image>();
                    if (img != null)
                    {
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(TITLE_DIR + FLAGS[i]);
                        if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
                        img.color = Color.white;
                        img.preserveAspect = false;
                    }
                    var le = tab.GetComponent<LayoutElement>();
                    if (le != null)
                    {
                        le.preferredHeight = 96;
                        le.preferredWidth = 0;
                        le.flexibleWidth = 1;
                    }
                    foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 24;
                        tm.fontStyle = FontStyles.Bold;
                        tm.color = CREAM;
                        tm.outlineWidth = 0.30f;
                        tm.outlineColor = new Color(0, 0, 0, 0.95f);
                    }
                }
            }

            // 2. Force ScrollRect content to size itself so scroll has range
            int srFixed = 0;
            var content = social.transform.Find("Content");
            if (content != null)
            {
                foreach (var sr in content.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (sr == null || sr.content == null) continue;

                    // Add VLG+CSF if missing so children stack and content grows
                    var vlg = sr.content.GetComponent<VerticalLayoutGroup>();
                    if (vlg == null && sr.content.childCount > 1)
                    {
                        vlg = sr.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                        vlg.childControlHeight = true;
                        vlg.spacing = 8;
                    }
                    var csf = sr.content.GetComponent<ContentSizeFitter>();
                    if (csf == null)
                    {
                        csf = sr.content.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    sr.movementType = ScrollRect.MovementType.Elastic;
                    sr.inertia = true;
                    sr.scrollSensitivity = 24f;
                    Canvas.ForceUpdateCanvases();
                    sr.verticalNormalizedPosition = 1f;
                    srFixed++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Flag tabs + scroll fix:\n• Tabs: -90px from right (was -60)\n• Sprites → Title_Flag_01 (Blue/Red/Green/Purple banners)\n• {srFixed} ScrollRect(s) wired with ContentSizeFitter\n\nHit ▶ Play.", "OK");
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
