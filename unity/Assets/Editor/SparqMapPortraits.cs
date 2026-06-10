using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Swaps the WorldMap's flat colored stage squares for the actual RIVAL PORTRAITS.
    /// Each stage shows the real monster sprite — locked ones ghosted grey.
    /// Top Heroes-style: enemies live ON the map.
    /// </summary>
    public static class SparqMapPortraits
    {
        private const string PREFAB_PATH = "Assets/Prefabs/WorldMap.prefab";

        [MenuItem("Sparq/44. Map nodes → real rival portraits")]
        public static void Apply()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (existing == null)
            {
                EditorUtility.DisplayDialog("Sparq", "WorldMap.prefab not found. Run Sparq → 39 first.", "OK");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(existing);
            var content = instance.transform.Find("MapScroll/MapContent");
            if (content == null)
            {
                EditorUtility.DisplayDialog("Sparq", "MapContent missing in WorldMap.", "OK");
                Object.DestroyImmediate(instance);
                return;
            }

            int updated = 0;
            for (int i = 0; i < Sparq.Systems.RivalRoster.ROSTER.Length; i++)
            {
                var r = Sparq.Systems.RivalRoster.ROSTER[i];
                var nodeName = $"Stage_{i+1}_{r.name}";
                var node = content.Find(nodeName);
                if (node == null) continue;

                // Bigger node so the sprite reads
                var nrt = node.GetComponent<RectTransform>();
                if (nrt != null) nrt.sizeDelta = new Vector2(150, 150);

                var img = node.GetComponent<Image>();
                if (img == null) continue;

                // Try to load the rival's sprite
                Sprite portrait = LoadRivalSprite(r);
                if (portrait != null)
                {
                    img.sprite = portrait;
                    img.preserveAspect = true;
                    img.color = Color.white; // remove tint so the art reads clean
                }

                // Hide the big stage number that was inside the node — make it a small badge
                var numChild = node.Find("Num");
                if (numChild != null)
                {
                    var numTM = numChild.GetComponent<TMPro.TMP_Text>();
                    if (numTM != null)
                    {
                        numTM.fontSize = 28;
                        numTM.alignment = TMPro.TextAlignmentOptions.BottomLeft;
                        numTM.color = new Color(1f, 0.85f, 0.3f);
                        numTM.fontStyle = TMPro.FontStyles.Bold;
                    }
                }

                // Add tier-colored badge ring behind the sprite
                if (node.Find("TierRing") == null)
                {
                    var ring = new GameObject("TierRing", typeof(RectTransform), typeof(Image));
                    ring.transform.SetParent(node, false);
                    ring.transform.SetAsFirstSibling(); // behind portrait
                    var rrt = ring.GetComponent<RectTransform>();
                    rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
                    rrt.offsetMin = new Vector2(-12, -12); rrt.offsetMax = new Vector2(12, 12);
                    Color tierColor = r.tier switch {
                        "mini"   => new Color(0.3f, 0.85f, 0.45f, 0.7f),
                        "fodder" => new Color(0.95f, 0.78f, 0.25f, 0.7f),
                        "elite"  => new Color(1.0f, 0.5f, 0.2f, 0.7f),
                        "boss"   => new Color(0.95f, 0.25f, 0.30f, 0.85f),
                        _        => new Color(0.5f, 0.5f, 0.5f, 0.5f)
                    };
                    ring.GetComponent<Image>().color = tierColor;
                    ring.GetComponent<Image>().raycastTarget = false;
                }
                updated++;
            }

            // Save back to prefab
            PrefabUtility.SaveAsPrefabAsset(instance, PREFAB_PATH);
            Object.DestroyImmediate(instance);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Map Portraits",
                $"✅ {updated} map nodes now show real rival portraits.\n\n" +
                "• Tier-colored ring behind each\n" +
                "• Stage number as small gold badge\n" +
                "• Locked rivals will still grey out at runtime\n\n" +
                "Hit ▶ Play → tap MAP. The map is now ALIVE.", "OK");
        }

        private static Sprite LoadRivalSprite(Sparq.Systems.RivalRoster.Rival r)
        {
            // Animated rival → first idle frame
            if (!string.IsNullOrEmpty(r.folderName))
            {
                string dir = $"Assets/Fantasy Monster Pack 5 Handcrafted 2D Creatures/{r.folderName}/{r.animSubfolder}";
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.png");
                    System.Array.Sort(files);
                    if (files.Length > 0)
                    {
                        string ap = files[0].Replace('\\','/');
                        int idx = ap.IndexOf("Assets/");
                        if (idx >= 0) ap = ap.Substring(idx);
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(ap);
                        if (sp != null) return sp;
                    }
                }
            }
            // Static path
            if (!string.IsNullOrEmpty(r.staticSpritePath))
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(r.staticSpritePath);
                if (sp != null) return sp;
            }
            return null;
        }
    }
}
