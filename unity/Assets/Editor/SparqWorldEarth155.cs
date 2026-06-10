using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 155: Swap WORLD top-button icon from chat bubble → Earth globe.
    /// </summary>
    public static class SparqWorldEarth155
    {
        private const string EARTH_PATH =
            "Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/IconMisc/Icon_Earth.png";

        [MenuItem("Sparq/155. WORLD button → Earth globe icon")]
        public static void Apply()
        {
            // Ensure sprite import settings
            var imp = AssetImporter.GetAtPath(EARTH_PATH) as TextureImporter;
            if (imp == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Earth icon missing:\n{EARTH_PATH}", "OK");
                return;
            }
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(EARTH_PATH);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Failed to load Earth sprite.", "OK");
                return;
            }

            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null) { EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK"); return; }

            Transform world = null;
            for (int i = 0; i < bar.transform.childCount; i++)
            {
                var c = bar.transform.GetChild(i);
                if (c.name.ToLower().Contains("world")) { world = c; break; }
            }
            if (world == null)
            {
                EditorUtility.DisplayDialog("Sparq", "WorldBtn not found. Run #143 first.", "OK");
                return;
            }

            var iconTr = world.Find("Icon");
            if (iconTr == null) return;
            var img = iconTr.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.preserveAspect = true;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ WORLD button now uses the Earth globe icon.\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
