using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 188: Cleanup pass — wipe any stale EquipmentPanel / BattleScene /
    /// SocialPanel runtime objects that may have been saved into the scene,
    /// reset Time.timeScale, and re-enable IdleBob on hero/pet.
    /// </summary>
    public static class SparqAnimFix188
    {
        [MenuItem("Sparq/188. Fix frozen home screen (cleanup runtime leftovers)")]
        public static void Apply()
        {
            int wiped = 0;
            string[] runtimeNames = { "EquipmentPanel", "BattleScene", "WorldRoot", "RPGPrefabPreview", "BattleRunner", "PrefabPreviewRoot" };

            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null) continue;
                foreach (var n in runtimeNames)
                {
                    if (go.name == n) { Object.DestroyImmediate(go); wiped++; break; }
                }
            }

            // Re-enable IdleBob on hero & pet
            int reenabled = 0;
            foreach (var name in new[] { "Karu", "Mochi" })
            {
                var go = GameObject.Find(name);
                if (go == null) continue;
                var bob = go.GetComponent<Sparq.UI.IdleBob>();
                if (bob != null) { bob.enabled = true; reenabled++; }
            }

            // Reset timeScale just in case
            Time.timeScale = 1f;

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Cleanup:\n• Wiped {wiped} runtime leftover object(s)\n• Re-enabled IdleBob on {reenabled} character(s)\n• Time.timeScale → 1\n\nHit ▶ Play.", "OK");
        }
    }
}
