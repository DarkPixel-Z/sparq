using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 154: Promote the Una HelpIcon to always render above modals
    /// (WorldPanel etc) by giving it its own override Canvas at sortingOrder 10000.
    /// </summary>
    public static class SparqUnaAlwaysOnTop154
    {
        [MenuItem("Sparq/154. Una HelpIcon → render above modals")]
        public static void Apply()
        {
            var help = GameObject.Find("HelpIcon");
            if (help == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HelpIcon not found.", "OK");
                return;
            }

            // Add Canvas + GraphicRaycaster (so it still receives clicks)
            var canvas = help.GetComponent<Canvas>();
            if (canvas == null) canvas = help.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder    = 10000; // above WorldRoot (9999)

            var raycaster = help.GetComponent<GraphicRaycaster>();
            if (raycaster == null) help.AddComponent<GraphicRaycaster>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Una HelpIcon now renders above all modals.\n\n" +
                "• Own override Canvas, sortingOrder 10000\n" +
                "• Stays clickable (GraphicRaycaster)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
