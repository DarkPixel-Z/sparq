using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Converts .otf fonts from 20 Logos PSD/Fonts to TMP font assets,
    /// then applies each one to the SPARQ logo so you can preview.
    /// </summary>
    public static class SparqFontPicker
    {
        private static readonly (string menu, string otfPath)[] FONTS = new[]
        {
            ("Sparq/60. Logo font → ChunkFive (heavy slab)",
             "Assets/20 Logos PSD/Fonts/chunkfiveex/Chunkfive-Regular.otf"),
            ("Sparq/60a. Logo font → Molot (industrial heavy)",
             "Assets/20 Logos PSD/Fonts/molot/Molot.otf"),
            ("Sparq/60b. Logo font → PeaceSans (playful round)",
             "Assets/20 Logos PSD/Fonts/peacesans/peace_sans-webfont.otf"),
            ("Sparq/60c. Logo font → KaushanScript (handwritten)",
             "Assets/20 Logos PSD/Fonts/kaushanscript/KaushanScript-Regular.otf"),
            ("Sparq/60d. Logo font → FiraSans Heavy (modern bold)",
             "Assets/20 Logos PSD/Fonts/firasansheavy/FiraSans-Heavy.otf"),
            ("Sparq/60e. Logo font → Gilam (geometric)",
             "Assets/20 Logos PSD/Fonts/gilam/Gilam-Heavy.otf"),
        };

        [MenuItem("Sparq/60. Logo font → ChunkFive (heavy slab)")]    public static void F0() => Apply(0);
        [MenuItem("Sparq/60a. Logo font → Molot (industrial heavy)")] public static void F1() => Apply(1);
        [MenuItem("Sparq/60b. Logo font → PeaceSans (playful round)")] public static void F2() => Apply(2);
        [MenuItem("Sparq/60c. Logo font → KaushanScript (handwritten)")] public static void F3() => Apply(3);
        [MenuItem("Sparq/60d. Logo font → FiraSans Heavy (modern bold)")] public static void F4() => Apply(4);
        [MenuItem("Sparq/60e. Logo font → Gilam (geometric)")]         public static void F5() => Apply(5);

        private static void Apply(int idx)
        {
            var entry = FONTS[idx];

            // Find the .otf — may have a slightly different filename. Search in directory.
            string dir = Path.GetDirectoryName(entry.otfPath);
            string otfPath = entry.otfPath;
            if (!File.Exists(otfPath))
            {
                if (Directory.Exists(dir))
                {
                    var found = Directory.GetFiles(dir, "*.otf");
                    if (found.Length == 0) found = Directory.GetFiles(dir, "*.ttf");
                    if (found.Length > 0)
                    {
                        otfPath = found[0].Replace('\\','/');
                        int aIdx = otfPath.IndexOf("Assets/");
                        if (aIdx >= 0) otfPath = otfPath.Substring(aIdx);
                    }
                }
            }
            if (!File.Exists(otfPath))
            {
                EditorUtility.DisplayDialog("Sparq Fonts",
                    $"Font file not found in {dir}.\n\n" +
                    "Make sure 20 Logos PSD is fully imported.", "OK");
                return;
            }

            // Load the source font asset
            var srcFont = AssetDatabase.LoadAssetAtPath<Font>(otfPath);
            if (srcFont == null)
            {
                EditorUtility.DisplayDialog("Sparq Fonts",
                    $"Couldn't load Font from {otfPath}.\n\n" +
                    "Try right-clicking the .otf file in Project → Create → TextMeshPro → Font Asset, then re-run this menu.",
                    "OK");
                return;
            }

            // Look for an existing SDF asset next to it
            string sdfPath = otfPath.Replace(".otf", " SDF.asset").Replace(".ttf", " SDF.asset");
            var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
            if (tmpFont == null)
            {
                // Try to create a basic TMP font asset on the fly
                tmpFont = TMP_FontAsset.CreateFontAsset(srcFont);
                if (tmpFont == null)
                {
                    EditorUtility.DisplayDialog("Sparq Fonts",
                        "Couldn't auto-create a TMP font asset.\n\n" +
                        "Right-click the .otf file:\n" +
                        $"  {otfPath}\n\n" +
                        "Then: Create → TextMeshPro → Font Asset\n\n" +
                        "Then re-run this menu.", "OK");
                    return;
                }
                AssetDatabase.CreateAsset(tmpFont, sdfPath);
                AssetDatabase.SaveAssets();
            }

            // Apply font to all TMP texts in the GameTitle
            var title = GameObject.Find("GameTitle");
            if (title == null)
            {
                EditorUtility.DisplayDialog("Sparq Fonts",
                    "GameTitle not in scene. Run Sparq → 53 first.", "OK");
                return;
            }

            int applied = 0;
            foreach (var tmp in title.GetComponentsInChildren<TMP_Text>(true))
            {
                if (tmp == null) continue;
                tmp.font = tmpFont;
                applied++;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Fonts",
                $"✅ Applied font: {Path.GetFileNameWithoutExtension(otfPath)}\n\n" +
                $"Updated {applied} TMP text(s) in the logo.\n\n" +
                "Hit ▶ Play. Try other fonts via Sparq → 60a-60e to compare.", "OK");
        }
    }
}
