using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Builds a dense Dreamy Forest 2D Lite background — multi-layer depth.
    /// </summary>
    public static class SparqForestBackground
    {
        private const string FOLIAGE_DIR = "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage";
        private const string GRASS_BLOCK = "Assets/Dreamy Forest 2D Lite - MGLawless/Grass Block.png";

        // Simple struct to avoid long tuple syntax (which can hit compiler quirks)
        private struct LayerConfig
        {
            public string name;
            public string[] prefixes;
            public float yMin, yMax;
            public int count;
            public float sMin, sMax;
            public float alpha;
            public int sort;
            public Color tint;
        }

        [MenuItem("Sparq/80. Dense Dreamy Forest background (fills screen)")]
        public static void Build()
        {
            var old = GameObject.Find("[Forest]");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("[Forest]");
            root.transform.position = Vector3.zero;

            EnsureSprites();

            var layers = new LayerConfig[]
            {
                new LayerConfig {
                    name = "Far", prefixes = new[]{ "tree1" },
                    yMin = 1.5f, yMax = 4.5f, count = 12,
                    sMin = 0.45f, sMax = 0.65f, alpha = 0.55f, sort = -50,
                    tint = new Color(0.55f, 0.50f, 0.85f) },
                new LayerConfig {
                    name = "Mid", prefixes = new[]{ "tree1", "bush1", "bush2 P" },
                    yMin = -1.0f, yMax = 3.0f, count = 14,
                    sMin = 0.65f, sMax = 0.95f, alpha = 0.85f, sort = -25,
                    tint = new Color(0.85f, 0.85f, 1.0f) },
                new LayerConfig {
                    name = "Near", prefixes = new[]{ "bush1", "bush2", "grass1" },
                    yMin = -2.5f, yMax = 0.0f, count = 16,
                    sMin = 0.85f, sMax = 1.15f, alpha = 1.0f, sort = 10,
                    tint = Color.white },
                new LayerConfig {
                    name = "Front", prefixes = new[]{ "grass1", "grass2", "grass1 P" },
                    yMin = -3.6f, yMax = -2.2f, count = 22,
                    sMin = 0.85f, sMax = 1.25f, alpha = 1.0f, sort = 25,
                    tint = Color.white },
            };

            foreach (var L in layers)
            {
                var layerGO = new GameObject($"Layer_{L.name}");
                layerGO.transform.SetParent(root.transform, false);
                layerGO.AddComponent<Sparq.Cinematic.ParallaxLayer>();

                var sprites = LoadSprites(L.prefixes);
                if (sprites.Count == 0) continue;

                for (int i = 0; i < L.count; i++)
                {
                    float x = Mathf.Lerp(-6f, 6f, (i + 0.5f + Random.Range(-0.4f, 0.4f)) / L.count);
                    float y = Random.Range(L.yMin, L.yMax);
                    float scale = Random.Range(L.sMin, L.sMax);
                    bool flipX = Random.value < 0.5f;
                    var sp = sprites[Random.Range(0, sprites.Count)];
                    var go = new GameObject(sp.name);
                    go.transform.SetParent(layerGO.transform, false);
                    go.transform.localPosition = new Vector3(x, y, 0);
                    go.transform.localScale = new Vector3(scale * (flipX ? -1 : 1), scale, 1);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sp;
                    sr.sortingOrder = L.sort + Random.Range(0, 3);
                    var c = L.tint; c.a = L.alpha; sr.color = c;
                }
            }

            // Ground tile carpet
            var groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GRASS_BLOCK);
            if (groundSprite != null)
            {
                var ground = new GameObject("Ground");
                ground.transform.SetParent(root.transform, false);
                for (int x = -7; x <= 7; x++)
                {
                    var tile = new GameObject($"GroundTile_{x}");
                    tile.transform.SetParent(ground.transform, false);
                    tile.transform.localPosition = new Vector3(x, -3.6f, 0);
                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = groundSprite;
                    sr.sortingOrder = 18;
                }
            }

            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, -1.0f, 0f);
            }
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(1.4f, -1.4f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Forest",
                "✅ Dense Dreamy Forest background built.\n\n" +
                "Re-run to randomize.\n\nHit ▶ Play.", "OK");
        }

        private static List<Sprite> LoadSprites(string[] prefixes)
        {
            var list = new List<Sprite>();
            if (!Directory.Exists(FOLIAGE_DIR)) return list;
            foreach (var f in Directory.GetFiles(FOLIAGE_DIR, "*.png"))
            {
                string ap = f.Replace('\\', '/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);
                string name = Path.GetFileNameWithoutExtension(ap);
                bool match = false;
                foreach (var p in prefixes)
                {
                    if (name.StartsWith(p, System.StringComparison.OrdinalIgnoreCase))
                    { match = true; break; }
                }
                if (!match) continue;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ap);
                if (sp != null) list.Add(sp);
            }
            return list;
        }

        private static void EnsureSprites()
        {
            bool changed = false;
            if (!Directory.Exists(FOLIAGE_DIR)) return;
            foreach (var f in Directory.GetFiles(FOLIAGE_DIR, "*.png"))
            {
                string ap = f.Replace('\\','/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);
                var imp = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    changed = true;
                }
            }
            var gImp = AssetImporter.GetAtPath(GRASS_BLOCK) as TextureImporter;
            if (gImp != null && gImp.textureType != TextureImporterType.Sprite)
            {
                gImp.textureType = TextureImporterType.Sprite;
                gImp.spriteImportMode = SpriteImportMode.Single;
                gImp.SaveAndReimport();
                changed = true;
            }
            if (changed) AssetDatabase.Refresh();
        }
    }
}
