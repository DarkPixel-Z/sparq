using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 181: Hard-disable the DailyBonusManager GameObject so the calendar
    /// stops auto-popping every time you hit Play. Re-enable from Inspector
    /// when you want it back.
    /// </summary>
    public static class SparqDisableDaily181
    {
        [MenuItem("Sparq/181. Disable Daily Bonus auto-popup")]
        public static void Apply()
        {
            int n = 0;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name == "[DailyBonusManager]" || go.name == "DailyBonusManager")
                {
                    go.SetActive(false);
                    n++;
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                $"✅ Disabled {n} DailyBonusManager(s).\n\nDaily Bonus popup will no longer auto-fire on Play.\n\nRe-enable from Inspector if you want it back.\n\nHit ▶ Play.", "OK");
        }
    }
}
