using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqFixLogoButtons
    {
        [MenuItem("Sparq/97. Force-wire logo + smaller top buttons")]
        public static void Apply()
        {
            // 1. FORCE the custom PNG logo (re-import + wire)
            string logoPath = "Assets/Art/Sparq/sparq-logo.png";
            var imp = AssetImporter.GetAtPath(logoPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "sparq-logo.png not found at:\n" + logoPath +
                    "\n\nFile may not be a valid image. Try opening it manually first.", "OK");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var oldLogo = GameObject.Find("GameTitle");
            if (oldLogo != null) Object.DestroyImmediate(oldLogo);

            var go = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -8f);

            float aspect = sprite.rect.width / sprite.rect.height;
            float h = 120f;             // bigger now
            float w = h * aspect;
            if (w > 380f) { w = 380f; h = w / aspect; }
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 2. SHRINK top buttons more
            var bar = GameObject.Find("HomeNavButtons");
            if (bar != null)
            {
                var brt = bar.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(1f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(1f, 1f);
                brt.anchoredPosition = new Vector2(-340f, -8f); // top-right area, leaving space for HUD
                brt.sizeDelta = new Vector2(440, 36);

                foreach (Transform t in bar.transform)
                {
                    var rt2 = t.GetComponent<RectTransform>();
                    if (rt2 != null) rt2.sizeDelta = new Vector2(80, 32);
                    var le = t.GetComponent<LayoutElement>();
                    if (le == null) le = t.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = 80;
                    le.preferredHeight = 32;
                    le.flexibleWidth = 0;
                    foreach (var tmp in t.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (tmp == null) continue;
                        tmp.fontSize = 10;
                        tmp.fontStyle = FontStyles.Bold;
                        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                        tmp.overflowMode = TextOverflowModes.Overflow;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }
                }
            }

            // 3. Move HUD a bit down to clear logo line
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var hrt = hud.GetComponent<RectTransform>();
                hrt.anchoredPosition = new Vector2(-14f, -52f); // below buttons row
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Done.\n\n" +
                $"• Logo: 120px tall, scale {w:F0}×{h:F0}\n" +
                "• Top buttons: 80×32, 10pt font\n" +
                "• HUD repositioned below button row\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
