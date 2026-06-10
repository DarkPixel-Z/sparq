using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHideRivalCard
    {
        [MenuItem("Sparq/47. Hide Rival Card from home (battles only on map)")]
        public static void Hide()
        {
            var card = GameObject.Find("RivalCard");
            if (card != null) card.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Rival Card hidden from home.\n\n" +
                "• Home screen now shows only Karu, forest, loot pickups\n" +
                "• Battles still work — tapping Karu still drains the selected rival's HP behind the scenes\n" +
                "• See current rival on the world map (tap MAP)\n" +
                "• Defeating a rival → loot popup → next rival auto-loaded\n\n" +
                "To re-show: select RivalCard in Hierarchy → check Active.", "OK");
        }

        [MenuItem("Sparq/47a. Show Rival Card again")]
        public static void Show()
        {
            // Find by name even if disabled
            var allRTs = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rt in allRTs)
            {
                if (rt != null && rt.name == "RivalCard")
                {
                    rt.gameObject.SetActive(true);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    EditorUtility.DisplayDialog("Sparq", "✅ Rival Card visible again.", "OK");
                    return;
                }
            }
        }
    }
}
