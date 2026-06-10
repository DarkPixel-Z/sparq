using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Builds a Top Heroes-style scrollable world map screen.
    /// • Full-screen painted background (Dreamy Forest tiles)
    /// • 20 stage nodes positioned along a zig-zag path
    /// • Node states: Current (pulsing gold), Unlocked (green), Locked (grey)
    /// • Tap node → set current rival + dismiss map + return to home
    /// • Pan via drag (ScrollRect)
    /// </summary>
    public static class SparqWorldMap
    {
        [MenuItem("Sparq/39. Build WORLD MAP (Top Heroes style)")]
        public static void Build()
        {
            // Find/create a WorldMap prefab at Assets/Prefabs
            var path = "Assets/Prefabs/WorldMap.prefab";

            // Build in-memory prefab root
            var root = new GameObject("WorldMap", typeof(RectTransform));
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Scroll container for pan
            var scrollGO = new GameObject("MapScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(root.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;

            var bgImg = scrollGO.GetComponent<Image>();
            bgImg.color = new Color(0.18f, 0.08f, 0.35f); // purple base (matches sky)
            bgImg.raycastTarget = true;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.inertia = true;

            // Content (the big map)
            var contentGO = new GameObject("MapContent", typeof(RectTransform));
            contentGO.transform.SetParent(scrollGO.transform, false);
            var crt = contentGO.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0.5f);
            crt.anchorMax = new Vector2(0f, 0.5f);
            crt.pivot     = new Vector2(0f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(3200, 1400); // big map

            scroll.content = crt;

            // Paint background with gradient-ish color blocks + forest sprites
            BuildPaintedBackground(contentGO.transform);

            // Add nodes for each rival
            for (int i = 0; i < Sparq.Systems.RivalRoster.ROSTER.Length; i++)
            {
                BuildStageNode(contentGO.transform, i);
            }

            // Overlay: title bar + close button + player avatar
            BuildOverlay(root.transform);

            // Save as prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs");
                AssetDatabase.Refresh();
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            // Wire it into PopupManager (replacing the old stageMapPrefab)
            var pmGO = GameObject.Find("[PopupManager]");
            if (pmGO != null)
            {
                var pm = pmGO.GetComponent<Sparq.UI.PopupManager>();
                var so = new SerializedObject(pm);
                so.FindProperty("stageMapPrefab").objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq World Map",
                "✅ Painted World Map built!\n\n" +
                "• 20 stage nodes in zig-zag path\n" +
                "• Scroll via drag (horizontal + vertical)\n" +
                "• Locked stages grey + 🔒\n" +
                "• Current stage pulses gold\n" +
                "• Tap node → sets rival, returns home\n\n" +
                "Hit ▶ Play → tap MAP. Then scroll around.", "OK");
        }

        private static void BuildPaintedBackground(Transform content)
        {
            // 5 biome bands horizontally
            string[] biomeNames = { "Moonveil Forest", "Shimmer Desert", "Bone Crypt", "Scorched Ridge", "Abyss Tower" };
            Color[] biomeColors = {
                new Color(0.25f, 0.18f, 0.45f),   // purple forest
                new Color(0.55f, 0.40f, 0.22f),   // desert tan
                new Color(0.18f, 0.22f, 0.32f),   // crypt grey-blue
                new Color(0.55f, 0.22f, 0.15f),   // scorched red
                new Color(0.12f, 0.06f, 0.20f),   // abyss black
            };

            float bandWidth = 3200f / biomeColors.Length;
            for (int i = 0; i < biomeColors.Length; i++)
            {
                var bandGO = new GameObject($"Biome_{i}_{biomeNames[i]}", typeof(RectTransform), typeof(Image));
                bandGO.transform.SetParent(content, false);
                var brt = bandGO.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(0, 1);
                brt.pivot = new Vector2(0, 0.5f);
                brt.anchoredPosition = new Vector2(i * bandWidth, 0);
                brt.sizeDelta = new Vector2(bandWidth, 0);
                var img = bandGO.GetComponent<Image>();
                img.color = biomeColors[i];
                img.raycastTarget = false;

                // Biome label
                var lblGO = new GameObject("Label", typeof(RectTransform));
                lblGO.transform.SetParent(bandGO.transform, false);
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.5f, 0.92f);
                lrt.anchorMax = new Vector2(0.5f, 0.92f);
                lrt.pivot     = new Vector2(0.5f, 0.5f);
                lrt.anchoredPosition = Vector2.zero;
                lrt.sizeDelta = new Vector2(500, 70);
                var tm = lblGO.AddComponent<TextMeshProUGUI>();
                tm.text = biomeNames[i];
                tm.fontSize = 36;
                tm.color = new Color(1f, 0.9f, 0.7f, 0.8f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.fontStyle = FontStyles.Bold;
                tm.raycastTarget = false;
            }

            // Random scattered tree/bush sprites across the whole map
            string[] foliage = {
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/tree1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/bush1.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/bush2.png",
                "Assets/Dreamy Forest 2D Lite - MGLawless/Trees and Foliage/grass1.png",
            };
            for (int i = 0; i < 40; i++)
            {
                var path = foliage[Random.Range(0, foliage.Length)];
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp == null) continue;
                var deco = new GameObject($"Deco_{i}", typeof(RectTransform), typeof(Image));
                deco.transform.SetParent(content, false);
                var drt = deco.GetComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 0.5f);
                drt.anchorMax = new Vector2(0, 0.5f);
                drt.pivot = new Vector2(0.5f, 0.5f);
                drt.anchoredPosition = new Vector2(Random.Range(80f, 3120f), Random.Range(-550f, 550f));
                drt.sizeDelta = new Vector2(90, 90) * Random.Range(0.7f, 1.3f);
                var img = deco.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.color = new Color(1, 1, 1, 0.6f);
                img.raycastTarget = false;
            }
        }

        private static void BuildStageNode(Transform content, int index)
        {
            var r = Sparq.Systems.RivalRoster.ROSTER[index];

            // Zig-zag path: alternating up/down, spread across 3200 width
            float x = Mathf.Lerp(150f, 3050f, (float)index / (Sparq.Systems.RivalRoster.ROSTER.Length - 1));
            float y = Mathf.Sin(index * 0.8f) * 350f;

            var nodeGO = new GameObject($"Stage_{index+1}_{r.name}", typeof(RectTransform), typeof(Image), typeof(Button));
            nodeGO.transform.SetParent(content, false);
            var nrt = nodeGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f);
            nrt.anchorMax = new Vector2(0, 0.5f);
            nrt.pivot = new Vector2(0.5f, 0.5f);
            nrt.anchoredPosition = new Vector2(x, y);
            nrt.sizeDelta = new Vector2(110, 110);

            var nImg = nodeGO.GetComponent<Image>();
            // Color by tier
            switch (r.tier)
            {
                case "mini":   nImg.color = new Color(0.3f, 0.85f, 0.45f); break; // green
                case "fodder": nImg.color = new Color(0.9f, 0.75f, 0.2f); break; // yellow
                case "elite":  nImg.color = new Color(0.95f, 0.5f, 0.2f); break; // orange
                case "boss":   nImg.color = new Color(0.9f, 0.2f, 0.25f); break; // red
                default:       nImg.color = new Color(0.6f, 0.6f, 0.7f); break;
            }

            // Button wires to a StageNodeButton component that knows its index
            var btn = nodeGO.GetComponent<Button>();
            var nodeCtrl = nodeGO.AddComponent<Sparq.UI.StageNodeButton>();
            nodeCtrl.rivalIndex = index;
            btn.onClick.AddListener(nodeCtrl.OnTap);

            // Label below
            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(nodeGO.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 0f);
            lrt.pivot = new Vector2(0.5f, 1f);
            lrt.anchoredPosition = new Vector2(0f, -4f);
            lrt.sizeDelta = new Vector2(240, 56);
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = $"{index+1}. {r.name}";
            tm.fontSize = 22;
            tm.color = Color.white;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;

            // Stage number inside
            var numGO = new GameObject("Num", typeof(RectTransform));
            numGO.transform.SetParent(nodeGO.transform, false);
            var nrt2 = numGO.GetComponent<RectTransform>();
            nrt2.anchorMin = Vector2.zero; nrt2.anchorMax = Vector2.one;
            nrt2.offsetMin = Vector2.zero; nrt2.offsetMax = Vector2.zero;
            var numTM = numGO.AddComponent<TextMeshProUGUI>();
            numTM.text = (index + 1).ToString();
            numTM.fontSize = 48;
            numTM.color = new Color(0.1f, 0.05f, 0.2f);
            numTM.fontStyle = FontStyles.Bold;
            numTM.alignment = TextAlignmentOptions.Center;
            numTM.raycastTarget = false;
        }

        private static void BuildOverlay(Transform root)
        {
            // Top bar with title
            var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(root, false);
            var trt = topBar.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = Vector2.zero;
            trt.sizeDelta = new Vector2(0, 100);
            topBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            topBar.GetComponent<Image>().raycastTarget = false;

            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(topBar.transform, false);
            var tirt = titleGO.GetComponent<RectTransform>();
            tirt.anchorMin = Vector2.zero; tirt.anchorMax = Vector2.one;
            tirt.offsetMin = Vector2.zero; tirt.offsetMax = Vector2.zero;
            var titleTM = titleGO.AddComponent<TextMeshProUGUI>();
            titleTM.text = "WORLD MAP";
            titleTM.fontSize = 42;
            titleTM.color = new Color(1f, 0.9f, 0.6f);
            titleTM.fontStyle = FontStyles.Bold;
            titleTM.alignment = TextAlignmentOptions.Center;
            titleTM.raycastTarget = false;
        }
    }
}
