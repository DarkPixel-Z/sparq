using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 136: Logo polish — re-runs a higher-precision chroma key
    /// (cleaner edges) and sizes/positions the logo for clarity.
    /// </summary>
    public static class SparqLogoFix136
    {
        private const string SRC  = "Assets/Art/Sparq/sparq-logo.png";
        private const string DEST = "Assets/Art/Sparq/sparq-logo-transparent.png";

        [MenuItem("Sparq/136. Logo polish (clearer + larger top-left)")]
        public static void Apply()
        {
            // 1. Re-key with tighter feather for cleaner edges
            BuildTransparentLogo();

            // 2. Apply to GameTitle, scale up
            var title = GameObject.Find("GameTitle");
            if (title == null)
            {
                EditorUtility.DisplayDialog("Sparq", "GameTitle not found.", "OK");
                return;
            }

            // Strip extra graphic components
            foreach (var g in title.GetComponents<Graphic>())
                if (!(g is Image img && img.sprite != null && img.sprite.name.Contains("transparent")))
                    Object.DestroyImmediate(g);

            // Wipe child decorations
            for (int i = title.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(title.transform.GetChild(i).gameObject);

            // Strip non-essential components
            foreach (var c in title.GetComponents<Component>())
            {
                if (c is RectTransform || c is Transform || c is CanvasRenderer) continue;
                if (c is Image) continue;
                Object.DestroyImmediate(c);
            }

            // Apply transparent sprite
            var existing = title.GetComponent<Image>();
            if (existing == null) existing = title.AddComponent<Image>();
            existing.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DEST);
            existing.preserveAspect = true;
            existing.raycastTarget = false;
            existing.color = Color.white;

            // Position larger top-left, slightly inset
            var rt = title.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(8, -8);
            rt.sizeDelta = new Vector2(280, 130);

            // Render on top
            title.transform.SetAsLastSibling();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Logo polished:\n\n" +
                "• Tighter chroma-key (cleaner edges)\n" +
                "• Larger size (280×130) top-left\n" +
                "• Bilinear filtering for crispness\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildTransparentLogo()
        {
            var srcImp = AssetImporter.GetAtPath(SRC) as TextureImporter;
            if (srcImp == null) return;
            bool changed = false;
            if (!srcImp.isReadable) { srcImp.isReadable = true; changed = true; }
            if (srcImp.textureCompression != TextureImporterCompression.Uncompressed)
            { srcImp.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
            if (changed) srcImp.SaveAndReimport();

            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(SRC);
            if (src == null) return;

            int w = src.width, h = src.height;
            var pixels = src.GetPixels();
            // Sample 4 corners to be robust to gradients
            Color[] keys = new[] {
                src.GetPixel(2, 2),
                src.GetPixel(w-3, 2),
                src.GetPixel(2, h-3),
                src.GetPixel(w-3, h-3)
            };
            // Use the average corner color as the key
            float kr = 0, kg = 0, kb = 0;
            foreach (var c in keys) { kr += c.r; kg += c.g; kb += c.b; }
            kr /= 4; kg /= 4; kb /= 4;

            // Tighter threshold for cleaner edges
            float threshold = 0.16f;
            float feather   = 0.05f;

            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                float dr = p.r - kr, dg = p.g - kg, db = p.b - kb;
                float d = Mathf.Sqrt(dr*dr + dg*dg + db*db);
                if (d < threshold) p.a = 0f;
                else if (d < threshold + feather)
                    p.a *= (d - threshold) / feather;
                pixels[i] = p;
            }

            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            dst.SetPixels(pixels);
            dst.Apply();

            System.IO.File.WriteAllBytes(DEST, dst.EncodeToPNG());
            AssetDatabase.ImportAsset(DEST, ImportAssetOptions.ForceUpdate);

            var dstImp = AssetImporter.GetAtPath(DEST) as TextureImporter;
            if (dstImp != null)
            {
                dstImp.textureType = TextureImporterType.Sprite;
                dstImp.spriteImportMode = SpriteImportMode.Single;
                dstImp.alphaIsTransparency = true;
                dstImp.mipmapEnabled = false;
                dstImp.filterMode = FilterMode.Bilinear; // crisp
                dstImp.maxTextureSize = 1024;
                dstImp.SaveAndReimport();
            }
        }
    }
}
