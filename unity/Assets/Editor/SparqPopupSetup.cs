using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Drops a [PopupManager] into the scene and wires the GUI Pro
    /// PopupFullScreen_LevelUp prefab reference.
    /// </summary>
    public static class SparqPopupSetup
    {
        private const string LEVELUP_PATH =
            "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_DemoScene_Panels/PopupFullScreen_LevelUp.prefab";

        [MenuItem("Sparq/18. Wire LevelUp Popup")]
        public static void Wire()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LEVELUP_PATH);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Sparq Popup",
                    "Can't find PopupFullScreen_LevelUp.prefab at:\n" + LEVELUP_PATH, "OK");
                return;
            }

            // Find or create [PopupManager]
            var existing = GameObject.Find("[PopupManager]");
            if (existing != null) Object.DestroyImmediate(existing);

            var go = new GameObject("[PopupManager]");
            var mgr = go.AddComponent<Sparq.UI.PopupManager>();

            // Assign the prefab via SerializedObject (so it persists)
            var so = new SerializedObject(mgr);
            so.FindProperty("levelUpPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Popup",
                "✅ PopupManager wired.\n\n" +
                "• Level up in-game → full-screen Fantasy Hero popup appears\n" +
                "• Shows the new level number + flavor text\n" +
                "• Click anywhere or wait 5s to dismiss\n\n" +
                "Hit ▶ Play → tap quests until level up.", "OK");
        }
    }
}
