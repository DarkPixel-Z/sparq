using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Sparq.Editor
{
    /// <summary>
    /// Drops Dreamy Forest 2D Lite trees/bushes/grass into the home scene,
    /// arranged in 3 parallax layers (far / mid / near). Replaces our flat
    /// purple background with a real painted forest.
    /// </summary>
    public static class SparqDreamyForest
    {
        private const string FOLIAGE_DIR = "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage";

        // Layer config: (layerName, sortingOrder, yMin, yMax, xCount, scaleMin, scaleMax, alpha, prefixes)
        private static readonly (string name, int sort, float y, float yJit, int count, float sMin, float sMax, float alpha, string[] prefixes)[] LAYERS = new[]
        {
            // Far: small distant trees, blueish tint, low opacity
            ("ForestFar",  -40, -1.2f, 0.4f, 6,  0.45f, 0.65f, 0.6f, new[] { "tree1", "bush2 P" }),
            // Mid: bushes + trees, normal
            ("ForestMid",  -20, -2.4f, 0.6f, 8,  0.7f,  1.0f,  0.85f, new[] { "tree1", "bush1", "bush2", "grass1 P" }),
            // Near: foreground grass + bushes, biggest, in front of characters bottom
            ("ForestNear",  20, -3.6f, 0.4f, 10, 0.8f,  1.2f,  1.0f,  new[] { "grass1", "grass2", "bush1 1", "bush1 2" }),
        };

        [MenuItem("Sparq/27. Dreamy Forest parallax background")]
        public static void Build()
        {
            // Pre-import: ensure all foliage PNGs are imported as Sprites
            EnsureSpriteImport();

            // Remove any previous forest pass
            var old = GameObject.Find("[Forest]");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("[Forest]");
            root.transform.position = Vector3.zero;

            int totalSprites = 0;
            foreach (var L in LAYERS)
            {
                var layerGO = new GameObject(L.name);
                layerGO.transform.SetParent(root.transform, false);

                // Attach parallax drift
                var px = layerGO.AddComponent<Sparq.Cinematic.ParallaxLayer>();
                // Far drifts slow, near drifts fast
                px.driftSpeed = (L.sort < 0) ? 0.18f : 0.35f;
                px.swayAmplitude = (L.sort < 0) ? 0.02f : 0.05f;

                var sprites = LoadSprites(L.prefixes);
                if (sprites.Count == 0)
                {
                    Debug.LogWarning($"[DreamyForest] No sprites matched for layer {L.name}.");
                    continue;
                }

                // Spread N sprites across screen width [-5..5]
                for (int i = 0; i < L.count; i++)
                {
                    float x = Mathf.Lerp(-5.5f, 5.5f, (i + 0.5f + Random.Range(-0.3f, 0.3f)) / L.count);
                    float y = L.y + Random.Range(-L.yJit, L.yJit);
                    float scale = Random.Range(L.sMin, L.sMax);
                    float flipX = Random.value < 0.5f ? -1f : 1f;

                    var spr = sprites[Random.Range(0, sprites.Count)];
                    var go = new GameObject(spr.name);
                    go.transform.SetParent(layerGO.transform, false);
                    go.transform.localPosition = new Vector3(x, y, 0f);
                    go.transform.localScale = new Vector3(scale * flipX, scale, 1f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = spr;
                    sr.sortingOrder = L.sort;
                    // Tint: far is dimmer / cooler
                    if (L.sort < -30)
                        sr.color = new Color(0.6f, 0.55f, 0.85f, L.alpha); // far cool tint
                    else if (L.sort < 0)
                        sr.color = new Color(0.85f, 0.85f, 1.0f, L.alpha);
                    else
                        sr.color = new Color(1f, 1f, 1f, L.alpha);
                    totalSprites++;
                }
            }

            // Make sure Karu / Hellhound render in front of mid, behind near
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 5; // between Mid (-20) and Near (20)
                karu.transform.localScale = Vector3.one * 1.4f; // make Karu the hero
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[DreamyForest] Placed {totalSprites} foliage sprites across 3 parallax layers.");
            EditorUtility.DisplayDialog("Sparq Forest",
                $"✅ Dreamy Forest applied.\n\n" +
                $"• {totalSprites} sprites across 3 layers\n" +
                $"• Far layer: cool blue tint, slow drift\n" +
                $"• Mid layer: normal trees + bushes\n" +
                $"• Near layer: foreground grass (in front of mid)\n" +
                $"• Karu scaled up 1.4x to read as hero\n\n" +
                "Hit ▶ Play. Re-run this menu to randomize the layout.", "OK");
        }

        private static List<Sprite> LoadSprites(string[] prefixes)
        {
            var list = new List<Sprite>();
            var files = System.IO.Directory.GetFiles(FOLIAGE_DIR, "*.png");
            foreach (var path in files)
            {
                string assetPath = path.Replace('\\', '/');
                string fname = System.IO.Path.GetFileNameWithoutExtension(assetPath);

                bool match = false;
                foreach (var p in prefixes)
                {
                    if (fname.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                    {
                        match = true; break;
                    }
                }
                if (!match) continue;

                int idx = assetPath.IndexOf("Assets/");
                if (idx >= 0) assetPath = assetPath.Substring(idx);

                var s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (s != null) list.Add(s);
            }
            return list;
        }

        private static void EnsureSpriteImport()
        {
            if (!System.IO.Directory.Exists(FOLIAGE_DIR)) return;
            var files = System.IO.Directory.GetFiles(FOLIAGE_DIR, "*.png");
            bool changed = false;
            foreach (var f in files)
            {
                string assetPath = f.Replace('\\', '/');
                int idx = assetPath.IndexOf("Assets/");
                if (idx >= 0) assetPath = assetPath.Substring(idx);

                var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
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
