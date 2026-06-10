using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqLogoButtonAndUna
    {
        [MenuItem("Sparq/99. Logo as button + HUD top + Una in help")]
        public static void Apply()
        {
            // ── 1. LOGO AS BUTTON ────────────────────────────────────────────
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var oldLogo = GameObject.Find("GameTitle");
            if (oldLogo != null) Object.DestroyImmediate(oldLogo);

            // Frame container (rounded button shape)
            var titleGO = new GameObject("GameTitle", typeof(RectTransform), typeof(Image), typeof(Button));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -8f);
            rt.sizeDelta = new Vector2(290, 110);

            // Yellow button-like background frame
            var frameImg = titleGO.GetComponent<Image>();
            frameImg.color = new Color(1f, 0.85f, 0.30f, 0.95f); // golden frame
            frameImg.raycastTarget = false;

            // Inner dark plate
            var plate = new GameObject("Plate", typeof(RectTransform), typeof(Image));
            plate.transform.SetParent(titleGO.transform, false);
            var prt = plate.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(4, 4); prt.offsetMax = new Vector2(-4, -4);
            plate.GetComponent<Image>().color = new Color(0.20f, 0.05f, 0.30f, 0.85f);
            plate.GetComponent<Image>().raycastTarget = false;

            // Logo image inside the frame
            string logoPath = "Assets/Art/Sparq/sparq-logo.png";
            EnsureSprite(logoPath);
            var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            if (logoSprite != null)
            {
                var logo = new GameObject("Logo", typeof(RectTransform), typeof(Image));
                logo.transform.SetParent(plate.transform, false);
                var lrt = logo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(2, 2); lrt.offsetMax = new Vector2(-2, -2);
                var img = logo.GetComponent<Image>();
                img.sprite = logoSprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }

            // ── 2. KARU HUD → TOP RIGHT ──────────────────────────────────────
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchorMin = new Vector2(1f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(1f, 1f);
                hrt.anchoredPosition = new Vector2(-14f, -8f);  // top right corner
                hrt.sizeDelta = new Vector2(320, 96);
            }

            // ── 3. UNA IN HELP BUTTON ────────────────────────────────────────
            string unaPath = "Assets/Art/Sparq/una-mage.png";
            EnsureSprite(unaPath);
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unaPath);

            var help = GameObject.Find("HelpIcon");
            if (help != null && unaSprite != null)
            {
                // Find the Una child Image and set its sprite
                Image unaImg = null;
                foreach (var img in help.GetComponentsInChildren<Image>(true))
                {
                    if (img == null) continue;
                    if (img.gameObject.name == "Una") { unaImg = img; break; }
                }
                if (unaImg != null)
                {
                    unaImg.sprite = unaSprite;
                    unaImg.color = Color.white;
                    unaImg.preserveAspect = true;
                }
                // Make help icon a bit bigger so Una reads
                var hrt2 = help.GetComponent<RectTransform>();
                hrt2.sizeDelta = new Vector2(96, 96);
            }

            // Move top buttons down so they don't crowd the bigger logo
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 1f);
                brt.anchorMax = new Vector2(0.5f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -130f);
                brt.sizeDelta = new Vector2(440, 36);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            string status = $"Logo sprite: {(logoSprite != null ? "✓ wired" : "❌ NOT FOUND")}\n" +
                            $"Una sprite: {(unaSprite != null ? "✓ wired" : "❌ NOT FOUND")}";
            EditorUtility.DisplayDialog("Sparq",
                "✅ Done.\n\n" +
                "• Logo wrapped in golden button frame (top-left)\n" +
                "• Karu HUD → top-right corner\n" +
                "• Help icon → Una mage sprite\n" +
                "• Top button row pushed below logo\n\n" +
                status + "\n\nHit ▶ Play.", "OK");
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
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
        }
    }
}
