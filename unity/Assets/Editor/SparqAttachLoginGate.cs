using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqAttachLoginGate
    {
        [MenuItem("Sparq/83b. Wire Login Gate (auto-show character pick on first launch)")]
        public static void Wire()
        {
            var existing = GameObject.Find("[LoginGate]");
            if (existing == null)
            {
                var go = new GameObject("[LoginGate]");
                go.AddComponent<Sparq.UI.LoginGate>();
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ LoginGate added.\n\n" +
                "On Play, if no character is picked yet,\n" +
                "the Character Select screen auto-appears.\n\n" +
                "Use Sparq → 83a to reset and trigger it.", "OK");
        }
    }
}
