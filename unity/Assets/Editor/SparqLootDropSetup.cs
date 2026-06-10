using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqLootDropSetup
    {
        private const string LOOT_PATH =
            "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_DemoScene_Panels/PopupDim_RewardItems.prefab";
        private const string VICTORY_PATH =
            "Assets/Layer Lab/GUI Pro-SuperCasual/Prefabs/Prefabs_DemoScene_Panels/PopupDim_Play_Result_Victory.prefab";

        [MenuItem("Sparq/43. Wire Loot Drop + Victory popups (Super Casual)")]
        public static void Wire()
        {
            var pmGO = GameObject.Find("[PopupManager]");
            if (pmGO == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "[PopupManager] not in scene. Run Sparq → 18 first.", "OK");
                return;
            }
            var pm = pmGO.GetComponent<Sparq.UI.PopupManager>();

            var loot    = AssetDatabase.LoadAssetAtPath<GameObject>(LOOT_PATH);
            var victory = AssetDatabase.LoadAssetAtPath<GameObject>(VICTORY_PATH);

            if (loot == null || victory == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    $"Prefab(s) missing.\nLoot: {(loot != null ? "ok" : "MISSING")}\nVictory: {(victory != null ? "ok" : "MISSING")}",
                    "OK");
                return;
            }

            var so = new SerializedObject(pm);
            so.FindProperty("lootDropPrefab").objectReferenceValue = loot;
            so.FindProperty("victoryPrefab").objectReferenceValue  = victory;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Loot",
                "✅ Loot Drop + Victory popups wired.\n\n" +
                "Next time you defeat a rival, the Super Casual reward popup appears with:\n" +
                "• Bouncy entry animation\n" +
                "• Coin amount + XP gained\n" +
                "• Tap anywhere to dismiss\n\n" +
                "Hit ▶ Play → tap-defeat Plip (or current rival).", "OK");
        }
    }
}
