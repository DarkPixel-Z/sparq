using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>Menu 159: Wipe duplicate SocialPanel + WorldRoot leftovers.</summary>
    public static class SparqDedupeSocial159
    {
        [MenuItem("Sparq/159. Dedupe SocialPanel + remove old WorldRoot")]
        public static void Apply()
        {
            int sp = 0, wr = 0;
            GameObject keep = null;

            // Wipe everything named SocialPanel except the most recent
            var all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in all)
            {
                if (go == null) continue;
                if (go.name == "SocialPanel")
                {
                    if (keep == null) keep = go;
                    else { Object.DestroyImmediate(go); sp++; }
                }
                else if (go.name == "WorldRoot")
                {
                    Object.DestroyImmediate(go); wr++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Cleanup:\n• Removed {sp} duplicate SocialPanel(s)\n• Removed {wr} stale WorldRoot(s)\n\nKept 1 SocialPanel.\n\nHit ▶ Play and tap WORLD.", "OK");
        }
    }
}
