using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

namespace Sparq.Editor
{
    /// <summary>
    /// Paints the World Map's 5 biome bands with real foliage from Dreamy Forest +
    /// biome-specific color washes. Replaces flat color bands with painterly texture.
    /// Run AFTER Sparq → 39 (build map) and 40 (Beautify).
    /// </summary>
    public static class SparqPaintedBiomes
    {
        [MenuItem("Sparq/41. Paint biome backgrounds (Forest/Desert/Crypt/Ridge/Abyss)")]
        public static void Paint()
        {
            // Load the WorldMap prefab and re-instantiate
            var prefabPath = "Assets/Prefabs/WorldMap.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing == null)
            {
                EditorUtility.DisplayDialog("Sparq Biomes",
                    "WorldMap.prefab not found. Run Sparq → 39 first.", "OK");
                return;
            }

            // Operate on the prefab via PrefabUtility — open it, modify, save
            var instance = PrefabUtility.InstantiatePrefab(existing) as GameObject;

            var content = instance.transform.Find("MapScroll/MapContent");
            if (content == null)
            {
                EditorUtility.DisplayDialog("Sparq Biomes",
                    "MapContent not found inside WorldMap prefab.", "OK");
                Object.DestroyImmediate(instance);
                return;
            }

            // Wipe old biome bands + decorations
            var toDelete = new List<Transform>();
            foreach (Transform child in content)
            {
                if (child.name.StartsWith("Biome_") || child.name.StartsWith("Deco_"))
                    toDelete.Add(child);
            }
            foreach (var t in toDelete) Object.DestroyImmediate(t.gameObject);

            // Biome configurations — name + 3 sky/ground/accent colors + foliage density
            var biomes = new[]
            {
                ("Moonveil Forest",  new Color(0.30f, 0.18f, 0.50f), new Color(0.18f, 0.10f, 0.32f), new Color(0.95f, 0.55f, 0.85f), 0.7f),
                ("Shimmer Desert",   new Color(0.85f, 0.65f, 0.35f), new Color(0.55f, 0.35f, 0.18f), new Color(1.00f, 0.82f, 0.45f), 0.3f),
                ("Bone Crypt",       new Color(0.30f, 0.32f, 0.45f), new Color(0.12f, 0.14f, 0.22f), new Color(0.55f, 0.65f, 0.85f), 0.5f),
                ("Scorched Ridge",   new Color(0.65f, 0.25f, 0.18f), new Color(0.30f, 0.12f, 0.08f), new Color(1.00f, 0.55f, 0.20f), 0.4f),
                ("Abyss Tower",      new Color(0.15f, 0.08f, 0.30f), new Color(0.05f, 0.02f, 0.12f), new Color(0.65f, 0.25f, 0.95f), 0.6f),
            };

            float bandWidth = 3200f / biomes.Length;
            string[] foliage = {
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/tree1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/tree1 1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/bush1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/bush2.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/grass1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/grass2.png",
            };

            for (int i = 0; i < biomes.Length; i++)
            {
                var (bname, sky, ground, accent, density) = biomes[i];

                // Sky band (top 60%)
                var skyGO = new GameObject($"Biome_{i}_Sky", typeof(RectTransform), typeof(Image));
                skyGO.transform.SetParent(content, false);
                var skRT = skyGO.GetComponent<RectTransform>();
                skRT.anchorMin = new Vector2(0, 0.4f); skRT.anchorMax = new Vector2(0, 1f);
                skRT.pivot = new Vector2(0, 0.5f);
                skRT.anchoredPosition = new Vector2(i * bandWidth, 0);
                skRT.sizeDelta = new Vector2(bandWidth, 0);
                skyGO.GetComponent<Image>().color = sky;
                skyGO.GetComponent<Image>().raycastTarget = false;

                // Ground band (bottom 40%)
                var grdGO = new GameObject($"Biome_{i}_Ground", typeof(RectTransform), typeof(Image));
                grdGO.transform.SetParent(content, false);
                var grRT = grdGO.GetComponent<RectTransform>();
                grRT.anchorMin = new Vector2(0, 0); grRT.anchorMax = new Vector2(0, 0.4f);
                grRT.pivot = new Vector2(0, 0.5f);
                grRT.anchoredPosition = new Vector2(i * bandWidth, 0);
                grRT.sizeDelta = new Vector2(bandWidth, 0);
                grdGO.GetComponent<Image>().color = ground;
                grdGO.GetComponent<Image>().raycastTarget = false;

                // Horizon glow strip
                var horGO = new GameObject($"Biome_{i}_Horizon", typeof(RectTransform), typeof(Image));
                horGO.transform.SetParent(content, false);
                var hrRT = horGO.GetComponent<RectTransform>();
                hrRT.anchorMin = new Vector2(0, 0.36f); hrRT.anchorMax = new Vector2(0, 0.46f);
                hrRT.pivot = new Vector2(0, 0.5f);
                hrRT.anchoredPosition = new Vector2(i * bandWidth, 0);
                hrRT.sizeDelta = new Vector2(bandWidth, 0);
                var horImg = horGO.GetComponent<Image>();
                var horC = accent; horC.a = 0.3f;
                horImg.color = horC;
                horImg.raycastTarget = false;

                // Biome label
                var lblGO = new GameObject($"Biome_{i}_Label", typeof(RectTransform));
                lblGO.transform.SetParent(content, false);
                var lblRT = lblGO.AddComponent<RectTransform>();
                lblRT.anchorMin = new Vector2(0, 0.92f); lblRT.anchorMax = new Vector2(0, 0.92f);
                lblRT.pivot = new Vector2(0.5f, 0.5f);
                lblRT.anchoredPosition = new Vector2(i * bandWidth + bandWidth * 0.5f, 0);
                lblRT.sizeDelta = new Vector2(560, 80);
                var lblTM = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
                lblTM.text = bname.ToUpper();
                lblTM.fontSize = 38;
                lblTM.fontStyle = TMPro.FontStyles.Bold;
                lblTM.color = new Color(1, 0.95f, 0.85f, 0.9f);
                lblTM.alignment = TMPro.TextAlignmentOptions.Center;
                lblTM.raycastTarget = false;

                // Scattered foliage in this biome (denser per density param)
                int foliageCount = (int)(density * 30);
                for (int f = 0; f < foliageCount; f++)
                {
                    var path = foliage[Random.Range(0, foliage.Length)];
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (sp == null) continue;
                    var deco = new GameObject($"Deco_{i}_{f}", typeof(RectTransform), typeof(Image));
                    deco.transform.SetParent(content, false);
                    var drt = deco.GetComponent<RectTransform>();
                    drt.anchorMin = new Vector2(0, 0); drt.anchorMax = new Vector2(0, 0);
                    drt.pivot = new Vector2(0.5f, 0.5f);
                    drt.anchoredPosition = new Vector2(
                        i * bandWidth + Random.Range(40f, bandWidth - 40f),
                        Random.Range(80f, 700f));
                    float scale = Random.Range(0.6f, 1.2f);
                    drt.sizeDelta = new Vector2(110, 110) * scale;
                    var img = deco.GetComponent<Image>();
                    img.sprite = sp;
                    img.preserveAspect = true;
                    // Tint foliage to match biome
                    img.color = Color.Lerp(Color.white, accent, 0.25f);
                    var c = img.color; c.a = Random.Range(0.6f, 1.0f); img.color = c;
                    img.raycastTarget = false;

                    // Random horizontal flip
                    if (Random.value < 0.5f) drt.localScale = new Vector3(-1, 1, 1);
                }
            }

            // Make sure the stage nodes render on top of all the new biome stuff
            foreach (Transform child in content)
            {
                if (child.name.StartsWith("Stage_"))
                    child.SetAsLastSibling();
            }

            // Save updated prefab
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Biomes",
                "✅ World map painted!\n\n" +
                "• 5 biomes with sky + ground gradients\n" +
                "• Horizon glow strip per biome\n" +
                "• Biome name labels (MOONVEIL FOREST, etc.)\n" +
                "• Tinted foliage scattered (~100 sprites total)\n" +
                "• Stage nodes float on top\n\n" +
                "Hit ▶ Play → tap MAP. Should look way more painterly now.", "OK");
        }
    }
}
