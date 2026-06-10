using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqAdventureSetup
    {
        [MenuItem("Sparq/46. Add ADVENTURE loot system (chests, butterflies, easter eggs)")]
        public static void Install()
        {
            var existing = GameObject.Find("[ScatteredLoot]");
            if (existing != null) Object.DestroyImmediate(existing);
            var go = new GameObject("[ScatteredLoot]");
            go.AddComponent<Sparq.Systems.ScatteredLoot>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Adventure",
                "✅ Adventure loot system installed.\n\n" +
                "Each Play, 4 random goodies appear scattered in the forest:\n" +
                "• 💰 Chest — random coins + XP\n" +
                "• 🦋 Butterfly — small luck reward\n" +
                "• ✨ Rune — coins + XP, glows\n" +
                "• 🌟 Secret — Easter egg message + small reward\n" +
                "• 🍂 Golden Leaf — coins\n\n" +
                "Tap to collect. New ones respawn every 30-90s.\n\n" +
                "8 different easter egg messages — keep tapping to find them all.\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
