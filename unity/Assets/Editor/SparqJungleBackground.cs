using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces the Dreamy Forest with a painted Jungle parallax background
    /// from the 2D Fantasy Platformer Pack — far better match for the chibi heroes.
    /// 6 layers (sky → mountain → trees → ground), each at different parallax speeds.
    /// </summary>
    public static class SparqJungleBackground
    {
        private const string JUNGLE_DIR = "Assets/2D Fantasy Platformer Pack/Backgrounds/Jungle";

        [MenuItem("Sparq/89. JUNGLE parallax background (replaces forest)")]
        public static void Build()
        {
            EnsureSprites();

            // Wipe Dreamy forest, leave everything else
            var oldForest = GameObject.Find("[Forest]");
            if (oldForest != null) Object.DestroyImmediate(oldForest);

            var camGO = Camera.main;
            if (camGO == null) return;
            var cam = camGO;

            // Build new root
            var root = new GameObject("[Forest]"); // keep same name so other scripts find it
            root.transform.position = Vector3.zero;

            // Background layers 0-5 (0 = farthest sky, 5 = closest ground)
            // Each layer: full-screen Image stacked in depth via sortingOrder
            float orthoSize = cam.orthographicSize;
            float aspect = cam.aspect;
            float worldHeight = orthoSize * 2f;
            float worldWidth = worldHeight * aspect;

            for (int i = 0; i <= 5; i++)
            {
                string path = $"{JUNGLE_DIR}/{i}.png";
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null) continue;

                var go = new GameObject($"Jungle_{i}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = new Vector3(0, 0, 0);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = -100 + i * 10;  // 0=back, 5=front
                sr.color = Color.white;

                // Scale to fit camera view
                float spriteH = sp.bounds.size.y;
                float spriteW = sp.bounds.size.x;
                float scaleY = worldHeight / spriteH;
                float scaleX = worldWidth / spriteW;
                float scale = Mathf.Max(scaleX, scaleY) * 1.05f; // slight overfill so edges don't show
                go.transform.localScale = new Vector3(scale, scale, 1);

                // Subtle parallax (front layers drift faster than far)
                var px = go.AddComponent<Sparq.Cinematic.ParallaxLayer>();
                // Public fields exist — set via SerializedObject
                var so = new SerializedObject(px);
                var driftProp = so.FindProperty("driftSpeed");
                if (driftProp != null) driftProp.floatValue = 0.05f + i * 0.03f;
                so.ApplyModifiedProperties();
            }

            // Add clouds floating in front of mid layers
            var cloudPath = $"{JUNGLE_DIR}/clouds.png";
            var cloudSp = AssetDatabase.LoadAssetAtPath<Sprite>(cloudPath);
            if (cloudSp != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var c = new GameObject($"Cloud_{i}");
                    c.transform.SetParent(root.transform, false);
                    c.transform.localPosition = new Vector3(Random.Range(-4f, 4f), Random.Range(2f, 4f), 0);
                    c.transform.localScale = Vector3.one * Random.Range(0.4f, 0.7f);
                    var sr = c.AddComponent<SpriteRenderer>();
                    sr.sprite = cloudSp;
                    sr.sortingOrder = -50;
                    sr.color = new Color(1f, 1f, 1f, 0.85f);
                    c.AddComponent<Sparq.Cinematic.ParallaxLayer>();
                }
            }

            // Now scale Karu + Mochi BIG to match the painted environment
            var karu = GameObject.Find("Karu");
            if (karu != null)
            {
                karu.transform.localScale = Vector3.one * 0.45f;
                karu.transform.position = new Vector3(0f, -1.0f, 0f);
                var sr = karu.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 50;
            }
            var mochi = GameObject.Find("Mochi");
            if (mochi != null)
            {
                mochi.transform.localScale = Vector3.one * 0.40f;  // very close to Karu's size now
                mochi.transform.position = new Vector3(2.2f, -1.4f, 0f);
                var msr = mochi.GetComponent<SpriteRenderer>();
                if (msr != null) msr.sortingOrder = 49;
            }
            var squad = GameObject.Find("[HeroSquad]");
            if (squad != null)
            {
                foreach (Transform t in squad.transform)
                {
                    t.localScale = Vector3.one * 0.30f;
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 48;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Jungle",
                "✅ Jungle parallax background replaces forest.\n\n" +
                "• 6 layers (sky → mountains → trees → ground)\n" +
                "• 3 clouds drifting\n" +
                "• Karu: scale 0.45 (was 0.05) — properly visible\n" +
                "• Mochi: scale 0.40 (matches Karu)\n" +
                "• Squad: 0.30\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void EnsureSprites()
        {
            if (!Directory.Exists(JUNGLE_DIR)) return;
            bool changed = false;
            foreach (var f in Directory.GetFiles(JUNGLE_DIR, "*.png"))
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
            if (changed) AssetDatabase.Refresh();
        }
    }
}
