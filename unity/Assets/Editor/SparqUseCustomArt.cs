using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqUseCustomArt
    {
        private const string UNA_PATH  = "Assets/Art/Sparq/una-mage.png";
        private const string LOGO_PATH = "Assets/Art/Sparq/sparq-logo.png";

        [MenuItem("Sparq/92. Wire NEW Una mage + new logo")]
        public static void Apply()
        {
            ImportAsSprite(UNA_PATH);
            ImportAsSprite(LOGO_PATH);

            var unaSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(UNA_PATH);
            var logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LOGO_PATH);

            string report = "";

            // --- 1. Update help button (was Una) to use the new mage axolotl ---
            if (unaSprite != null)
            {
                var help = GameObject.Find("HelpIcon");
                if (help != null)
                {
                    foreach (var img in help.GetComponentsInChildren<Image>(true))
                    {
                        if (img == null) continue;
                        if (img.gameObject.name == "Una")
                        {
                            img.sprite = unaSprite;
                            img.color = Color.white;
                        }
                    }
                    report += "✅ Una help button → mage axolotl\n";
                }
                else report += "⚠ HelpIcon not in scene — run Sparq → 84 first\n";
            }
            else
            {
                report += $"❌ {UNA_PATH} not found — save the file first\n";
            }

            // --- 2. Update logo ---
            if (logoSprite != null)
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var old = GameObject.Find("GameTitle");
                    if (old != null) Object.DestroyImmediate(old);

                    var go = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(canvas.transform, false);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(14f, -10f);

                    // Auto-size by aspect — height 90, width by ratio
                    float aspect = logoSprite.rect.width / logoSprite.rect.height;
                    float h = 90f;
                    float w = h * aspect;
                    if (w > 320f) { w = 320f; h = w / aspect; }
                    rt.sizeDelta = new Vector2(w, h);

                    var img = go.GetComponent<Image>();
                    img.sprite = logoSprite;
                    img.preserveAspect = true;
                    img.raycastTarget = false;

                    report += $"✅ Logo wired — {w:F0}×{h:F0}\n";
                }
            }
            else
            {
                report += $"❌ {LOGO_PATH} not found — save the file first\n";
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Custom Art", report + "\nHit ▶ Play.", "OK");
        }

        private static void ImportAsSprite(string path)
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
