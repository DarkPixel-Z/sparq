using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Sparq.Editor
{
    /// <summary>
    /// Reverts the over-dense forest from #80 back to the lighter parallax forest from #27.
    /// </summary>
    public static class SparqRevertForest
    {
        private const string FOLIAGE_DIR = "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage";

        [MenuItem("Sparq/81. REVERT forest (lighter parallax, like #27)")]
        public static void Revert()
        {
            // Wipe the dense forest
            var old = GameObject.Find("[Forest]");
            if (old != null) Object.DestroyImmediate(old);

            // Build a lighter, sparser parallax forest
            var root = new GameObject("[Forest]");
            root.transform.position = Vector3.zero;

            string[] foliage = {
                "tree1", "bush1", "bush2", "grass1"
            };

            // 3 sparse layers
            BuildLayer(root.transform, "Far",  -40, -1.2f, 0.4f, 6,  0.45f, 0.65f, 0.6f,  new Color(0.6f, 0.55f, 0.85f), foliage);
            BuildLayer(root.transform, "Mid",  -20, -2.4f, 0.6f, 8,  0.7f,  1.0f,  0.85f, new Color(0.85f, 0.85f, 1.0f), foliage);
            BuildLayer(root.transform, "Near", 20,  -3.6f, 0.4f, 10, 0.8f,  1.2f,  1.0f,  Color.white, foliage);

            // Reset Karu position
            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, -0.6f, 0f);
            }
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.position = new Vector3(1.4f, -1.0f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Reverted to lighter parallax forest.\n\n" +
                "• 3 layers (far/mid/near)\n" +
                "• ~24 sprites total (was 64+)\n" +
                "• Karu repositioned at center-bottom\n" +
                "• No more grass-overload\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void BuildLayer(Transform parent, string name, int sort, float yMid, float yJit,
                                       int count, float sMin, float sMax, float alpha, Color tint, string[] prefixes)
        {
            var layer = new GameObject($"Layer_{name}");
            layer.transform.SetParent(parent, false);
            layer.AddComponent<Sparq.Cinematic.ParallaxLayer>();

            var sprites = LoadSprites(prefixes);
            if (sprites.Count == 0) return;

            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(-5.5f, 5.5f, (i + 0.5f + Random.Range(-0.3f, 0.3f)) / count);
                float y = yMid + Random.Range(-yJit, yJit);
                float scale = Random.Range(sMin, sMax);
                bool flipX = Random.value < 0.5f;
                var sp = sprites[Random.Range(0, sprites.Count)];
                var go = new GameObject(sp.name);
                go.transform.SetParent(layer.transform, false);
                go.transform.localPosition = new Vector3(x, y, 0);
                go.transform.localScale = new Vector3(scale * (flipX ? -1 : 1), scale, 1);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = sort;
                var c = tint; c.a = alpha; sr.color = c;
            }
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
                string n = Path.GetFileNameWithoutExtension(ap);
                bool match = false;
                foreach (var p in prefixes)
                {
                    if (n.StartsWith(p, System.StringComparison.OrdinalIgnoreCase)) { match = true; break; }
                }
                if (!match) continue;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ap);
                if (sp != null) list.Add(sp);
            }
            return list;
        }
    }
}
