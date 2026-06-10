using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Picker for the 3 custom pixel-art logo variants the user designed.
    /// </summary>
    public static class SparqLogoVariants
    {
        [MenuItem("Sparq/90. Logo → Variant 1")]
        public static void V1() => Apply("Assets/Art/Sparq/sparq-logo-1.png");

        [MenuItem("Sparq/90a. Logo → Variant 2")]
        public static void V2() => Apply("Assets/Art/Sparq/sparq-logo-2.png");

        [MenuItem("Sparq/90b. Logo → Variant 3")]
        public static void V3() => Apply("Assets/Art/Sparq/sparq-logo-3.png");

        private static void Apply(string path)
        {
            // Force Sprite import
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Point; // pixel art needs Point
                imp.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq Logo",
                    $"Couldn't load:\n{path}\n\n" +
                    "Make sure the file is saved and named correctly.", "OK");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            var go = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(14f, -10f);

            // Compute size from sprite aspect ratio so the pixel art doesn't squish
            float aspect = sprite.rect.width / sprite.rect.height;
            float h = 90f;
            float w = h * aspect;
            // Cap width
            if (w > 280f) { w = 280f; h = w / aspect; }
            rt.sizeDelta = new Vector2(w, h);

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Logo",
                $"✅ Wired: {System.IO.Path.GetFileName(path)}\n\n" +
                $"Display size: {w:F0}×{h:F0}\n" +
                "Pixel art set to Point filtering (crisp pixels).\n\n" +
                "Hit ▶ Play. If size is off, tell me and I'll tune.", "OK");
        }
    }
}
