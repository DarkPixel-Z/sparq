using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>
    /// One-time prepper that walks the 2D Fantasy Monster Sprite Pack and ensures
    /// every PNG is imported as a Sprite with alpha transparency. Without this,
    /// PetPanel can't display pet images at runtime (LoadAssetAtPath<Sprite> = null).
    /// Runs automatically on script reload, also exposed as a manual menu item.
    /// </summary>
    [InitializeOnLoad]
    public static class SparqMonsterSpritePrepper
    {
        private const string ROOT = "Assets/2D Fantasy Monster Sprite Pack/Monsters";
        private const string MARKER = "Sparq.MonsterSpritePrepper.v1";

        static SparqMonsterSpritePrepper()
        {
            if (SessionState.GetBool(MARKER, false)) return;
            SessionState.SetBool(MARKER, true);
            EditorApplication.delayCall += PrepAll;
        }

        [MenuItem("Sparq/205. Prep monster sprites for runtime")]
        public static void PrepAllManual() => PrepAll();

        private static void PrepAll()
        {
            int touched = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ROOT });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                bool changed = false;
                if (imp.textureType != TextureImporterType.Sprite)
                { imp.textureType = TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency)
                { imp.alphaIsTransparency = true; changed = true; }
                if (imp.spriteImportMode != SpriteImportMode.Single)
                { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
                if (changed) { imp.SaveAndReimport(); touched++; }
            }
            if (touched > 0)
                Debug.Log($"[Sparq] Prepped {touched} monster sprite(s) for runtime.");
        }
    }
}
