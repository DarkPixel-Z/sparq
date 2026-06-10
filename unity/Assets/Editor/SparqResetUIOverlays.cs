using UnityEditor;
using UnityEngine;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 999: Nuke any leftover overlay panels that may be blocking input.
    /// Run this in Edit mode if buttons stop responding on the home screen.
    /// </summary>
    public static class SparqResetUIOverlays
    {
        private static readonly string[] PANEL_NAMES = new[]
        {
            "PetPanel", "QuestsPanel", "JournalPanel", "RemindPanel", "FeedPanel",
            "ProfilePanel", "FoodPicker", "RemindCreator", "CustomQuestRoot",
            "BattleScene", "BossIntro", "SocialPanel",
        };

        [MenuItem("Sparq/999. Reset UI overlays (unblock buttons)")]
        public static void Reset()
        {
            int destroyed = 0;
            foreach (var name in PANEL_NAMES)
            {
                while (true)
                {
                    var go = GameObject.Find(name);
                    if (go == null) break;
                    Object.DestroyImmediate(go);
                    destroyed++;
                }
            }
            // Clean up duplicate top-level WorldMap canvases — keep only the first
            var allRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            bool keptFirstWorldMap = false;
            foreach (var go in allRoots)
            {
                if (go == null || go.name != "WorldMap") continue;
                if (!keptFirstWorldMap) { keptFirstWorldMap = true; continue; }
                Object.DestroyImmediate(go);
                destroyed++;
            }
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
            EditorUtility.DisplayDialog("Sparq",
                destroyed > 0
                    ? $"✅ Removed {destroyed} leftover overlay panel(s).\nButtons should respond now."
                    : "No overlay panels found in the scene.\nIf buttons still don't work, check the Console for runtime exceptions.",
                "OK");
        }
    }
}
