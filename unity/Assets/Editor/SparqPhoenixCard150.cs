using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 150: Phoenix-themed Daily Trial card.
    ///   • Flame-tinted bubble bg
    ///   • Fire-pictogram embers flanking the ribbon
    ///   • Title in golden ember + crimson halo
    ///   • Glyph circle fiery orange
    /// </summary>
    public static class SparqPhoenixCard150
    {
        private const string FH_FIRE = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Fire.Png";

        // Phoenix palette
        private static readonly Color BUBBLE_FLAME = new Color(1.00f, 0.78f, 0.42f, 0.96f); // warm parchment
        private static readonly Color RIBBON_FIRE = new Color(0.85f, 0.30f, 0.10f);
        private static readonly Color TITLE_GOLD  = new Color(1.00f, 0.85f, 0.30f);
        private static readonly Color GLYPH_FIRE  = new Color(0.95f, 0.45f, 0.18f);
        private static readonly Color FLAME       = new Color(1.00f, 0.55f, 0.20f);
        private static readonly Color CRIMSON     = new Color(0.62f, 0.13f, 0.18f);
        private static readonly Color CREAM       = new Color(1.00f, 0.95f, 0.82f);

        [MenuItem("Sparq/150. Trial card → Phoenix theme")]
        public static void Apply()
        {
            EnsureSprite(FH_FIRE);

            var card = GameObject.Find("DailyTrialCard");
            if (card == null) { EditorUtility.DisplayDialog("Sparq", "DailyTrialCard not found.", "OK"); return; }

            // 1. Recolor bubble bg to flame parchment
            var img = card.GetComponent<Image>();
            if (img != null) img.color = BUBBLE_FLAME;

            // 2. Ribbon → ember-red w/ gold text
            var ribbon = card.transform.Find("Ribbon");
            if (ribbon != null)
            {
                var rImg = ribbon.GetComponent<Image>();
                if (rImg != null) rImg.color = RIBBON_FIRE;
                foreach (var tm in ribbon.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = TITLE_GOLD;
                    tm.outlineWidth = 0.20f;
                    tm.outlineColor = new Color(0.20f, 0.05f, 0.02f, 0.9f);
                }

                AttachEmber(ribbon, "EmberLeft",  -1f);
                AttachEmber(ribbon, "EmberRight", +1f);
            }

            // 3. Glyph circle fiery + dark text
            var glyphBg = card.transform.Find("GlyphBg");
            if (glyphBg != null)
            {
                var gImg = glyphBg.GetComponent<Image>();
                if (gImg != null) gImg.color = GLYPH_FIRE;
                foreach (var tm in glyphBg.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = new Color(0.20f, 0.06f, 0.02f);
                    tm.outlineWidth = 0.20f;
                    tm.outlineColor = TITLE_GOLD;
                }
            }

            // 4. Title → bold golden ember w/ crimson halo
            var title = card.transform.Find("Title");
            if (title != null)
            {
                foreach (var tm in title.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = TITLE_GOLD;
                    tm.fontStyle = FontStyles.Bold | FontStyles.SmallCaps;
                    tm.outlineWidth = 0.28f;
                    tm.outlineColor = CRIMSON;
                }
            }

            // 5. Subtitle / reward → warm browns for parchment harmony
            var sub = card.transform.Find("Sub");
            if (sub != null)
                foreach (var tm in sub.GetComponentsInChildren<TMP_Text>(true))
                    tm.color = new Color(0.30f, 0.18f, 0.10f);

            var reward = card.transform.Find("Reward");
            if (reward != null)
                foreach (var tm in reward.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = new Color(0.55f, 0.20f, 0.05f);
                    tm.fontStyle = FontStyles.Bold;
                }

            // 6. BEGIN button → flame orange w/ dark text
            var begin = card.transform.Find("BeginBtn");
            if (begin != null)
            {
                var bImg = begin.GetComponent<Image>();
                if (bImg != null) bImg.color = FLAME;
                foreach (var tm in begin.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.color = new Color(0.20f, 0.05f, 0.02f);
                    tm.outlineWidth = 0.18f;
                    tm.outlineColor = TITLE_GOLD;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Phoenix theme applied:\n\n" +
                "• Bubble: warm flame parchment\n" +
                "• Ribbon: ember red w/ gold text + flame embers either side\n" +
                "• Title: bold gold w/ crimson halo (small caps)\n" +
                "• BEGIN: flame orange\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void AttachEmber(Transform ribbon, string name, float side)
        {
            // Remove if already there (idempotent)
            var existing = ribbon.Find(name);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var fire = AssetDatabase.LoadAssetAtPath<Sprite>(FH_FIRE);
            if (fire == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(ribbon, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(side > 0 ? 1 : 0, 0.5f);
            rt.anchorMax = new Vector2(side > 0 ? 1 : 0, 0.5f);
            rt.pivot     = new Vector2(side > 0 ? 1 : 0, 0.5f);
            rt.anchoredPosition = new Vector2(side * 6, 0);
            rt.sizeDelta = new Vector2(22, 22);
            var img = go.GetComponent<Image>();
            img.sprite = fire;
            img.color = TITLE_GOLD;
            img.preserveAspect = true;
            img.raycastTarget = false;
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
