using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>Menu 191: Stage map tests.</summary>
    public static class SparqTestMap191
    {
        [MenuItem("Sparq/191. Open Stage Map")]
        public static void Open()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK");
                return;
            }
            Sparq.UI.StageMapPanel.Show();
        }

        [MenuItem("Sparq/191a. Unlock all stages (cheat)")]
        public static void UnlockAll()
        {
            PlayerPrefs.SetInt("sparq.stage.highest", Sparq.Systems.StageService.CHAPTER1.Length);
            PlayerPrefs.Save();
            EditorUtility.DisplayDialog("Sparq", "All 8 stages unlocked.\nOpen MAP to see them all.", "OK");
        }

        [MenuItem("Sparq/191b. Reset stage progress")]
        public static void Reset()
        {
            Sparq.Systems.StageService.Reset();
            EditorUtility.DisplayDialog("Sparq", "Stage progress reset to Stage 1 only.", "OK");
        }
    }
}
