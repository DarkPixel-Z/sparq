using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 156: Preview each Layer Lab fantasy social prefab in the scene
    /// so you can see what they actually look like before wiring them in.
    /// Each preview spawns the prefab inside an overlay canvas + tap-anywhere-to-close.
    /// </summary>
    public static class SparqPreviewPrefabs156
    {
        private const string PFX_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/";

        [MenuItem("Sparq/156. Preview prefab — Chat")]
        public static void PreviewChat() => Spawn(PFX_DIR + "Chat.prefab", "Chat");

        [MenuItem("Sparq/156a. Preview prefab — Clan (guild)")]
        public static void PreviewClan() => Spawn(PFX_DIR + "Clan.prefab", "Clan");

        [MenuItem("Sparq/156b. Preview prefab — Ranking (leaderboard)")]
        public static void PreviewRanking() => Spawn(PFX_DIR + "Ranking.prefab", "Ranking");

        [MenuItem("Sparq/156c. Preview prefab — Player Profile")]
        public static void PreviewProfile() => Spawn(PFX_DIR + "Player_Profile_1.prefab", "Player Profile");

        [MenuItem("Sparq/156z. Remove all prefab previews")]
        public static void Cleanup()
        {
            int n = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name == "PrefabPreviewRoot")
                { Object.DestroyImmediate(go); n++; }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq", $"Removed {n} preview(s).", "OK");
        }

        private static void Spawn(string prefabPath, string label)
        {
            var pfx = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (pfx == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Prefab not found:\n{prefabPath}", "OK");
                return;
            }

            // Wipe any prior preview
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name == "PrefabPreviewRoot") Object.DestroyImmediate(go);
            }

            // Top-level overlay canvas at scene root so it dominates everything
            var root = new GameObject("PrefabPreviewRoot",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var rc = root.GetComponent<Canvas>();
            rc.renderMode = RenderMode.ScreenSpaceOverlay;
            rc.sortingOrder = 9998;
            var rs = root.GetComponent<CanvasScaler>();
            rs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            rs.referenceResolution = new Vector2(1080, 1920);
            rs.matchWidthOrHeight = 0.5f;

            // Dim + close on tap
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);
            var btn = dim.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => Object.DestroyImmediate(root));

            // Instantiate prefab as child of root canvas
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfx, root.transform);
            if (inst == null) inst = Object.Instantiate(pfx, root.transform);
            inst.name = label + "_Preview";

            // Center the instance
            var instRT = inst.GetComponent<RectTransform>();
            if (instRT != null)
            {
                instRT.anchorMin = new Vector2(0.5f, 0.5f);
                instRT.anchorMax = new Vector2(0.5f, 0.5f);
                instRT.pivot     = new Vector2(0.5f, 0.5f);
                instRT.anchoredPosition = Vector2.zero;
            }

            // Hint text top-right
            var hint = new GameObject("Hint", typeof(RectTransform));
            hint.transform.SetParent(root.transform, false);
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -10);
            hrt.sizeDelta = new Vector2(0, 36);
            var tm = hint.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = $"PREVIEW: {label} prefab — tap dim to close";
            tm.fontSize = 18;
            tm.fontStyle = TMPro.FontStyles.Bold;
            tm.color = new Color(1f, 0.82f, 0.32f);
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ {label} prefab spawned for preview.\n\n" +
                "• Centered, dim background, tap to close\n" +
                "• Or run Sparq/156z to remove\n\n" +
                "Hit ▶ Play to see it interactive, or just look in Game view.", "OK");
        }
    }
}
