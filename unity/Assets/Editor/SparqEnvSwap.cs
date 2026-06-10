using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 132: Swap the home screen environment.
    /// Includes the fantasy backgrounds from Layer Lab GUI Pro-FantasyHero
    /// (single full-screen images — clean and proper RPG vibe) plus the
    /// multi-layer parallax forest pack as alternates.
    /// </summary>
    public static class SparqEnvSwap
    {
        private const string FH_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/";

        // Fantasy backgrounds (full-screen single image, much cleaner)
        [MenuItem("Sparq/132. Environment → Fantasy Hero #1 (castle / sky)")]
        public static void Fh1() => ApplyFantasy(FH_DIR + "Background_01.png", "Fantasy #1");
        [MenuItem("Sparq/132a. Environment → Fantasy Hero #2")]
        public static void Fh2() => ApplyFantasy(FH_DIR + "Background_02.png", "Fantasy #2");
        [MenuItem("Sparq/132b. Environment → Fantasy Hero #3")]
        public static void Fh3() => ApplyFantasy(FH_DIR + "Background_03.png", "Fantasy #3");
        [MenuItem("Sparq/132c. Environment → Fantasy Hero #4")]
        public static void Fh4() => ApplyFantasy(FH_DIR + "Background_04.png", "Fantasy #4");
        [MenuItem("Sparq/132d. Environment → Fantasy Hero #5")]
        public static void Fh5() => ApplyFantasy(FH_DIR + "Background_05.png", "Fantasy #5");

        // Multi-layer forest packs (kept as fallback)
        [MenuItem("Sparq/132e. Environment → Autumn forest (parallax)")]
        public static void Autumn() => ApplyPrefab(
            "Assets/Background for mobile games, forest/Prefabs/Background, autumn.prefab", "Autumn forest");
        [MenuItem("Sparq/132f. Environment → Summer forest (parallax)")]
        public static void Summer() => ApplyPrefab(
            "Assets/Background for mobile games, forest/Prefabs/Background, summer.prefab", "Summer forest");
        [MenuItem("Sparq/132g. Environment → Winter forest (parallax)")]
        public static void Winter() => ApplyPrefab(
            "Assets/Background for mobile games, forest/Prefabs/Background, winter.prefab", "Winter forest");

        // ───────────────────── Single-image fantasy bg ─────────────────────
        private static void ApplyFantasy(string path, string label)
        {
            EnsureSprite(path);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Sprite not found:\n{path}", "OK");
                return;
            }

            ClearOldEnv();
            DisableExisting();

            var go = new GameObject("SparqEnv");
            go.transform.position = new Vector3(0, 0, 5f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;

            // Scale to fill camera
            var cam = Camera.main;
            if (cam != null)
            {
                float orthoH = cam.orthographicSize * 2f;
                float orthoW = orthoH * cam.aspect;
                float spriteH = sprite.bounds.size.y;
                float spriteW = sprite.bounds.size.x;
                float kx = orthoW / spriteW;
                float ky = orthoH / spriteH;
                float k  = Mathf.Max(kx, ky); // cover
                go.transform.localScale = new Vector3(k, k, 1f);
            }
            else
            {
                go.transform.localScale = Vector3.one * 1.5f;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Environment → {label}\n\n" +
                "• Single full-screen fantasy image\n" +
                "• Old forest disabled\n\n" +
                "Try other variants: 132 / 132a-d (fantasy), 132e-g (parallax forest).", "OK");
        }

        // ───────────────────── Multi-layer forest prefab ─────────────────────
        private static void ApplyPrefab(string prefabPath, string label)
        {
            var pfx = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pfx == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Prefab missing:\n{prefabPath}", "OK");
                return;
            }
            ClearOldEnv();
            DisableExisting();

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfx);
            inst.name = "SparqEnv";
            inst.transform.position = new Vector3(0, 0, 5f);

            var cam = Camera.main;
            float orthoH = cam != null ? cam.orthographicSize * 2f : 10f;
            float spriteH = 0f;
            foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sprite == null) continue;
                float h = sr.sprite.bounds.size.y;
                if (h > spriteH) spriteH = h;
            }
            if (spriteH > 0.01f)
            {
                float k = orthoH / spriteH;
                inst.transform.localScale = new Vector3(k, k, 1f);
            }
            foreach (var sr in inst.GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder -= 100;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Environment → {label}\n\nIf only sky shows, try the Fantasy Hero variants (132 / 132a–d).", "OK");
        }

        // ───────────────────── Helpers ─────────────────────
        private static void ClearOldEnv()
        {
            // Remove ALL previous SparqEnv instances (fixes duplicate bug)
            var all = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var o in all)
                if (o != null && o.name == "SparqEnv") Object.DestroyImmediate(o);
        }

        private static void DisableExisting()
        {
            string[] names = { "Forest", "[Forest]", "DreamyForest", "JungleParallax" };
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) go.SetActive(false);
            }
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }
}
