using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sparq.EditorTools
{
    /// <summary>
    /// One-time fix for the BattleOfHeroes Spine assets.
    ///
    /// Problem: skeleton.json / skeleton.atlas were imported as plain text BEFORE
    /// the Spine runtime was installed. Unity cached the wrong importer choice.
    /// Deleting the .meta files forces Unity to re-detect them — Spine's importer
    /// then takes over and generates the *_SkeletonData.asset / *_Atlas.asset files.
    /// </summary>
    public static class SparqFixSpineImports
    {
        private const string TARGET_ROOT = "Assets/BattleOfHeroes/UI/Spine";

        [MenuItem("Sparq/Fix Spine Imports (BattleOfHeroes)", priority = 50)]
        public static void Fix()
        {
            string absRoot = Path.GetFullPath(TARGET_ROOT);
            if (!Directory.Exists(absRoot))
            {
                EditorUtility.DisplayDialog("Spine Fix",
                    $"Folder not found:\n{TARGET_ROOT}", "OK");
                return;
            }

            int deleted = 0;
            // Delete .meta files for every skeleton.atlas / skeleton.json / skeleton.png
            // — and any other .atlas/.json/.png files inside the Spine root.
            string[] patterns = { "skeleton.atlas.meta", "skeleton.json.meta", "skeleton.png.meta",
                                  "*.atlas.meta", "*.json.meta" };
            foreach (var pat in patterns)
            {
                foreach (var f in Directory.GetFiles(absRoot, pat, SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(f);
                        deleted++;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[SpineFix] Couldn't delete {f}: {ex.Message}");
                    }
                }
            }

            Debug.Log($"[SpineFix] Deleted {deleted} stale .meta files. Refreshing AssetDatabase…");

            // Force AssetDatabase to rescan — Unity will regenerate .meta files
            // and pick the correct (Spine) importer for each asset.
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            EditorUtility.DisplayDialog("Spine Fix",
                $"Deleted {deleted} stale .meta files.\n\n" +
                "Unity is now re-importing the Spine assets. Wait for the import bar " +
                "(bottom-right) to clear, then check any character folder for a new " +
                "*_SkeletonData.asset file.", "OK");
        }
    }
}
