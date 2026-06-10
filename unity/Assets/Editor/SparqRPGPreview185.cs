using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 185: Preview each GUI Pro-FantasyRPG prefab so you can browse
    /// and decide which to wire into the game.
    /// </summary>
    public static class SparqRPGPreview185
    {
        private const string PFX_DIR = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_DemoScene_Panels/";

        // Top picks for our roadmap
        [MenuItem("Sparq/185. Preview RPG → Equipment ★")]      public static void Equip()    => Spawn("Equipment.prefab",     "Equipment");
        [MenuItem("Sparq/185a. Preview RPG → Mission (Quests) ★")] public static void Mission() => Spawn("Mission.prefab",       "Mission");
        [MenuItem("Sparq/185b. Preview RPG → Login ★")]          public static void Login()    => Spawn("Login.prefab",         "Login");
        [MenuItem("Sparq/185c. Preview RPG → StageSelect (Map) ★")] public static void Stage()  => Spawn("StageSelect.prefab",   "StageSelect");
        [MenuItem("Sparq/185d. Preview RPG → Shop_Gem ★")]       public static void ShopGem()  => Spawn("Shop_Gem.prefab",      "Shop_Gem");
        [MenuItem("Sparq/185e. Preview RPG → Shop_Gold")]        public static void ShopGold() => Spawn("Shop_Gold.prefab",     "Shop_Gold");
        [MenuItem("Sparq/185f. Preview RPG → Shop_Chest")]       public static void ShopChest()=> Spawn("Shop_Chest.prefab",    "Shop_Chest");
        [MenuItem("Sparq/185g. Preview RPG → Ranking")]          public static void Ranking()  => Spawn("Ranking.prefab",       "Ranking");
        [MenuItem("Sparq/185h. Preview RPG → Guild")]            public static void Guild()    => Spawn("Guild.prefab",         "Guild");
        [MenuItem("Sparq/185i. Preview RPG → CharacterSelect")]  public static void CharSel()  => Spawn("CharacterSelect.prefab", "CharacterSelect");
        [MenuItem("Sparq/185j. Preview RPG → Home")]             public static void Home()     => Spawn("Home.prefab",          "Home");
        [MenuItem("Sparq/185k. Preview RPG → BattlePass")]       public static void BPass()    => Spawn("BattlePass.prefab",    "BattlePass");
        [MenuItem("Sparq/185l. Preview RPG → Settings")]         public static void Settings() => Spawn("Settings.prefab",      "Settings");
        [MenuItem("Sparq/185m. Preview RPG → RewardDaily")]      public static void RDaily()   => Spawn("RewardDaily.prefab",   "RewardDaily");
        [MenuItem("Sparq/185n. Preview RPG → Roulette")]         public static void Roulette() => Spawn("Roulette.prefab",      "Roulette");
        [MenuItem("Sparq/185o. Preview RPG → LevelUp")]          public static void LevelUp()  => Spawn("LevelUp.prefab",       "LevelUp");
        [MenuItem("Sparq/185p. Preview RPG → PlayResult (battle end)")] public static void PResult() => Spawn("PlayResult.prefab", "PlayResult");
        [MenuItem("Sparq/185q. Preview RPG → PlayBoss")]         public static void PBoss()    => Spawn("PlayBoss.prefab",      "PlayBoss");

        [MenuItem("Sparq/185z. Remove RPG previews")]
        public static void Cleanup()
        {
            int n = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name == "RPGPrefabPreview")
                { Object.DestroyImmediate(go); n++; }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq", $"Removed {n} preview(s).", "OK");
        }

        private static void Spawn(string prefabName, string label)
        {
            string path = PFX_DIR + prefabName;
            var pfx = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (pfx == null)
            {
                EditorUtility.DisplayDialog("Sparq", $"Prefab missing:\n{path}", "OK");
                return;
            }

            // Wipe previous preview
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.name == "RPGPrefabPreview")
                    Object.DestroyImmediate(go);
            }

            // Top-level overlay canvas
            var root = new GameObject("RPGPrefabPreview",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14000;
            var sc = root.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight = 0.5f;

            // Dim + close on tap
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.08f, 0.96f);
            var btn = dim.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => Object.DestroyImmediate(root));

            // Spawn prefab centered
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(pfx, root.transform);
            if (inst == null) inst = Object.Instantiate(pfx, root.transform);
            inst.name = label + "_Preview";
            var instRT = inst.GetComponent<RectTransform>();
            if (instRT != null)
            {
                instRT.anchorMin = new Vector2(0.5f, 0.5f);
                instRT.anchorMax = new Vector2(0.5f, 0.5f);
                instRT.pivot     = new Vector2(0.5f, 0.5f);
                instRT.anchoredPosition = Vector2.zero;
            }

            // Hint
            var hint = new GameObject("Hint", typeof(RectTransform));
            hint.transform.SetParent(root.transform, false);
            var hrt = hint.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.anchoredPosition = new Vector2(0, -10);
            hrt.sizeDelta = new Vector2(0, 36);
            var tm = hint.AddComponent<TextMeshProUGUI>();
            tm.text = $"PREVIEW: {label} — tap dim to close";
            tm.fontSize = 18;
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.82f, 0.32f);
            tm.alignment = TextAlignmentOptions.Center;
            tm.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ {label} prefab spawned for preview.\n\nTap dim to close, or run 185z to remove.\n\nLook at it in Game view.", "OK");
        }
    }
}
