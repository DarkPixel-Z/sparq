using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces the static Hellhound portrait with an ANIMATED slime/monster
    /// from the Fantasy Monster Pack. Loads idle frames + applies UISpriteAnimator.
    /// </summary>
    public static class SparqAnimatedRival
    {
        private const string ROOT = "Assets/Fantasy Monster Pack 5 Handcrafted 2D Creatures";

        [MenuItem("Sparq/28. Animate Volt portrait (slime monster)")]
        public static void AnimateVolt() => AnimateForMonster("monster1");

        [MenuItem("Sparq/28a. Animate Volt with monster2")]
        public static void Animate2() => AnimateForMonster("monster2");

        [MenuItem("Sparq/28b. Animate Volt with monster3")]
        public static void Animate3() => AnimateForMonster("monster3");

        [MenuItem("Sparq/28c. Animate Volt with monster4")]
        public static void Animate4() => AnimateForMonster("monster4");

        [MenuItem("Sparq/28d. Animate Volt with monster5")]
        public static void Animate5() => AnimateForMonster("monster5");

        private static void AnimateForMonster(string monsterFolder)
        {
            string idleDir = $"{ROOT}/{monsterFolder}/idel";
            if (!Directory.Exists(idleDir))
            {
                EditorUtility.DisplayDialog("Sparq Rival",
                    $"No idle folder found at:\n{idleDir}", "OK");
                return;
            }

            // Find all PNG frames in the idle folder, ensure they're imported as Sprites
            EnsureSpriteImport(idleDir);
            var frames = LoadFramesSorted(idleDir);
            if (frames.Count == 0)
            {
                EditorUtility.DisplayDialog("Sparq Rival",
                    $"No sprite frames found in:\n{idleDir}", "OK");
                return;
            }

            var portrait = GameObject.Find("VoltPortrait");
            if (portrait == null)
            {
                EditorUtility.DisplayDialog("Sparq Rival",
                    "VoltPortrait not found. Run Sparq → 12 first to add the rival card.", "OK");
                return;
            }

            var img = portrait.GetComponent<Image>();
            if (img == null) img = portrait.AddComponent<Image>();

            // Set first frame
            img.sprite = frames[0];
            img.preserveAspect = true;
            img.color = Color.white;

            // Attach animator
            var anim = portrait.GetComponent<Sparq.UI.UISpriteAnimator>();
            if (anim == null) anim = portrait.AddComponent<Sparq.UI.UISpriteAnimator>();
            anim.SetFrames(frames.ToArray(), 8f);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq Rival] {monsterFolder} animated on VoltPortrait — {frames.Count} idle frames at 8 FPS.");
            EditorUtility.DisplayDialog("Sparq Rival",
                $"✅ Volt is now animated!\n\n" +
                $"• Source: {monsterFolder}/idel ({frames.Count} frames)\n" +
                $"• Loops at 8 FPS\n\n" +
                $"Hit ▶ Play — Volt will breathe + idle.\n\n" +
                $"Try menus 28a-28d to swap to a different creature.", "OK");
        }

        private static List<Sprite> LoadFramesSorted(string dir)
        {
            var list = new List<(string path, Sprite sprite)>();
            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                string ap = f.Replace('\\', '/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(ap);
                if (s != null) list.Add((ap, s));
            }
            list.Sort((a, b) => string.Compare(a.path, b.path, System.StringComparison.OrdinalIgnoreCase));
            var result = new List<Sprite>();
            foreach (var t in list) result.Add(t.sprite);
            return result;
        }

        private static void EnsureSpriteImport(string dir)
        {
            bool changed = false;
            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                string ap = f.Replace('\\', '/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);

                var imp = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = false;
                    imp.SaveAndReimport();
                    changed = true;
                }
            }
            if (changed) AssetDatabase.Refresh();
        }
    }
}
