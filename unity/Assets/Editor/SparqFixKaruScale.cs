using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqFixKaruScale
    {
        [MenuItem("Sparq/30. Fix Karu (bear) size + position")]
        public static void Fix()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq", "No Karu in scene.", "OK");
                return;
            }

            // The bear prefab is ~5 world units tall. Camera ortho size is ~5.
            // Scale it down to read like a small character (~1.5 world units tall)
            karu.transform.localScale = new Vector3(0.35f, 0.35f, 1f);

            // Position it on the "ground" line of the forest (mid-bottom of screen)
            karu.transform.position = new Vector3(0f, -0.6f, 0f);

            // Constrain its collider to actual visible size
            var col = karu.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(2.5f, 3.0f);
                col.offset = new Vector2(0f, 0.3f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq] Karu scale = 0.35, position = (0, -0.6).");
            EditorUtility.DisplayDialog("Sparq",
                "✅ Karu sized down + planted on forest floor.\n\n" +
                "Hit ▶ Play.\n\n" +
                "If still too big: select Karu in Hierarchy → Inspector → Transform → Scale, set to 0.25 or 0.2.",
                "OK");
        }

        [MenuItem("Sparq/30b. Revert to original SVG Karu")]
        public static void Revert()
        {
            var bear = GameObject.Find("Karu");
            var oldKaru = GameObject.Find("Karu_OLD_SVG_disabled");
            if (oldKaru == null)
            {
                EditorUtility.DisplayDialog("Sparq", "No disabled SVG Karu found to restore.", "OK");
                return;
            }
            if (bear != null && bear != oldKaru) Object.DestroyImmediate(bear);
            oldKaru.name = "Karu";
            oldKaru.SetActive(true);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq", "✅ Original SVG Karu restored.", "OK");
        }
    }
}
