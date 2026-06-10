using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace Sparq.Editor
{
    public static class SparqLogoChromaKey
    {
        private const string SRC  = "Assets/Art/Sparq/sparq-logo.png";
        private const string DEST = "Assets/Art/Sparq/sparq-logo-transparent.png";

        [MenuItem("Sparq/130. Logo - chroma-key purple to transparent")]
        public static void Apply()
        {
            // Make sure source is readable + sprite
            var srcImp = AssetImporter.GetAtPath(SRC) as TextureImporter;
            if (srcImp == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Logo not found at {SRC}", "OK");
                return;
            }
            bool needReimport = false;
            if (!srcImp.isReadable) { srcImp.isReadable = true; needReimport = true; }
            if (srcImp.textureCompression != TextureImporterCompression.Uncompressed)
            { srcImp.textureCompression = TextureImporterCompression.Uncompressed; needReimport = true; }
            if (needReimport) srcImp.SaveAndReimport();

            var src = AssetDatabase.LoadAssetAtPath<Texture2D>(SRC);
            if (src == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Failed to load source texture.", "OK");
                return;
            }

            // Sample the top-left corner as the "background purple" key
            Color key = src.GetPixel(2, src.height - 3);

            // Build new texture with chroma-keyed alpha
            var w = src.width; var h = src.height;
            var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = src.GetPixels();
            float threshold = 0.18f;       // distance below which pixel is keyed
            float feather   = 0.08f;       // distance over which alpha fades
            for (int i = 0; i < pixels.Length; i++)
            {
                Color p = pixels[i];
                float dr = p.r - key.r, dg = p.g - key.g, db = p.b - key.b;
                float d = Mathf.Sqrt(dr*dr + dg*dg + db*db);
                if (d < threshold) p.a = 0f;
                else if (d < threshold + feather)
                    p.a *= (d - threshold) / feather;
                pixels[i] = p;
            }
            dst.SetPixels(pixels);
            dst.Apply();

            // Write PNG
            File.WriteAllBytes(DEST, dst.EncodeToPNG());
            AssetDatabase.ImportAsset(DEST, ImportAssetOptions.ForceUpdate);

            // Configure as Sprite
            var dstImp = AssetImporter.GetAtPath(DEST) as TextureImporter;
            if (dstImp != null)
            {
                dstImp.textureType = TextureImporterType.Sprite;
                dstImp.spriteImportMode = SpriteImportMode.Single;
                dstImp.alphaIsTransparency = true;
                dstImp.mipmapEnabled = false;
                dstImp.SaveAndReimport();
            }

            // Apply to GameTitle
            var title = GameObject.Find("GameTitle");
            if (title != null)
            {
                // Clean children + extra components first
                for (int i = title.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(title.transform.GetChild(i).gameObject);
                // Remove Graphic/Image components first (so CanvasRenderer is no longer required)
                foreach (var g in title.GetComponents<Graphic>())
                    Object.DestroyImmediate(g);
                // Then strip everything else except RectTransform & CanvasRenderer
                foreach (var c in title.GetComponents<Component>())
                {
                    if (c is RectTransform || c is Transform) continue;
                    if (c is CanvasRenderer) continue;
                    Object.DestroyImmediate(c);
                }
                var img = title.AddComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DEST);
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = Color.white;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Chroma-key applied.\n\n" +
                $"• Sampled key color: RGB({key.r:F2},{key.g:F2},{key.b:F2})\n" +
                $"• Saved: {DEST}\n" +
                "• Threshold: 0.18, feather: 0.08\n\n" +
                "If edges look rough or some purple remains, run again — I'll tune thresholds.", "OK");
        }
    }
}
