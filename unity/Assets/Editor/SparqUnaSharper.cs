using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 201: Make the Una help icon clearer — bigger size, less padding so the
    /// sprite reads larger, and re-import the texture with bilinear filtering and
    /// no compression so it stays crisp.
    /// </summary>
    public static class SparqUnaSharper
    {
        private const string UNA_PATH = "Assets/Art/Sparq/una-mage.png";

        [MenuItem("Sparq/201. Sharpen Una help icon")]
        public static void Apply()
        {
            // 1. Re-import Una texture with high quality settings
            var imp = AssetImporter.GetAtPath(UNA_PATH) as TextureImporter;
            if (imp != null)
            {
                bool changed = false;
                if (imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency)
                { imp.alphaIsTransparency = true; changed = true; }
                if (imp.filterMode != FilterMode.Bilinear)
                { imp.filterMode = FilterMode.Bilinear; changed = true; }
                if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                { imp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (imp.maxTextureSize < 1024)
                { imp.maxTextureSize = 1024; changed = true; }
                if (imp.mipmapEnabled)
                { imp.mipmapEnabled = false; changed = true; } // UI sprites don't need mipmaps
                if (changed && !Application.isPlaying) imp.SaveAndReimport();
            }

            // 2. Resize HelpIcon in the scene + tighten internal padding so Una fills more
            var help = GameObject.Find("HelpIcon");
            if (help == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "Texture re-imported.\nHelpIcon GameObject not found in scene — re-run Sparq → 135 first to recreate it, then run this again.",
                    "OK");
                return;
            }

            var hrt = help.GetComponent<RectTransform>();
            if (hrt != null) hrt.sizeDelta = new Vector2(110, 110);

            var unaT = help.transform.Find("Una");
            if (unaT != null)
            {
                var urt = unaT.GetComponent<RectTransform>();
                if (urt != null)
                {
                    urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                    // Less padding — was 10px, now 5px → larger Una
                    urt.offsetMin = new Vector2(5, 5);
                    urt.offsetMax = new Vector2(-5, -5);
                }
                var uimg = unaT.GetComponent<Image>();
                if (uimg != null)
                {
                    uimg.preserveAspect = true;
                    // Reload sprite (in case re-import changed the asset reference)
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(UNA_PATH);
                    if (sp != null) uimg.sprite = sp;
                }
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Sparq",
                "✅ Una sharpened:\n• Texture: bilinear filter, uncompressed, 1024 max\n• HelpIcon: 80→110px, padding 10→5px\n\nHit ▶ Play.",
                "OK");
        }
    }
}
