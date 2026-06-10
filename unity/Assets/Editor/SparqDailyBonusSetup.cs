using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Wires up the Daily Login 7-Day Bonus carousel:
    ///   • Adds a [DailyBonusManager] to the scene
    ///   • Assigns the GUI Pro Daily_Bonus_7Day prefab to PopupManager
    /// </summary>
    public static class SparqDailyBonusSetup
    {
        private const string DAILY_BONUS_PATH =
            "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/Daily_Bonus_7Day.prefab";

        [MenuItem("Sparq/25. Wire Daily Login Bonus (7-Day)")]
        public static void Wire()
        {
            // 1. Find/create [DailyBonusManager]
            var existing = GameObject.Find("[DailyBonusManager]");
            if (existing != null) Object.DestroyImmediate(existing);
            var go = new GameObject("[DailyBonusManager]");
            go.AddComponent<Sparq.Systems.DailyBonusManager>();

            // 2. Wire the prefab into PopupManager
            var pmGO = GameObject.Find("[PopupManager]");
            if (pmGO == null)
            {
                EditorUtility.DisplayDialog("Sparq Daily",
                    "[PopupManager] not in scene. Run Sparq → 18 first.", "OK");
                return;
            }
            var pm = pmGO.GetComponent<Sparq.UI.PopupManager>();
            if (pm == null)
            {
                EditorUtility.DisplayDialog("Sparq Daily",
                    "PopupManager component missing on [PopupManager].", "OK");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DAILY_BONUS_PATH);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Sparq Daily",
                    "Daily_Bonus_7Day.prefab not found at:\n" + DAILY_BONUS_PATH, "OK");
                return;
            }

            var so = new SerializedObject(pm);
            so.FindProperty("dailyBonusPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Daily Bonus",
                "✅ Daily Login Bonus wired.\n\n" +
                "• [DailyBonusManager] in scene\n" +
                "• 7-Day carousel prefab linked to PopupManager\n" +
                "• On every new calendar day, the carousel pops up automatically\n" +
                "• Tap to claim → coins + XP awarded, streak++\n" +
                "• Day 7 = jackpot. Cycle repeats.\n\n" +
                "Hit ▶ Play — the carousel will appear since you haven't claimed yet today.", "OK");
        }

        [MenuItem("Sparq/26b. SKIP today's Daily Bonus popup (mark claimed)")]
        public static void SkipToday()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) Sparq.Core.SaveService.Load();
            data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.lastDailyBonusDate = System.DateTime.Now.ToString("yyyy-MM-dd");
                Sparq.Core.SaveService.Save();
                EditorUtility.DisplayDialog("Sparq Daily Bonus",
                    "✅ Today's bonus marked claimed.\n\n" +
                    "Hit ▶ Play — popup won't appear today.", "OK");
            }
        }

        [MenuItem("Sparq/26. Reset Daily Bonus (test re-trigger)")]
        public static void ResetForTesting()
        {
            var data = Sparq.Core.SaveService.Data;
            if (data == null) Sparq.Core.SaveService.Load();
            data = Sparq.Core.SaveService.Data;
            if (data != null)
            {
                data.lastDailyBonusDate = "";
                Sparq.Core.SaveService.Save();
                EditorUtility.DisplayDialog("Sparq Daily Bonus",
                    "✅ Cleared lastDailyBonusDate.\n\n" +
                    "Hit ▶ Play — bonus carousel will re-appear.", "OK");
            }
        }
    }
}
