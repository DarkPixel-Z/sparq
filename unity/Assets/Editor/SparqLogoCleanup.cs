using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqLogoCleanup
    {
        [MenuItem("Sparq/129. Logo - remove plate, leave only image")]
        public static void Apply()
        {
            var title = GameObject.Find("GameTitle");
            if (title == null)
            {
                EditorUtility.DisplayDialog("Sparq", "GameTitle not found.", "OK");
                return;
            }

            // Find the actual logo sprite in the hierarchy (or use the GameTitle's Image)
            Sprite logoSprite = null;

            // Prefer the user's custom logo
            string[] candidates = {
                "Assets/Art/Sparq/sparq-logo.png",
                "Assets/Art/Sparq/logo-sparq.png",
            };
            foreach (var p in candidates)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) { logoSprite = s; break; }
            }
            if (logoSprite == null)
            {
                // Fall back to whatever sprite is currently on a child Image
                foreach (var img in title.GetComponentsInChildren<Image>(true))
                {
                    if (img.sprite != null && img.sprite.name.ToLower().Contains("logo"))
                    {
                        logoSprite = img.sprite;
                        break;
                    }
                }
            }

            // Wipe all children (purple plate, decorations, buttons)
            for (int i = title.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(title.transform.GetChild(i).gameObject);

            // Strip all components on the GameTitle root EXCEPT RectTransform (and CanvasRenderer if needed)
            foreach (var g in title.GetComponents<Graphic>())
                Object.DestroyImmediate(g);
            foreach (var c in title.GetComponents<Component>())
            {
                if (c is RectTransform || c is Transform) continue;
                if (c is CanvasRenderer) continue;
                Object.DestroyImmediate(c);
            }

            // Add a single Image with the logo, fully transparent background
            var img2 = title.AddComponent<Image>();
            img2.sprite = logoSprite;
            img2.preserveAspect = true;
            img2.raycastTarget = false;
            img2.color = Color.white;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Logo cleaned up.\n\n" +
                "• Purple plate removed\n" +
                "• Buttons/decorations removed\n" +
                "• Only the logo image remains\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
